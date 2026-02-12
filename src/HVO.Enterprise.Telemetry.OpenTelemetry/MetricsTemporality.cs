namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Metrics aggregation temporality preference.
    /// </summary>
    public enum MetricsTemporality
    {
        /// <summary>Cumulative temporality (default for Prometheus, OTLP).</summary>
        Cumulative = 0,

        /// <summary>Delta temporality (preferred by Datadog, Lightstep).</summary>
        Delta = 1
    }
}
