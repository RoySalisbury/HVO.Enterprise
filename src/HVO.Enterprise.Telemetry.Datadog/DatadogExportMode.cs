namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Specifies the Datadog telemetry export mode.
    /// </summary>
    public enum DatadogExportMode
    {
        /// <summary>
        /// Automatically detect the export mode based on platform and configuration.
        /// Uses OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured; otherwise falls back to DogStatsD.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Use OpenTelemetry Protocol (OTLP) for trace and metric export.
        /// Requires an OpenTelemetry SDK and Datadog agent with OTLP ingest enabled.
        /// </summary>
        OTLP = 1,

        /// <summary>
        /// Use native DogStatsD protocol for metric export and manual trace enrichment.
        /// Works on all platforms including .NET Framework 4.8.
        /// </summary>
        DogStatsD = 2
    }
}
