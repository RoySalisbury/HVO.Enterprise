namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Metrics configuration.
    /// </summary>
    public sealed class MetricsOptions
    {
        /// <summary>
        /// Gets or sets whether metrics collection is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets metrics collection interval in seconds.
        /// </summary>
        public int CollectionIntervalSeconds { get; set; } = 10;
    }
}
