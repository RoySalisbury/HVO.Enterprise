using System.Collections.Generic;
using System.Linq;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Options for configuring telemetry logger enrichment.
    /// </summary>
    /// <remarks>
    /// These options control which Activity and correlation fields are automatically
    /// injected into log scopes. Field names are configurable to match the naming
    /// conventions of your log aggregation platform (e.g., Datadog uses lowercase
    /// with underscores, Application Insights uses PascalCase).
    /// </remarks>
    public sealed class TelemetryLoggerOptions
    {
        /// <summary>
        /// Gets or sets whether to enable automatic enrichment.
        /// When <c>false</c>, the enriched logger delegates directly to the inner logger
        /// with zero overhead beyond a boolean check.
        /// </summary>
        public bool EnableEnrichment { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include <see cref="System.Diagnostics.Activity.TraceId"/> in log scope.
        /// </summary>
        public bool IncludeTraceId { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include <see cref="System.Diagnostics.Activity.SpanId"/> in log scope.
        /// </summary>
        public bool IncludeSpanId { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include <see cref="System.Diagnostics.Activity.ParentSpanId"/> in log scope.
        /// </summary>
        public bool IncludeParentSpanId { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include <see cref="System.Diagnostics.Activity.ActivityTraceFlags"/> in log scope.
        /// </summary>
        public bool IncludeTraceFlags { get; set; }

        /// <summary>
        /// Gets or sets whether to include <see cref="System.Diagnostics.Activity.TraceStateString"/> in log scope.
        /// </summary>
        public bool IncludeTraceState { get; set; }

        /// <summary>
        /// Gets or sets whether to include the correlation ID in the log scope.
        /// </summary>
        /// <remarks>
        /// The correlation ID is taken from <see cref="Correlation.CorrelationContext"/> only when a value
        /// has been explicitly set (via its raw value accessor). The underlying implementation intentionally
        /// uses the raw value (e.g., <c>CorrelationContext.GetRawValue()</c>) and does not trigger any
        /// fallback or auto-generation behavior. If no explicit correlation ID is present, no correlation
        /// identifier field is added to the log scope even when this option is <c>true</c>.
        /// </remarks>
        public bool IncludeCorrelationId { get; set; } = true;

        /// <summary>
        /// Gets or sets the field name for TraceId in the log scope.
        /// </summary>
        public string TraceIdFieldName { get; set; } = "TraceId";

        /// <summary>
        /// Gets or sets the field name for SpanId in the log scope.
        /// </summary>
        public string SpanIdFieldName { get; set; } = "SpanId";

        /// <summary>
        /// Gets or sets the field name for ParentSpanId in the log scope.
        /// </summary>
        public string ParentSpanIdFieldName { get; set; } = "ParentSpanId";

        /// <summary>
        /// Gets or sets the field name for TraceFlags in the log scope.
        /// </summary>
        public string TraceFlagsFieldName { get; set; } = "TraceFlags";

        /// <summary>
        /// Gets or sets the field name for TraceState in the log scope.
        /// </summary>
        public string TraceStateFieldName { get; set; } = "TraceState";

        /// <summary>
        /// Gets or sets the field name for CorrelationId in the log scope.
        /// </summary>
        public string CorrelationIdFieldName { get; set; } = "CorrelationId";

        /// <summary>
        /// Gets or sets the list of custom enrichers to apply to log entries.
        /// </summary>
        /// <remarks>
        /// <para>Custom enrichers are invoked after the built-in Activity and correlation
        /// enrichment. Enricher exceptions are silently suppressed to prevent
        /// enrichment failures from affecting logging.</para>
        /// <para>When this list is set, an immutable snapshot (array) is taken
        /// immediately and used for all subsequent log calls. Mutating the list
        /// after registration has no effect — set this property again to update.</para>
        /// </remarks>
        public List<ILogEnricher>? CustomEnrichers
        {
            get => _customEnrichers;
            set
            {
                _customEnrichers = value;
                // Take an immutable snapshot so the hot-path iteration in
                // TelemetryEnrichedLogger is safe against concurrent mutation.
                CustomEnrichersSnapshot = value?.ToArray();
            }
        }

        /// <summary>
        /// Immutable snapshot of <see cref="CustomEnrichers"/> used on the logging hot path.
        /// </summary>
        internal ILogEnricher[]? CustomEnrichersSnapshot { get; private set; }

        private List<ILogEnricher>? _customEnrichers;
    }
}
