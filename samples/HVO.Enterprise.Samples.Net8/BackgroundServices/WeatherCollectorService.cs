using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Services;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.BackgroundJobs;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.BackgroundServices
{
    /// <summary>
    /// Periodically fetches weather data from all monitored locations.
    /// Demonstrates:
    ///   • BackgroundService with cancellation support
    ///   • Correlation propagation into background work
    ///   • Background job context capture &amp; restore
    ///   • Operation scopes in background threads
    ///   • Exception handling that doesn't crash the worker
    ///   • Metric recording for monitoring collection health
    ///   • IServiceScopeFactory for resolving scoped services from a singleton
    /// </summary>
    public sealed class WeatherCollectorService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
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
            ITelemetryService telemetry,
            IOperationScopeFactory scopeFactory,
            ILogger<WeatherCollectorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _telemetry = telemetry;
            _scopeFactory = scopeFactory;
            _logger = logger;
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

                // Capture context for any sub-tasks
                var jobContext = BackgroundJobContext.Capture();

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

                    // Resolve scoped IWeatherService within its own DI scope.
                    // This is the correct pattern for background services that
                    // need scoped dependencies.
                    using var serviceScope = _serviceScopeFactory.CreateScope();
                    var weatherService = serviceScope.ServiceProvider
                        .GetRequiredService<IWeatherService>();

                    var sw = Stopwatch.StartNew();
                    var summary = await weatherService.GetWeatherSummaryAsync(stoppingToken);
                    sw.Stop();

                    // Evaluate alerts from collected data
                    var alerts = weatherService.EvaluateAlerts(summary.Observations);

                    scope.WithTag("collector.locations_collected", summary.LocationCount)
                         .WithTag("collector.avg_temperature", summary.AverageTemperature)
                         .WithTag("collector.alert_count", alerts.Count)
                         .WithTag("collector.duration_ms", sw.ElapsedMilliseconds)
                         .Succeed();

                    _telemetry.RecordMetric("collector.cycle.duration_ms", sw.ElapsedMilliseconds);
                    _telemetry.RecordMetric("collector.cycle.locations", summary.LocationCount);
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
                        "avg {AvgTemp}°C, {Alerts} alerts, {Duration}ms",
                        _collectionCount, summary.LocationCount,
                        summary.AverageTemperature, alerts.Count, sw.ElapsedMilliseconds);
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
