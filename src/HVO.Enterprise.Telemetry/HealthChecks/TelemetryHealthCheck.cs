using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// ASP.NET Core health check for the telemetry system.
    /// Evaluates error rates, queue depth, and dropped item percentages against
    /// configurable thresholds to determine system health.
    /// </summary>
    public sealed class TelemetryHealthCheck : IHealthCheck
    {
        private readonly ITelemetryStatistics _statistics;
        private readonly TelemetryHealthCheckOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryHealthCheck"/> class.
        /// </summary>
        /// <param name="statistics">The telemetry statistics provider.</param>
        /// <param name="options">Optional health check configuration. Uses defaults if null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
        public TelemetryHealthCheck(
            ITelemetryStatistics statistics,
            TelemetryHealthCheckOptions? options = null)
        {
            if (statistics == null)
                throw new ArgumentNullException(nameof(statistics));

            _statistics = statistics;
            _options = options ?? TelemetryHealthCheckOptions.Default;
        }

        /// <summary>
        /// Checks the health of the telemetry system.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var snapshot = _statistics.GetSnapshot();
            var status = DetermineHealthStatus(snapshot);
            var data = BuildHealthData(snapshot);
            var description = BuildDescription(snapshot, status);

            var result = new HealthCheckResult(
                status,
                description,
                data: data);

            return Task.FromResult(result);
        }

        private HealthStatus DetermineHealthStatus(TelemetryStatisticsSnapshot snapshot)
        {
            // Check error rate
            if (snapshot.CurrentErrorRate > _options.UnhealthyErrorRateThreshold)
                return HealthStatus.Unhealthy;

            if (snapshot.CurrentErrorRate > _options.DegradedErrorRateThreshold)
                return HealthStatus.Degraded;

            // Check queue depth percentage
            if (_options.MaxExpectedQueueDepth > 0)
            {
                var queueDepthPercent = (double)snapshot.QueueDepth / _options.MaxExpectedQueueDepth * 100;

                if (queueDepthPercent > _options.UnhealthyQueueDepthPercent)
                    return HealthStatus.Unhealthy;

                if (queueDepthPercent > _options.DegradedQueueDepthPercent)
                    return HealthStatus.Degraded;
            }

            // Check dropped items
            if (snapshot.ItemsDropped > 0 && snapshot.ItemsEnqueued > 0)
            {
                var dropRate = (double)snapshot.ItemsDropped / snapshot.ItemsEnqueued * 100;

                if (dropRate > _options.UnhealthyDropRatePercent)
                    return HealthStatus.Unhealthy;

                if (dropRate > _options.DegradedDropRatePercent)
                    return HealthStatus.Degraded;
            }

            return HealthStatus.Healthy;
        }

        private static Dictionary<string, object> BuildHealthData(TelemetryStatisticsSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["uptime"] = snapshot.Uptime.ToString(),
                ["activitiesCreated"] = snapshot.ActivitiesCreated,
                ["activitiesActive"] = snapshot.ActiveActivities,
                ["queueDepth"] = snapshot.QueueDepth,
                ["maxQueueDepth"] = snapshot.MaxQueueDepth,
                ["itemsDropped"] = snapshot.ItemsDropped,
                ["errorRate"] = Math.Round(snapshot.CurrentErrorRate, 2),
                ["throughput"] = Math.Round(snapshot.CurrentThroughput, 2),
                ["processingErrors"] = snapshot.ProcessingErrors
            };
        }

        private string BuildDescription(TelemetryStatisticsSnapshot snapshot, HealthStatus status)
        {
            var sb = new StringBuilder();
            sb.Append("Telemetry system is ");
            sb.Append(status.ToString());
            sb.Append(". ");

            if (status != HealthStatus.Healthy)
            {
                if (snapshot.CurrentErrorRate > _options.DegradedErrorRateThreshold)
                {
                    sb.AppendFormat("Error rate: {0:F2}/sec. ", snapshot.CurrentErrorRate);
                }

                if (_options.MaxExpectedQueueDepth > 0)
                {
                    var queuePercent = (double)snapshot.QueueDepth / _options.MaxExpectedQueueDepth * 100;
                    if (queuePercent > _options.DegradedQueueDepthPercent)
                    {
                        sb.AppendFormat("Queue depth: {0} ({1:F0}%). ", snapshot.QueueDepth, queuePercent);
                    }
                }

                if (snapshot.ItemsDropped > 0)
                {
                    sb.AppendFormat("Items dropped: {0}. ", snapshot.ItemsDropped);
                }
            }
            else
            {
                sb.AppendFormat("Uptime: {0:F1}h, ", snapshot.Uptime.TotalHours);
                sb.AppendFormat("Throughput: {0:F0}/sec", snapshot.CurrentThroughput);
            }

            return sb.ToString();
        }
    }
}
