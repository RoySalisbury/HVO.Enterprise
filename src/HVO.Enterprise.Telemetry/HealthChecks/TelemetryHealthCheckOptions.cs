using System;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// Configuration options for <see cref="TelemetryHealthCheck"/>.
    /// Controls the thresholds that determine when the telemetry system is
    /// considered degraded or unhealthy.
    /// </summary>
    public sealed class TelemetryHealthCheckOptions
    {
        /// <summary>
        /// Default options with sensible thresholds for most applications.
        /// </summary>
        public static readonly TelemetryHealthCheckOptions Default = new TelemetryHealthCheckOptions();

        /// <summary>
        /// Gets or sets the error rate (errors/sec) above which system is considered degraded.
        /// Default is 1.0 error per second.
        /// </summary>
        public double DegradedErrorRateThreshold { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the error rate (errors/sec) above which system is considered unhealthy.
        /// Default is 10.0 errors per second.
        /// </summary>
        public double UnhealthyErrorRateThreshold { get; set; } = 10.0;

        /// <summary>
        /// Gets or sets the maximum expected queue depth for percentage calculations.
        /// Default is 10,000 (matching the default bounded queue capacity).
        /// </summary>
        public int MaxExpectedQueueDepth { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the queue depth percentage above which system is considered degraded.
        /// Default is 75%.
        /// </summary>
        public double DegradedQueueDepthPercent { get; set; } = 75.0;

        /// <summary>
        /// Gets or sets the queue depth percentage above which system is considered unhealthy.
        /// Default is 95%.
        /// </summary>
        public double UnhealthyQueueDepthPercent { get; set; } = 95.0;

        /// <summary>
        /// Gets or sets the dropped item percentage above which system is considered degraded.
        /// Calculated as (dropped / enqueued * 100). Default is 0.1%.
        /// </summary>
        public double DegradedDropRatePercent { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the dropped item percentage above which system is considered unhealthy.
        /// Calculated as (dropped / enqueued * 100). Default is 1.0%.
        /// </summary>
        public double UnhealthyDropRatePercent { get; set; } = 1.0;

        /// <summary>
        /// Validates the configuration options and throws if invalid.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any threshold is invalid.</exception>
        public void Validate()
        {
            if (DegradedErrorRateThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(DegradedErrorRateThreshold), "Must be non-negative.");

            if (UnhealthyErrorRateThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(UnhealthyErrorRateThreshold), "Must be non-negative.");

            if (UnhealthyErrorRateThreshold < DegradedErrorRateThreshold)
                throw new ArgumentOutOfRangeException(nameof(UnhealthyErrorRateThreshold), "Must be >= degraded threshold.");

            if (MaxExpectedQueueDepth <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxExpectedQueueDepth), "Must be positive.");

            if (DegradedQueueDepthPercent < 0 || DegradedQueueDepthPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(DegradedQueueDepthPercent), "Must be between 0 and 100.");

            if (UnhealthyQueueDepthPercent < 0 || UnhealthyQueueDepthPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(UnhealthyQueueDepthPercent), "Must be between 0 and 100.");

            if (UnhealthyQueueDepthPercent < DegradedQueueDepthPercent)
                throw new ArgumentOutOfRangeException(nameof(UnhealthyQueueDepthPercent), "Must be >= degraded threshold.");

            if (DegradedDropRatePercent < 0 || DegradedDropRatePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(DegradedDropRatePercent), "Must be between 0 and 100.");

            if (UnhealthyDropRatePercent < 0 || UnhealthyDropRatePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(UnhealthyDropRatePercent), "Must be between 0 and 100.");

            if (UnhealthyDropRatePercent < DegradedDropRatePercent)
                throw new ArgumentOutOfRangeException(nameof(UnhealthyDropRatePercent), "Must be >= degraded threshold.");
        }
    }
}
