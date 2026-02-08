namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Sampling configuration for an ActivitySource.
    /// </summary>
    public sealed class SamplingOptions
    {
        /// <summary>
        /// Gets or sets the sampling rate (0.0 to 1.0).
        /// </summary>
        public double Rate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets whether to always sample errors.
        /// </summary>
        public bool AlwaysSampleErrors { get; set; } = true;
    }
}
