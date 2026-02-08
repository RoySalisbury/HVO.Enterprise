using System;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Configures telemetry for a type or method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TelemetryConfigurationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether telemetry is enabled.
        /// </summary>
        public ConfigurationToggle Enabled { get; set; } = ConfigurationToggle.Inherit;

        /// <summary>
        /// Gets or sets the sampling rate (0.0 to 1.0). Use NaN to inherit.
        /// </summary>
        public double SamplingRate { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets parameter capture mode.
        /// </summary>
        public ParameterCaptureMode ParameterCapture { get; set; } = ParameterCaptureMode.Unspecified;

        /// <summary>
        /// Gets or sets whether to record exceptions.
        /// </summary>
        public ConfigurationToggle RecordExceptions { get; set; } = ConfigurationToggle.Inherit;

        /// <summary>
        /// Gets or sets timeout threshold in milliseconds. Use 0 to inherit.
        /// </summary>
        public int TimeoutThresholdMs { get; set; }

        /// <summary>
        /// Converts attribute to <see cref="OperationConfiguration"/>.
        /// </summary>
        /// <returns>Operation configuration based on attribute values.</returns>
        public OperationConfiguration ToConfiguration()
        {
            var configuration = new OperationConfiguration();

            if (!double.IsNaN(SamplingRate))
                configuration.SamplingRate = SamplingRate;

            if (Enabled != ConfigurationToggle.Inherit)
                configuration.Enabled = Enabled == ConfigurationToggle.Enabled;

            if (ParameterCapture != ParameterCaptureMode.Unspecified)
                configuration.ParameterCapture = ParameterCapture;

            if (RecordExceptions != ConfigurationToggle.Inherit)
                configuration.RecordExceptions = RecordExceptions == ConfigurationToggle.Enabled;

            if (TimeoutThresholdMs > 0)
                configuration.TimeoutThresholdMs = TimeoutThresholdMs;

            return configuration;
        }
    }
}
