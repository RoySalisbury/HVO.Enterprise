namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Background queue configuration.
    /// </summary>
    public sealed class QueueOptions
    {
        /// <summary>
        /// Gets or sets the queue capacity.
        /// </summary>
        public int Capacity { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum batch size.
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}
