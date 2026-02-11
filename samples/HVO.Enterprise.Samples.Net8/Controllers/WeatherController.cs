using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Models;
using HVO.Enterprise.Samples.Net8.Services;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Controllers
{
    /// <summary>
    /// REST controller demonstrating telemetry integration in ASP.NET Core controllers.
    /// Each action manually creates an operation scope for fine-grained control,
    /// adds tags/properties, and records metrics.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly ITelemetryService _telemetry;
        private readonly IOperationScopeFactory _scopeFactory;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(
            IWeatherService weatherService,
            ITelemetryService telemetry,
            IOperationScopeFactory scopeFactory,
            ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _telemetry = telemetry;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Returns a weather summary for all monitored locations.
        /// GET /api/weather/summary
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(WeatherSummary), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WeatherSummary>> GetSummary(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.Begin("Controller.GetWeatherSummary", new()
            {
                ActivityKind = ActivityKind.Server,
                InitialTags = new Dictionary<string, object?>
                {
                    ["http.route"] = "GET /api/weather/summary",
                    ["correlation.id"] = CorrelationContext.Current
                }
            });

            try
            {
                var summary = await _weatherService.GetWeatherSummaryAsync(cancellationToken);

                scope.WithTag("weather.location_count", summary.LocationCount)
                     .WithTag("weather.avg_temp", summary.AverageTemperature)
                     .Succeed();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
                _logger.LogError(ex, "Failed to get weather summary");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Returns current weather for a specific location by name.
        /// GET /api/weather/{locationName}
        /// </summary>
        [HttpGet("{locationName}")]
        [ProducesResponseType(typeof(WeatherObservation), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WeatherObservation>> GetByLocation(
            string locationName, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.Begin("Controller.GetWeatherByLocation", new()
            {
                ActivityKind = ActivityKind.Server
            });

            scope.WithTag("weather.location_name", locationName);

            var locations = _weatherService.GetMonitoredLocations();
            var location = locations.FirstOrDefault(l =>
                string.Equals(l.Name, locationName, StringComparison.OrdinalIgnoreCase));

            if (location == null)
            {
                scope.WithTag("weather.location_found", false);
                _logger.LogWarning("Location {LocationName} not found", locationName);
                return NotFound(new { error = $"Location '{locationName}' is not in the monitored list." });
            }

            try
            {
                scope.WithTag("weather.location_found", true);
                var observation = await _weatherService.GetCurrentWeatherAsync(
                    location.Name, location.Latitude, location.Longitude, cancellationToken);

                scope.Succeed();
                return Ok(observation);
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
                _logger.LogError(ex, "Error fetching weather for {Location}", locationName);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lists all currently monitored locations.
        /// GET /api/weather/locations
        /// </summary>
        [HttpGet("locations")]
        [ProducesResponseType(typeof(IReadOnlyList<MonitoredLocation>), 200)]
        public ActionResult<IReadOnlyList<MonitoredLocation>> GetLocations()
        {
            _telemetry.TrackEvent("controller.locations.listed");
            return Ok(_weatherService.GetMonitoredLocations());
        }

        /// <summary>
        /// Adds a new monitored location.
        /// POST /api/weather/locations
        /// </summary>
        [HttpPost("locations")]
        [ProducesResponseType(typeof(MonitoredLocation), 201)]
        [ProducesResponseType(400)]
        public ActionResult<MonitoredLocation> AddLocation([FromBody] AddLocationRequest request)
        {
            using var scope = _scopeFactory.Begin("Controller.AddLocation");

            // Demonstrates parameter capture via tags
            scope.WithTag("location.name", request.Name)
                 .WithTag("location.latitude", request.Latitude)
                 .WithTag("location.longitude", request.Longitude);

            try
            {
                _weatherService.AddMonitoredLocation(request.Name, request.Latitude, request.Longitude);
                var location = new MonitoredLocation(request.Name, request.Latitude, request.Longitude);

                scope.Succeed();
                _telemetry.RecordMetric("weather.locations.total", _weatherService.GetMonitoredLocations().Count);

                return CreatedAtAction(nameof(GetLocations), location);
            }
            catch (ArgumentException ex)
            {
                scope.Fail(ex);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Removes a monitored location.
        /// DELETE /api/weather/locations/{name}
        /// </summary>
        [HttpDelete("locations/{name}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public ActionResult RemoveLocation(string name)
        {
            _telemetry.TrackEvent("controller.location.delete.attempted");

            if (_weatherService.RemoveMonitoredLocation(name))
            {
                return NoContent();
            }

            return NotFound(new { error = $"Location '{name}' not found." });
        }

        /// <summary>
        /// Evaluates weather alerts based on the latest cached observations.
        /// GET /api/weather/alerts
        /// </summary>
        [HttpGet("alerts")]
        [ProducesResponseType(typeof(IReadOnlyList<WeatherAlert>), 200)]
        public ActionResult<IReadOnlyList<WeatherAlert>> GetAlerts()
        {
            using var scope = _scopeFactory.Begin("Controller.GetAlerts");

            var locations = _weatherService.GetMonitoredLocations();
            // We need current observations, trigger a quick check with defaults
            // In a real app, this would use cached data; here we evaluate what we have
            scope.WithTag("monitored_location_count", locations.Count);

            // Return empty alerts (background service will populate cache)
            var alerts = _weatherService.EvaluateAlerts(Array.Empty<WeatherObservation>());
            scope.WithTag("alert_count", alerts.Count).Succeed();

            return Ok(alerts);
        }

        /// <summary>
        /// Returns live telemetry diagnostics — statistics, error rates, throughput.
        /// GET /api/weather/diagnostics
        /// </summary>
        [HttpGet("diagnostics")]
        [ProducesResponseType(typeof(TelemetryDiagnosticsResponse), 200)]
        public ActionResult<TelemetryDiagnosticsResponse> GetDiagnostics()
        {
            var stats = _telemetry.Statistics;
            var snapshot = stats.GetSnapshot();

            return Ok(new TelemetryDiagnosticsResponse
            {
                ActivitiesCreated = snapshot.ActivitiesCreated,
                ActivitiesCompleted = snapshot.ActivitiesCompleted,
                ActiveActivities = snapshot.ActiveActivities,
                ExceptionsTracked = snapshot.ExceptionsTracked,
                EventsRecorded = snapshot.EventsRecorded,
                MetricsRecorded = snapshot.MetricsRecorded,
                QueueDepth = snapshot.QueueDepth,
                ItemsProcessed = snapshot.ItemsProcessed,
                ItemsDropped = snapshot.ItemsDropped,
                AverageProcessingTimeMs = snapshot.AverageProcessingTimeMs,
                CurrentErrorRate = snapshot.CurrentErrorRate,
                CurrentThroughput = snapshot.CurrentThroughput,
                Uptime = snapshot.Uptime
            });
        }

        /// <summary>
        /// Intentionally throws an exception to demonstrate exception tracking.
        /// GET /api/weather/error-demo
        /// </summary>
        [HttpGet("error-demo")]
        [ProducesResponseType(500)]
        public ActionResult TriggerErrorDemo()
        {
            using var scope = _scopeFactory.Begin("Controller.ErrorDemo");

            try
            {
                scope.WithTag("demo", true);
                throw new InvalidOperationException(
                    "This is a deliberate error to demonstrate exception tracking and aggregation.");
            }
            catch (Exception ex)
            {
                // scope.Fail() handles everything: sets error status on the
                // Activity, records exception tags (type + fingerprint), and
                // marks the scope as failed.  Calling TrackException() or
                // RecordException() as well would duplicate the tags.
                scope.Fail(ex);

                _logger.LogError(ex, "Deliberate error for demo purposes");
                return StatusCode(500, new
                {
                    error = ex.Message,
                    note = "This error was intentional. Check /api/weather/diagnostics to see it tracked."
                });
            }
        }
    }
}
