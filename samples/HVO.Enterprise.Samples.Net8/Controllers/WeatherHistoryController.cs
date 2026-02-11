using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Caching;
using HVO.Enterprise.Samples.Net8.Data;
using HVO.Enterprise.Samples.Net8.Messaging;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Controllers
{
    /// <summary>
    /// REST controller for weather history from the SQLite database.
    /// Demonstrates EF Core, ADO.NET, caching, and messaging integration
    /// with full HVO telemetry instrumentation.
    /// </summary>
    [ApiController]
    [Route("api/weather/history")]
    [Produces("application/json")]
    public class WeatherHistoryController : ControllerBase
    {
        private readonly WeatherRepository _repository;
        private readonly WeatherAdoNetRepository _adoNetRepository;
        private readonly WeatherCacheService _cache;
        private readonly WeatherObservationPublisher _publisher;
        private readonly ITelemetryService _telemetry;
        private readonly ILogger<WeatherHistoryController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherHistoryController"/> class.
        /// </summary>
        public WeatherHistoryController(
            WeatherRepository repository,
            WeatherAdoNetRepository adoNetRepository,
            WeatherCacheService cache,
            WeatherObservationPublisher publisher,
            ITelemetryService telemetry,
            ILogger<WeatherHistoryController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _adoNetRepository = adoNetRepository ?? throw new ArgumentNullException(nameof(adoNetRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets recent weather readings from the database (EF Core).
        /// Results are cached using the cache-aside pattern.
        /// </summary>
        /// <param name="location">Optional location filter.</param>
        /// <param name="count">Maximum results (default 20).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent weather readings.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<WeatherReadingEntity>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentReadings(
            [FromQuery] string? location = null,
            [FromQuery] int count = 20,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"weather:history:{location ?? "all"}:{count}";

            var readings = await _cache.GetOrCreateAsync(
                cacheKey,
                async ct => await _repository.GetRecentReadingsAsync(location, count, ct).ConfigureAwait(false),
                TimeSpan.FromMinutes(1),
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} readings for {Location} (CorrelationId={CorrelationId})",
                readings?.Count ?? 0, location ?? "all", CorrelationContext.Current);

            return Ok(readings);
        }

        /// <summary>
        /// Gets aggregate weather statistics for a location (EF Core).
        /// </summary>
        /// <param name="location">Location to aggregate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("{location}/aggregate")]
        [ProducesResponseType(typeof(WeatherAggregateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAggregate(
            string location, CancellationToken cancellationToken = default)
        {
            var aggregate = await _repository.GetAggregateAsync(location, cancellationToken)
                .ConfigureAwait(false);

            if (aggregate == null)
            {
                return NotFound(new { message = $"No readings found for location '{location}'" });
            }

            return Ok(aggregate);
        }

        /// <summary>
        /// Gets location averages using raw ADO.NET queries.
        /// Demonstrates instrumented ADO.NET alongside EF Core.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("averages")]
        [ProducesResponseType(typeof(Dictionary<string, double>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLocationAverages(CancellationToken cancellationToken = default)
        {
            var cacheKey = "weather:averages";

            var averages = await _cache.GetOrCreateAsync(
                cacheKey,
                async ct => await _adoNetRepository.GetLocationAveragesAsync(ct).ConfigureAwait(false),
                TimeSpan.FromMinutes(2),
                cancellationToken).ConfigureAwait(false);

            return Ok(averages);
        }

        /// <summary>
        /// Gets total count of weather readings.
        /// Demonstrates both EF Core and ADO.NET returning the same result.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("count")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCount(CancellationToken cancellationToken = default)
        {
            var efCoreCount = await _repository.GetTotalCountAsync(cancellationToken).ConfigureAwait(false);
            var adoNetCount = await _adoNetRepository.GetTotalCountAsync(cancellationToken).ConfigureAwait(false);

            return Ok(new
            {
                efCoreCount,
                adoNetCount,
                match = efCoreCount == adoNetCount,
            });
        }

        /// <summary>
        /// Adds a weather reading and publishes an observation event to the message bus.
        /// Demonstrates write path: EF Core persist → cache invalidation → message publish.
        /// </summary>
        /// <param name="request">Weather reading data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [ProducesResponseType(typeof(WeatherReadingEntity), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddReading(
            [FromBody] AddWeatherReadingRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new { message = "Location is required" });
            }

            // 1. Persist to database via EF Core
            var entity = new WeatherReadingEntity
            {
                Location = request.Location,
                TemperatureCelsius = request.TemperatureCelsius,
                Humidity = request.Humidity,
                WindSpeedKmh = request.WindSpeedKmh,
                Condition = request.Condition,
                RecordedAtUtc = DateTime.UtcNow,
                CorrelationId = CorrelationContext.Current,
            };

            var saved = await _repository.AddReadingAsync(entity, cancellationToken).ConfigureAwait(false);

            // 2. Invalidate relevant cache entries
            await _cache.InvalidateAsync($"weather:history:{request.Location}:20", cancellationToken)
                .ConfigureAwait(false);
            await _cache.InvalidateAsync("weather:history:all:20", cancellationToken)
                .ConfigureAwait(false);
            await _cache.InvalidateAsync("weather:averages", cancellationToken)
                .ConfigureAwait(false);

            // 3. Publish observation event to message bus
            await _publisher.PublishObservationAsync(new WeatherObservationEvent
            {
                Location = request.Location,
                TemperatureCelsius = request.TemperatureCelsius,
                Humidity = request.Humidity,
                WindSpeedKmh = request.WindSpeedKmh,
                Condition = request.Condition,
                ObservedAtUtc = entity.RecordedAtUtc,
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Added weather reading for {Location}: {Temperature}°C (Id={ReadingId})",
                saved.Location, saved.TemperatureCelsius, saved.Id);

            return CreatedAtAction(
                nameof(GetRecentReadings),
                new { location = saved.Location },
                saved);
        }

        /// <summary>
        /// Gets extension-specific diagnostics (DB stats, cache stats, messaging stats).
        /// Enhances the existing /api/weather/diagnostics with extension data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("/api/weather/extensions")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExtensionDiagnostics(CancellationToken cancellationToken = default)
        {
            var dbCount = await _repository.GetTotalCountAsync(cancellationToken).ConfigureAwait(false);

            return Ok(new
            {
                database = new
                {
                    provider = "SQLite",
                    totalReadings = dbCount,
                },
                cache = new
                {
                    provider = "FakeRedisCache (in-process)",
                },
                messaging = new
                {
                    provider = "FakeMessageBus (System.Threading.Channels)",
                },
                telemetry = new
                {
                    enabled = _telemetry.IsEnabled,
                    correlationId = CorrelationContext.Current,
                },
            });
        }
    }

    /// <summary>
    /// Request model for adding a weather reading.
    /// </summary>
    public class AddWeatherReadingRequest
    {
        /// <summary>Location name.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Temperature in Celsius.</summary>
        public double TemperatureCelsius { get; set; }

        /// <summary>Humidity percentage (0-100).</summary>
        public double? Humidity { get; set; }

        /// <summary>Wind speed in km/h.</summary>
        public double? WindSpeedKmh { get; set; }

        /// <summary>Weather condition description.</summary>
        public string? Condition { get; set; }
    }
}
