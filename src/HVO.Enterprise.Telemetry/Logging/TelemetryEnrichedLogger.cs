using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Logger that automatically enriches log entries with Activity trace context
    /// and correlation ID via <see cref="ILogger.BeginScope{TState}"/>.
    /// </summary>
    /// <remarks>
    /// <para>This wrapper intercepts every <see cref="Log{TState}"/> call and, when
    /// enrichment is enabled, creates a scope containing TraceId, SpanId, and
    /// CorrelationId (plus any custom enricher output). The scope is passed as a
    /// <c>Dictionary&lt;string, object?&gt;</c> which all major logging providers
    /// (Serilog, NLog, Console, Application Insights) understand as structured
    /// properties.</para>
    /// <para>When no <see cref="Activity.Current"/> exists and no correlation ID is
    /// available, no scope is created and delegation is direct — zero allocation.</para>
    /// </remarks>
    internal sealed class TelemetryEnrichedLogger : ILogger
    {
        private readonly ILogger _innerLogger;
        private readonly TelemetryLoggerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryEnrichedLogger"/> class.
        /// </summary>
        /// <param name="innerLogger">The inner logger to delegate to.</param>
        /// <param name="options">Enrichment options.</param>
        internal TelemetryEnrichedLogger(ILogger innerLogger, TelemetryLoggerOptions options)
        {
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _innerLogger.BeginScope(state);

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!_innerLogger.IsEnabled(logLevel))
                return;

            if (!_options.EnableEnrichment)
            {
                _innerLogger.Log(logLevel, eventId, state, exception, formatter);
                return;
            }

            // Create enrichment scope — may return null if no context available
            using (var enrichmentScope = CreateEnrichmentScope())
            {
                _innerLogger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        /// <summary>
        /// Creates a log scope populated with Activity context, correlation ID,
        /// and custom enricher output. Returns <c>null</c> if no enrichment data
        /// is available — zero allocation fast path when no context exists.
        /// </summary>
        private IDisposable? CreateEnrichmentScope()
        {
            // Fast path: check whether any enrichment sources are present before
            // allocating the dictionary. This avoids per-log-call allocations when
            // there is no Activity, no correlation ID, and no custom enrichers.
            var activity = Activity.Current;
            var enrichers = _options.CustomEnrichersSnapshot;
            var hasCorrelation = _options.IncludeCorrelationId
                && !string.IsNullOrEmpty(CorrelationContext.GetRawValue());

            if (activity == null && !hasCorrelation && (enrichers == null || enrichers.Length == 0))
                return null;

            var enrichmentData = new Dictionary<string, object?>(8, StringComparer.Ordinal);

            // Enrich from Activity.Current — already captured above
            if (activity != null)
            {
                if (_options.IncludeTraceId)
                    enrichmentData[_options.TraceIdFieldName] = activity.TraceId.ToString();

                if (_options.IncludeSpanId)
                    enrichmentData[_options.SpanIdFieldName] = activity.SpanId.ToString();

                if (_options.IncludeParentSpanId && activity.ParentSpanId != default)
                    enrichmentData[_options.ParentSpanIdFieldName] = activity.ParentSpanId.ToString();

                if (_options.IncludeTraceFlags)
                    enrichmentData[_options.TraceFlagsFieldName] = activity.ActivityTraceFlags.ToString();

                if (_options.IncludeTraceState && !string.IsNullOrEmpty(activity.TraceStateString))
                    enrichmentData[_options.TraceStateFieldName] = activity.TraceStateString;
            }

            // Enrich from CorrelationContext
            if (hasCorrelation)
            {
                var rawCorrelationId = CorrelationContext.GetRawValue();
                if (!string.IsNullOrEmpty(rawCorrelationId))
                    enrichmentData[_options.CorrelationIdFieldName] = rawCorrelationId;
            }

            // Apply custom enrichers (immutable snapshot — safe for concurrent reads)
            if (enrichers != null)
            {
                for (int i = 0; i < enrichers.Length; i++)
                {
                    try
                    {
                        enrichers[i].Enrich(enrichmentData);
                    }
                    catch (Exception)
                    {
                        // Custom enrichers are user-supplied and may throw for any reason.
                        // We suppress to prevent enrichment failures from blocking logging.
                        // Callers can detect missing enrichment via absent properties.
                    }
                }
            }

            // Only create scope if we have data
            if (enrichmentData.Count > 0)
                return _innerLogger.BeginScope(new LogEnrichmentScope(enrichmentData));

            return null;
        }
    }
}
