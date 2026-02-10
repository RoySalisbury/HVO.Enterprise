namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Configuration options for Application Insights integration with HVO telemetry.
    /// </summary>
    public sealed class AppInsightsOptions
    {
        /// <summary>
        /// Gets or sets the Application Insights connection string.
        /// </summary>
        /// <remarks>
        /// Preferred over <see cref="InstrumentationKey"/> for new applications.
        /// Format: <c>InstrumentationKey=...;IngestionEndpoint=...</c>
        /// </remarks>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Application Insights instrumentation key (legacy).
        /// </summary>
        /// <remarks>
        /// Use <see cref="ConnectionString"/> for new applications.
        /// If both are set, <see cref="ConnectionString"/> takes precedence.
        /// </remarks>
        public string? InstrumentationKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable the dual-mode bridge.
        /// Default: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// When enabled, the bridge detects whether OpenTelemetry OTLP exporters are configured
        /// and operates in the appropriate mode (OTLP or Direct).
        /// </remarks>
        public bool EnableBridge { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to register the <see cref="ActivityTelemetryInitializer"/>.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableActivityInitializer { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to register the <see cref="CorrelationTelemetryInitializer"/>.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableCorrelationInitializer { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="CorrelationTelemetryInitializer"/>
        /// should fall back to <see cref="System.Diagnostics.Activity.Current"/> TraceId when no
        /// explicit correlation ID is set. Default: <see langword="true"/>.
        /// </summary>
        public bool CorrelationFallbackToActivity { get; set; } = true;

        /// <summary>
        /// Gets or sets the property name used for the correlation ID in telemetry custom properties.
        /// Default: <c>"CorrelationId"</c>.
        /// </summary>
        public string CorrelationPropertyName { get; set; } = CorrelationTelemetryInitializer.DefaultPropertyName;

        /// <summary>
        /// Gets or sets a value indicating whether to force a specific bridge mode.
        /// When <see langword="null"/>, the mode is auto-detected. Default: <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set to <see langword="true"/> to force OTLP mode or <see langword="false"/> to force Direct mode.
        /// </remarks>
        public bool? ForceOtlpMode { get; set; }

        /// <summary>
        /// Returns the effective connection string, falling back to the instrumentation key if needed.
        /// </summary>
        /// <returns>The connection string, or <see langword="null"/> if neither is configured.</returns>
        internal string? GetEffectiveConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                return ConnectionString;
            }

            if (!string.IsNullOrEmpty(InstrumentationKey))
            {
                return "InstrumentationKey=" + InstrumentationKey;
            }

            return null;
        }
    }
}
