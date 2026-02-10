using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Initializes Application Insights telemetry with correlation ID from <see cref="CorrelationContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="CorrelationContext.TryGetExplicitCorrelationId"/> to check for explicitly set
    /// correlation IDs without triggering auto-generation. When an explicit value is set (via
    /// <see cref="CorrelationContext.BeginScope"/> or direct assignment to <see cref="CorrelationContext.Current"/>),
    /// it is always used.
    /// </para>
    /// <para>
    /// When no explicit value is set and <see cref="FallbackToActivity"/> is <see langword="true"/> (default),
    /// derives the correlation from <see cref="Activity.Current"/> directly (W3C TraceId or hierarchical
    /// RootId/Id), avoiding the all-zeroes TraceId issue for hierarchical Activities.
    /// </para>
    /// <para>
    /// Thread-safe and AsyncLocal-aware — correlation IDs flow correctly across async/await boundaries.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var configuration = TelemetryConfiguration.CreateDefault();
    /// configuration.TelemetryInitializers.Add(new CorrelationTelemetryInitializer());
    /// </code>
    /// </example>
    public sealed class CorrelationTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Default property name used for the correlation ID in telemetry custom properties.
        /// </summary>
        public const string DefaultPropertyName = "CorrelationId";

        private const string ZeroTraceId = "00000000000000000000000000000000";

        private readonly string _propertyName;
        private readonly bool _fallbackToActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="propertyName">
        /// Property name for correlation ID in telemetry custom properties. Default: <c>"CorrelationId"</c>.
        /// </param>
        /// <param name="fallbackToActivity">
        /// Whether to use <see cref="Activity.Current"/> TraceId if no explicit correlation is set. Default: <see langword="true"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.
        /// </exception>
        public CorrelationTelemetryInitializer(
            string propertyName = DefaultPropertyName,
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
        /// Gets a value indicating whether this initializer will fall back to <see cref="Activity.Current"/>
        /// TraceId when no explicit correlation ID is available.
        /// </summary>
        public bool FallbackToActivity => _fallbackToActivity;

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var supportProperties = telemetry as ISupportProperties;
            if (supportProperties == null)
            {
                return;
            }

            // Already has a correlation ID — don't overwrite
            if (supportProperties.Properties.ContainsKey(_propertyName))
            {
                return;
            }

            // Use TryGetExplicitCorrelationId to check if an explicit correlation was set
            // without triggering auto-generation.
            if (CorrelationContext.TryGetExplicitCorrelationId(out var explicitId))
            {
                supportProperties.Properties[_propertyName] = explicitId!;
                return;
            }

            if (!_fallbackToActivity)
            {
                return;
            }

            // Fallback: derive correlation from Activity.Current directly.
            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            string? correlationId;
            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                var traceId = activity.TraceId.ToString();
                correlationId = (traceId != ZeroTraceId) ? traceId : null;
            }
            else
            {
                // Hierarchical format: prefer RootId, fall back to Id
                correlationId = activity.RootId ?? activity.Id;
            }

            if (!string.IsNullOrEmpty(correlationId))
            {
                supportProperties.Properties[_propertyName] = correlationId;
            }
        }
    }
}
