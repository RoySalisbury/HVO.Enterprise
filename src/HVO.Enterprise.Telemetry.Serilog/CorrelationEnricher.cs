using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using Serilog.Core;
using Serilog.Events;

namespace HVO.Enterprise.Telemetry.Serilog
{
    /// <summary>
    /// Enriches Serilog log events with correlation ID from <see cref="CorrelationContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads from <see cref="CorrelationContext.Current"/> and adds the correlation ID to log events.
    /// When no explicit correlation ID is set and <see cref="FallbackToActivity"/> is <see langword="true"/>,
    /// falls back to <see cref="Activity.Current"/> TraceId (W3C) or RootId (hierarchical).
    /// </para>
    /// <para>
    /// Thread-safe and AsyncLocal-aware — correlation IDs flow correctly across async/await boundaries.
    /// </para>
    /// <para>
    /// This enricher reads the raw AsyncLocal value directly (without triggering auto-generation)
    /// to distinguish explicit correlation IDs from fallback values. When an explicit value is set
    /// (via <see cref="CorrelationContext.BeginScope"/> or direct assignment), it is always used.
    /// When no explicit value is set and <see cref="FallbackToActivity"/> is <see langword="true"/>,
    /// falls back to <see cref="CorrelationContext.Current"/> (Activity TraceId or auto-generated).
    /// When no explicit value is set and fallback is disabled, no property is added.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Log.Logger = new LoggerConfiguration()
    ///     .Enrich.WithCorrelation()
    ///     .WriteTo.Console()
    ///     .CreateLogger();
    ///
    /// using (CorrelationContext.BeginScope("my-correlation-id"))
    /// {
    ///     Log.Information("This log will include CorrelationId=my-correlation-id");
    /// }
    /// </code>
    /// </example>
    public sealed class CorrelationEnricher : ILogEventEnricher
    {
        /// <summary>
        /// The default zero-value W3C TraceId (32 hex zeroes).
        /// </summary>
        private const string ZeroTraceId = "00000000000000000000000000000000";

        private readonly string _propertyName;
        private readonly bool _fallbackToActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationEnricher"/> class.
        /// </summary>
        /// <param name="propertyName">Property name for correlation ID. Default: <c>"CorrelationId"</c>.</param>
        /// <param name="fallbackToActivity">
        /// Whether to use Activity TraceId if no explicit correlation is set. Default: <see langword="true"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.
        /// </exception>
        public CorrelationEnricher(
            string propertyName = "CorrelationId",
            bool fallbackToActivity = true)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            _propertyName = propertyName;
            _fallbackToActivity = fallbackToActivity;
        }

        /// <summary>
        /// Gets a value indicating whether this enricher will fall back to Activity TraceId
        /// when no explicit correlation ID is available.
        /// </summary>
        public bool FallbackToActivity => _fallbackToActivity;

        /// <inheritdoc />
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }
            if (propertyFactory == null)
            {
                throw new ArgumentNullException(nameof(propertyFactory));
            }

            // Use GetRawValue() to check if an explicit correlation was set (AsyncLocal only,
            // no fallback or auto-generation). This lets us distinguish explicit values from
            // Activity-derived or auto-generated ones.
            var rawValue = CorrelationContext.GetRawValue();

            if (rawValue != null)
            {
                // Explicit AsyncLocal value — always use it regardless of fallback setting.
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(_propertyName, rawValue));
                return;
            }

            if (!_fallbackToActivity)
            {
                // No explicit correlation and fallback is disabled — skip enrichment.
                return;
            }

            // Fallback: read the full three-tier value (AsyncLocal → Activity → auto-gen).
            // Since rawValue was null, this will either pick up Activity.TraceId or auto-generate.
            var correlationId = CorrelationContext.Current;

            if (!string.IsNullOrEmpty(correlationId))
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(_propertyName, correlationId));
            }
        }
    }
}
