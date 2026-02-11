using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Models;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.BackgroundJobs;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Services
{
    /// <summary>
    /// Weather service that fetches real-time data from the Open-Meteo API.
    /// Demonstrates comprehensive use of the HVO.Enterprise.Telemetry library:
    ///   • Operation scopes with tags / lazy properties
    ///   • Correlation context propagation
    ///   • Exception recording and aggregation
    ///   • Structured logging with telemetry enrichment
    ///   • Background job context capture
    ///   • Metric recording
    /// </summary>
    public sealed class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ITelemetryService _telemetry;
        private readonly IOperationScopeFactory _scopeFactory;
        private readonly ExceptionAggregator _exceptionAggregator;
        private readonly ILogger<WeatherService> _logger;

        /// <summary>Thread-safe collection of monitored locations.</summary>
        private readonly ConcurrentDictionary<string, MonitoredLocation> _locations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Cache of last observations per location for alert evaluation.</summary>
        private readonly ConcurrentDictionary<string, WeatherObservation> _lastObservations = new(StringComparer.OrdinalIgnoreCase);

        private const string OpenMeteoBaseUrl = "https://api.open-meteo.com/v1/forecast";

        // Alert thresholds
        private const double HighTemperatureThreshold = 35.0;
        private const double LowTemperatureThreshold = -10.0;
        private const double HighWindSpeedThreshold = 80.0;

        public WeatherService(
            HttpClient httpClient,
            ITelemetryService telemetry,
            IOperationScopeFactory scopeFactory,
            ILogger<WeatherService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionAggregator = new ExceptionAggregator(expirationWindow: TimeSpan.FromHours(1));

            // Seed with some well-known cities
            SeedDefaultLocations();
        }

        /// <inheritdoc />
        public async Task<WeatherObservation> GetCurrentWeatherAsync(
            string locationName, double latitude, double longitude,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.Begin("WeatherService.GetCurrentWeather", new()
            {
                ActivityKind = ActivityKind.Client,
                InitialTags = new Dictionary<string, object?>
                {
                    ["weather.location"] = locationName,
                    ["weather.latitude"] = latitude,
                    ["weather.longitude"] = longitude,
                },
                LogEvents = true,
                CaptureExceptions = true
            });

            _logger.LogInformation("Fetching weather for {Location} ({Lat}, {Lon})",
                locationName, latitude, longitude);

            try
            {
                var url = $"{OpenMeteoBaseUrl}?latitude={latitude}&longitude={longitude}" +
                          "&current_weather=true&timezone=auto";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<OpenMeteoResponse>(json);

                if (data?.CurrentWeather == null)
                {
                    throw new InvalidOperationException(
                        $"Open-Meteo returned null current_weather for {locationName}");
                }

                var observation = new WeatherObservation
                {
                    LocationName = locationName,
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    ObservedAt = DateTimeOffset.UtcNow,
                    TemperatureCelsius = data.CurrentWeather.Temperature,
                    WindSpeedKmh = data.CurrentWeather.WindSpeed,
                    RelativeHumidity = 0, // Not in current_weather endpoint, placeholder
                    WeatherCode = data.CurrentWeather.WeatherCode
                };

                // Tag the scope with result data
                scope.WithTag("weather.temperature_c", observation.TemperatureCelsius)
                     .WithTag("weather.wind_speed_kmh", observation.WindSpeedKmh)
                     .WithTag("weather.code", observation.WeatherCode)
                     .WithTag("weather.description", observation.WeatherDescription)
                     .WithResult(observation.WeatherDescription);

                // Record metrics
                _telemetry.RecordMetric("weather.api.calls", 1);
                _telemetry.RecordMetric("weather.temperature_celsius", observation.TemperatureCelsius);
                _telemetry.RecordMetric("weather.wind_speed_kmh", observation.WindSpeedKmh);

                // Cache for alert evaluation
                _lastObservations[locationName] = observation;

                scope.Succeed();

                _logger.LogDebug(
                    "Weather for {Location}: {Temp}°C, {Wind} km/h, {Description}",
                    locationName, observation.TemperatureCelsius,
                    observation.WindSpeedKmh, observation.WeatherDescription);

                return observation;
            }
            catch (HttpRequestException ex)
            {
                _exceptionAggregator.RecordException(ex);
                _telemetry.TrackException(ex);
                scope.Fail(ex);
                _logger.LogError(ex, "HTTP error fetching weather for {Location}", locationName);
                throw;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Weather request for {Location} was cancelled", locationName);
                scope.WithTag("cancelled", true);
                throw;
            }
            catch (Exception ex)
            {
                _exceptionAggregator.RecordException(ex);
                _telemetry.TrackException(ex);
                scope.Fail(ex);
                _logger.LogError(ex, "Unexpected error fetching weather for {Location}", locationName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<WeatherSummary> GetWeatherSummaryAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.Begin("WeatherService.GetSummary");

            var locations = _locations.Values.ToList();

            _logger.LogInformation("Collecting weather data for {Count} locations", locations.Count);
            _telemetry.TrackEvent("weather.summary.started");
            scope.WithTag("location_count", locations.Count);

            var observations = new List<WeatherObservation>();
            var errors = new List<(string Location, Exception Error)>();

            foreach (var loc in locations)
            {
                try
                {
                    var obs = await GetCurrentWeatherAsync(
                        loc.Name, loc.Latitude, loc.Longitude, cancellationToken);
                    observations.Add(obs);
                }
                catch (Exception ex)
                {
                    errors.Add((loc.Name, ex));
                    _logger.LogWarning(ex,
                        "Failed to fetch weather for {Location}, skipping", loc.Name);
                }
            }

            if (observations.Count == 0)
            {
                var exception = new InvalidOperationException(
                    $"Failed to fetch weather for any of {locations.Count} locations. " +
                    $"Errors: {string.Join(", ", errors.Select(e => $"{e.Location}: {e.Error.Message}"))}");
                scope.Fail(exception);
                throw exception;
            }

            var summary = new WeatherSummary
            {
                LocationCount = observations.Count,
                AverageTemperature = Math.Round(observations.Average(o => o.TemperatureCelsius), 1),
                MinTemperature = observations.Min(o => o.TemperatureCelsius),
                MaxTemperature = observations.Max(o => o.TemperatureCelsius),
                AverageWindSpeed = Math.Round(observations.Average(o => o.WindSpeedKmh), 1),
                CollectedAt = DateTimeOffset.UtcNow,
                Observations = observations
            };

            _telemetry.RecordMetric("weather.summary.location_count", summary.LocationCount);
            _telemetry.RecordMetric("weather.summary.avg_temperature", summary.AverageTemperature);
            _telemetry.TrackEvent("weather.summary.completed");

            scope.Succeed();
            scope.WithResult(summary);

            _logger.LogInformation(
                "Weather summary: {Count} locations, avg {AvgTemp}°C ({MinTemp}..{MaxTemp}°C), " +
                "avg wind {AvgWind} km/h, {ErrorCount} errors",
                summary.LocationCount, summary.AverageTemperature,
                summary.MinTemperature, summary.MaxTemperature,
                summary.AverageWindSpeed, errors.Count);

            return summary;
        }

        /// <inheritdoc />
        public IReadOnlyList<MonitoredLocation> GetMonitoredLocations()
        {
            _telemetry.TrackEvent("weather.locations.listed");
            return _locations.Values.ToList();
        }

        /// <inheritdoc />
        public void AddMonitoredLocation(string name, double latitude, double longitude)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Location name is required.", nameof(name));
            if (latitude < -90 || latitude > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
            if (longitude < -180 || longitude > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

            var location = new MonitoredLocation(name, latitude, longitude);
            if (!_locations.TryAdd(name, location))
            {
                _logger.LogWarning("Location {Name} already exists, updating", name);
                _locations[name] = location;
            }

            _telemetry.TrackEvent("weather.location.added");
            _logger.LogInformation("Added monitored location: {Name} ({Lat}, {Lon})",
                name, latitude, longitude);
        }

        /// <inheritdoc />
        public bool RemoveMonitoredLocation(string name)
        {
            var removed = _locations.TryRemove(name, out _);
            if (removed)
            {
                _lastObservations.TryRemove(name, out _);
                _telemetry.TrackEvent("weather.location.removed");
                _logger.LogInformation("Removed monitored location: {Name}", name);
            }
            return removed;
        }

        /// <inheritdoc />
        public IReadOnlyList<WeatherAlert> EvaluateAlerts(IEnumerable<WeatherObservation> observations)
        {
            var alerts = new List<WeatherAlert>();

            foreach (var obs in observations)
            {
                if (obs.TemperatureCelsius > HighTemperatureThreshold)
                {
                    alerts.Add(CreateAlert(obs.LocationName, "HighTemperature",
                        $"Temperature {obs.TemperatureCelsius}°C exceeds {HighTemperatureThreshold}°C threshold",
                        "Warning"));
                }

                if (obs.TemperatureCelsius < LowTemperatureThreshold)
                {
                    alerts.Add(CreateAlert(obs.LocationName, "LowTemperature",
                        $"Temperature {obs.TemperatureCelsius}°C below {LowTemperatureThreshold}°C threshold",
                        "Warning"));
                }

                if (obs.WindSpeedKmh > HighWindSpeedThreshold)
                {
                    alerts.Add(CreateAlert(obs.LocationName, "HighWind",
                        $"Wind speed {obs.WindSpeedKmh} km/h exceeds {HighWindSpeedThreshold} km/h threshold",
                        "Critical"));
                }

                if (obs.WeatherCode >= 95)
                {
                    alerts.Add(CreateAlert(obs.LocationName, "SevereWeather",
                        $"Severe weather detected: {obs.WeatherDescription}",
                        "Critical"));
                }
            }

            if (alerts.Count > 0)
            {
                _telemetry.RecordMetric("weather.alerts.raised", alerts.Count);
                _logger.LogWarning("Raised {AlertCount} weather alerts", alerts.Count);
            }

            return alerts;
        }

        // ────────────────────────────────────────────────────────────

        private void SeedDefaultLocations()
        {
            var defaults = new[]
            {
                new MonitoredLocation("New York",      40.7128,  -74.0060),
                new MonitoredLocation("London",        51.5074,   -0.1278),
                new MonitoredLocation("Tokyo",         35.6762,  139.6503),
                new MonitoredLocation("Sydney",       -33.8688,  151.2093),
                new MonitoredLocation("São Paulo",    -23.5505,  -46.6333),
            };

            foreach (var loc in defaults)
            {
                _locations.TryAdd(loc.Name, loc);
            }
        }

        private static WeatherAlert CreateAlert(
            string locationName, string alertType, string message, string severity)
        {
            return new WeatherAlert
            {
                AlertId = Guid.NewGuid().ToString("N")[..12],
                LocationName = locationName,
                AlertType = alertType,
                Message = message,
                Severity = severity,
                IssuedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
