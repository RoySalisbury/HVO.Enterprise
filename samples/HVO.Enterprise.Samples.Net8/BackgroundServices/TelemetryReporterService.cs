using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.BackgroundServices
{
    /// <summary>
    /// Periodically logs telemetry statistics to the console/log output.
    /// Demonstrates:
    ///   • IHostedService (non-BackgroundService) pattern
    ///   • Reading ITelemetryStatistics for operational visibility
    ///   • Structured logging with telemetry enrichment
    ///   • Correlation context in background work
    /// </summary>
    public sealed class TelemetryReporterService : IHostedService, IDisposable
    {
        private readonly ITelemetryService _telemetry;
        private readonly ILogger<TelemetryReporterService> _logger;
        private Timer? _timer;
        private int _reportCount;

        /// <summary>How often to emit a telemetry report.</summary>
        private static readonly TimeSpan ReportInterval = TimeSpan.FromMinutes(1);

        public TelemetryReporterService(
            ITelemetryService telemetry,
            ILogger<TelemetryReporterService> logger)
        {
            _telemetry = telemetry;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry reporter starting with interval {Interval}", ReportInterval);
            _timer = new Timer(EmitReport, null, TimeSpan.FromSeconds(30), ReportInterval);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry reporter stopping after {Reports} reports", _reportCount);
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void EmitReport(object? state)
        {
            try
            {
                Interlocked.Increment(ref _reportCount);

                using var correlationScope = CorrelationContext.BeginScope(
                    $"reporter-{_reportCount}");

                var stats = _telemetry.Statistics;
                var snapshot = stats.GetSnapshot();

                _logger.LogInformation(
                    "╔══════════════════════════════════════════════════════╗\n" +
                    "║           TELEMETRY STATUS REPORT #{Report,-5}            ║\n" +
                    "╠══════════════════════════════════════════════════════╣\n" +
                    "║ Uptime:            {Uptime,-35}║\n" +
                    "║ Activities Created: {Created,-34}║\n" +
                    "║ Active Activities:  {Active,-34}║\n" +
                    "║ Exceptions Tracked: {Exceptions,-34}║\n" +
                    "║ Events Recorded:    {Events,-34}║\n" +
                    "║ Metrics Recorded:   {Metrics,-34}║\n" +
                    "║ Queue Depth:        {Queue,-34}║\n" +
                    "║ Items Processed:    {Processed,-34}║\n" +
                    "║ Items Dropped:      {Dropped,-34}║\n" +
                    "║ Error Rate:         {ErrorRate,-34}║\n" +
                    "║ Throughput:         {Throughput,-34}║\n" +
                    "╚══════════════════════════════════════════════════════╝",
                    _reportCount,
                    snapshot.Uptime.ToString(@"d\.hh\:mm\:ss"),
                    snapshot.ActivitiesCreated,
                    snapshot.ActiveActivities,
                    snapshot.ExceptionsTracked,
                    snapshot.EventsRecorded,
                    snapshot.MetricsRecorded,
                    snapshot.QueueDepth,
                    snapshot.ItemsProcessed,
                    snapshot.ItemsDropped,
                    $"{snapshot.CurrentErrorRate:F2}/min",
                    $"{snapshot.CurrentThroughput:F2}/sec");

                _telemetry.TrackEvent("reporter.cycle.completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error emitting telemetry report");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
