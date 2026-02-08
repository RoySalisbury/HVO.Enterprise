namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Feature flags for telemetry features.
    /// </summary>
    public sealed class FeatureFlags
    {
        /// <summary>
        /// Gets or sets whether automatic HTTP instrumentation is enabled.
        /// </summary>
        public bool EnableHttpInstrumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether DispatchProxy instrumentation is enabled.
        /// </summary>
        public bool EnableProxyInstrumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether exception tracking is enabled.
        /// </summary>
        public bool EnableExceptionTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether parameter capture is enabled.
        /// </summary>
        public bool EnableParameterCapture { get; set; } = false;
    }
}
