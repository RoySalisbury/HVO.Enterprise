using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Caching;
using HVO.Enterprise.Samples.Net8.Data;
using HVO.Enterprise.Samples.Net8.Messaging;
using HVO.Enterprise.Samples.Net8.Services;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.BackgroundServices
{
    /// <summary>
    /// Periodically fetches weather data from all monitored locations and feeds
    /// the multi-stage processing pipeline.
    /// Demonstrates:
    ///   • BackgroundService with cancellation support
    ///   • Correlation propagation into background work
    ///   • Operation scopes in background threads
    ///   • Exception handling that doesn't crash the worker
    ///   • Metric recording for monitoring collection health
    ///   • IServiceScopeFactory for resolving scoped services from a singleton
    ///   • Persisting observations to SQLite via EF Core
    ///   • Publishing observations to the message pipeline
    ///   • Caching the latest weather summary in Redis
    /// </summary>
    public sealed class WeatherCollectorService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly FakeMessageBus _messageBus;
        private readonly ITelemetryService _telemetry;
        private readonly IOperationScopeFactory _scopeFactory;
        private readonly ILogger<WeatherCollectorService> _logger;

        /// <summary>How often to collect weather data.</summary>
        private static readonly TimeSpan CollectionInterval = TimeSpan.FromMinutes(5);

        /// <summary>Initial delay before first collection (lets the app start up).</summary>
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(10);

        private int _collectionCount;

        public WeatherCollectorService(
            IServiceScopeFactory serviceScopeFactory,
            FakeMessageBus messageBus,
            ITelemetryService telemetry,
            IOperationScopeFactory scopeFactory,
            ILogger<WeatherCollectorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Weather collector starting. Interval={Interval}, InitialDelay={Delay}",
                CollectionInterval, InitialDelay);

            // Wait before first collection so the application has time to start
            try
            {
                await Task.Delay(InitialDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                // Each collection cycle gets its own correlation context
                using var correlationScope = CorrelationContext.BeginScope(
                    $"collector-{Interlocked.Increment(ref _collectionCount)}");

                using var scope = _scopeFactory.Begin("WeatherCollector.Cycle", new()
                {
                    ActivityKind = ActivityKind.Internal,
                    InitialTags = new Dictionary<string, object?>
                    {
                        ["collector.cycle"] = _collectionCount,
                        ["collector.correlation_id"] = CorrelationContext.Current
                    }
                });

                try
                {
                    _logger.LogInformation(
                        "Collection cycle #{Cycle} starting (CorrelationId={CorrelationId})",
                        _collectionCount, CorrelationContext.Current);

                    // Resolve scoped services within their own DI scope.
                    // This is the correct pattern for background services that
                    // need scoped dependencies.
                    using var serviceScope = _serviceScopeFactory.CreateScope();
                    var weatherService = serviceScope.ServiceProvider
                        .GetRequiredService<IWeatherService>();
                    var repository = serviceScope.ServiceProvider
                        .GetService<WeatherRepository>();
                    var cacheService = serviceScope.ServiceProvider
                        .GetService<WeatherCacheService>();

                    var sw = Stopwatch.StartNew();
                    var summary = await weatherService.GetWeatherSummaryAsync(stoppingToken);
                    sw.Stop();

                    scope.WithTag("collector.fetch_duration_ms", sw.ElapsedMilliseconds);

                    // ── Persist observations to SQLite in batch ──
                    int persistedCount = 0;
                    if (repository != null)
                    {
                        var persistSw = Stopwatch.StartNew();
                        var entities = summary.Observations.Select(observation => new WeatherReadingEntity
                        {
                            Location = observation.LocationName,
                            TemperatureCelsius = observation.TemperatureCelsius,
                            Humidity = observation.RelativeHumidity,
                            WindSpeedKmh = observation.WindSpeedKmh,
                            Condition = $"WMO:{observation.WeatherCode}",
                            RecordedAtUtc = DateTime.UtcNow,
                            CorrelationId = CorrelationContext.Current,
                        }).ToList();

                        persistedCount = await repository.AddReadingsAsync(entities, stoppingToken)
                            .ConfigureAwait(false);
                        persistSw.Stop();

                        _logger.LogInformation(
                            "Persisted {Count} readings to database in {Duration}ms (CorrelationId={CorrelationId})",
                            persistedCount, persistSw.ElapsedMilliseconds, CorrelationContext.Current);

                        scope.WithTag("collector.persisted_count", persistedCount)
                             .WithTag("collector.persist_duration_ms", persistSw.ElapsedMilliseconds);
                    }
                    else
                    {
                        _logger.LogDebug("WeatherRepository not registered — skipping persistence");
                    }

                    // ── Publish each observation to the message pipeline ──
                    var publishSw = Stopwatch.StartNew();
                    var events = summary.Observations.Select(observation => new WeatherObservationEvent
                    {
                        Location = observation.LocationName,
                        TemperatureCelsius = observation.TemperatureCelsius,
                        Humidity = observation.RelativeHumidity,
                        WindSpeedKmh = observation.WindSpeedKmh,
                        Condition = $"WMO:{observation.WeatherCode}",
                        ObservedAtUtc = DateTime.UtcNow,
                    }).ToList();

                    int publishedCount = 0;
                    foreach (var evt in events)
                    {
                        await _messageBus.PublishAsync(
                            FakeMessageBus.ObservationsTopic, evt, stoppingToken)
                            .ConfigureAwait(false);
                        publishedCount++;
                    }
                    publishSw.Stop();

                    _logger.LogInformation(
                        "Published {Count} observations to [{Topic}] in {Duration}ms (CorrelationId={CorrelationId})",
                        publishedCount, FakeMessageBus.ObservationsTopic,
                        publishSw.ElapsedMilliseconds, CorrelationContext.Current);

                    scope.WithTag("collector.published_count", publishedCount)
                         .WithTag("collector.publish_duration_ms", publishSw.ElapsedMilliseconds);

                    // ── Cache the latest summary in Redis ──
                    if (cacheService != null)
                    {
                        var cacheSw = Stopwatch.StartNew();
                        await cacheService.GetOrCreateAsync(
                            "weather:summary:latest",
                            _ => Task.FromResult(summary),
                            TimeSpan.FromMinutes(4), // slightly shorter than collection interval
                            stoppingToken).ConfigureAwait(false);
                        cacheSw.Stop();

                        _logger.LogInformation(
                            "Cached latest weather summary (TTL=4min, {Duration}ms, CorrelationId={CorrelationId})",
                            cacheSw.ElapsedMilliseconds, CorrelationContext.Current);

                        scope.WithTag("collector.cache_duration_ms", cacheSw.ElapsedMilliseconds);
                    }
                    else
                    {
                        _logger.LogDebug("WeatherCacheService not registered — skipping cache");
                    }

                    // ── Evaluate alerts from collected data ──
                    var alerts = weatherService.EvaluateAlerts(summary.Observations);

                    var totalDurationMs = sw.ElapsedMilliseconds + publishSw.ElapsedMilliseconds;

                    scope.WithTag("collector.locations_collected", summary.LocationCount)
                         .WithTag("collector.avg_temperature", summary.AverageTemperature)
                         .WithTag("collector.alert_count", alerts.Count)
                         .WithTag("collector.total_duration_ms", totalDurationMs)
                         .Succeed();

                    _telemetry.RecordMetric("collector.cycle.duration_ms", totalDurationMs);
                    _telemetry.RecordMetric("collector.cycle.locations", summary.LocationCount);
                    _telemetry.RecordMetric("collector.cycle.persisted", persistedCount);
                    _telemetry.RecordMetric("collector.cycle.published", publishedCount);
                    _telemetry.TrackEvent("collector.cycle.completed");

                    if (alerts.Count > 0)
                    {
                        foreach (var alert in alerts)
                        {
                            _logger.LogWarning(
                                "ALERT [{Severity}] {AlertType} at {Location}: {Message}",
                                alert.Severity, alert.AlertType, alert.LocationName, alert.Message);
                        }
                    }

                    _logger.LogInformation(
                        "Collection cycle #{Cycle} complete: {Locations} locations, " +
                        "avg {AvgTemp}°C, {Persisted} persisted, {Published} published, " +
                        "{Alerts} alerts, {Duration}ms (CorrelationId={CorrelationId})",
                        _collectionCount, summary.LocationCount,
                        summary.AverageTemperature, persistedCount, publishedCount,
                        alerts.Count, totalDurationMs, CorrelationContext.Current);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Collection cycle #{Cycle} cancelled", _collectionCount);
                    break;
                }
                catch (Exception ex)
                {
                    scope.RecordException(ex);
                    scope.Fail(ex);
                    _telemetry.TrackException(ex);
                    _telemetry.RecordMetric("collector.cycle.errors", 1);

                    _logger.LogError(ex,
                        "Collection cycle #{Cycle} failed. Will retry in {Interval}",
                        _collectionCount, CollectionInterval);
                }

                // Wait for the next cycle
                try
                {
                    await Task.Delay(CollectionInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Weather collector stopped after {Cycles} cycles", _collectionCount);
        }
    }
}
