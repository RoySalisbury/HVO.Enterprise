using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Interface for custom log enrichment.
    /// Implementations add domain-specific properties to log entries.
    /// </summary>
    /// <remarks>
    /// <para>Enrichers are invoked on every log call where enrichment is enabled.
    /// Implementations must be thread-safe and should avoid expensive operations
    /// (target &lt;1μs per call). Exceptions thrown by enrichers are caught and
    /// silently suppressed to prevent enrichment failures from affecting logging.</para>
    /// <para>For future extensibility, US-023 (Serilog) and US-024 (App Insights)
    /// will provide adapter enrichers that bridge to provider-specific enrichment.</para>
    /// </remarks>
    public interface ILogEnricher
    {
        /// <summary>
        /// Enriches the log entry with additional properties.
        /// </summary>
        /// <param name="properties">
        /// The mutable properties dictionary that will be passed to
        /// <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope{TState}"/>.
        /// Add key-value pairs to include them as structured log properties.
        /// </param>
        void Enrich(IDictionary<string, object?> properties);
    }
}
