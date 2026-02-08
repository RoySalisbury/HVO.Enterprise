using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Root configuration for HVO.Enterprise.Telemetry.
    /// </summary>
    public sealed class TelemetryOptions
    {
        /// <summary>
        /// Gets or sets whether telemetry is enabled globally.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default sampling rate (0.0 to 1.0).
        /// </summary>
        public double DefaultSamplingRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets per-source sampling configuration.
        /// </summary>
        public Dictionary<string, SamplingOptions> Sampling { get; set; } =
            new Dictionary<string, SamplingOptions>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets logging configuration.
        /// </summary>
        public LoggingOptions Logging { get; set; } = new LoggingOptions();

        /// <summary>
        /// Gets or sets metrics configuration.
        /// </summary>
        public MetricsOptions Metrics { get; set; } = new MetricsOptions();

        /// <summary>
        /// Gets or sets background queue configuration.
        /// </summary>
        public QueueOptions Queue { get; set; } = new QueueOptions();

        /// <summary>
        /// Gets or sets feature flags.
        /// </summary>
        public FeatureFlags Features { get; set; } = new FeatureFlags();

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configuration values are outside accepted ranges.
        /// </exception>
        public void Validate()
        {
            EnsureDefaults();

            if (DefaultSamplingRate < 0.0 || DefaultSamplingRate > 1.0)
                throw new InvalidOperationException("DefaultSamplingRate must be between 0.0 and 1.0");

            if (Queue.Capacity < 100)
                throw new InvalidOperationException("Queue capacity must be at least 100");

            if (Queue.BatchSize <= 0 || Queue.BatchSize > Queue.Capacity)
                throw new InvalidOperationException("Queue batch size must be between 1 and capacity");

            if (Metrics.CollectionIntervalSeconds <= 0)
                throw new InvalidOperationException("Metrics collection interval must be greater than zero");

            foreach (var kvp in Sampling)
            {
                if (kvp.Value == null)
                    throw new InvalidOperationException("Sampling options must not be null");

                if (kvp.Value.Rate < 0.0 || kvp.Value.Rate > 1.0)
                    throw new InvalidOperationException("Sampling rate for '" + kvp.Key + "' must be between 0.0 and 1.0");
            }
        }

        private void EnsureDefaults()
        {
            Sampling ??= new Dictionary<string, SamplingOptions>(StringComparer.OrdinalIgnoreCase);
            Logging ??= new LoggingOptions();
            Metrics ??= new MetricsOptions();
            Queue ??= new QueueOptions();
            Features ??= new FeatureFlags();

            Logging.MinimumLevel ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
