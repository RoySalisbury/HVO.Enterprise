using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Initializes Application Insights telemetry with Activity tracing context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Propagates W3C TraceContext from <see cref="Activity.Current"/> to Application Insights telemetry,
    /// ensuring proper distributed tracing correlation. Supports both W3C and hierarchical Activity ID formats.
    /// </para>
    /// <para>
    /// For <see cref="ISupportProperties"/> telemetry items, Activity tags are copied to custom properties
    /// and baggage items are prefixed with <c>"baggage."</c>.
    /// </para>
    /// <para>
    /// Thread-safe and AsyncLocal-aware — Activity context flows correctly across async/await boundaries.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var configuration = TelemetryConfiguration.CreateDefault();
    /// configuration.TelemetryInitializers.Add(new ActivityTelemetryInitializer());
    /// </code>
    /// </example>
    public sealed class ActivityTelemetryInitializer : ITelemetryInitializer
    {
        private const string ZeroTraceId = "00000000000000000000000000000000";
        private const string ZeroSpanId = "0000000000000000";

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                EnrichFromW3C(telemetry, activity);
            }
            else if (!string.IsNullOrEmpty(activity.Id))
            {
                EnrichFromHierarchical(telemetry, activity);
            }

            CopyTagsAndBaggage(telemetry, activity);
        }

        /// <summary>
        /// Enriches telemetry with W3C TraceContext from the Activity.
        /// </summary>
        private static void EnrichFromW3C(ITelemetry telemetry, Activity activity)
        {
            var traceId = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();

            if (!string.IsNullOrEmpty(traceId) && traceId != ZeroTraceId)
            {
                telemetry.Context.Operation.Id = traceId;
            }

            if (telemetry is OperationTelemetry operationTelemetry)
            {
                if (!string.IsNullOrEmpty(spanId) && spanId != ZeroSpanId)
                {
                    operationTelemetry.Id = spanId;
                }

                var parentSpanId = activity.ParentSpanId.ToString();
                if (!string.IsNullOrEmpty(parentSpanId) && parentSpanId != ZeroSpanId)
                {
                    operationTelemetry.Context.Operation.ParentId = parentSpanId;
                }
            }

            // Add W3C tracestate if present
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                var supportProperties = telemetry as ISupportProperties;
                if (supportProperties != null && !supportProperties.Properties.ContainsKey("tracestate"))
                {
                    supportProperties.Properties["tracestate"] = activity.TraceStateString;
                }
            }
        }

        /// <summary>
        /// Enriches telemetry with hierarchical Activity ID format (legacy fallback).
        /// </summary>
        private static void EnrichFromHierarchical(ITelemetry telemetry, Activity activity)
        {
            telemetry.Context.Operation.Id = activity.RootId ?? activity.Id;

            if (telemetry is OperationTelemetry operationTelemetry)
            {
                operationTelemetry.Id = activity.Id;

                if (!string.IsNullOrEmpty(activity.ParentId))
                {
                    operationTelemetry.Context.Operation.ParentId = activity.ParentId;
                }
            }
        }

        /// <summary>
        /// Copies Activity tags and baggage to telemetry custom properties.
        /// </summary>
        private static void CopyTagsAndBaggage(ITelemetry telemetry, Activity activity)
        {
            var supportProperties = telemetry as ISupportProperties;
            if (supportProperties == null)
            {
                return;
            }

            // Copy Activity tags to custom properties
            foreach (var tag in activity.Tags)
            {
                if (!string.IsNullOrEmpty(tag.Key) && !supportProperties.Properties.ContainsKey(tag.Key))
                {
                    supportProperties.Properties[tag.Key] = tag.Value ?? string.Empty;
                }
            }

            // Copy Activity baggage with "baggage." prefix
            foreach (var baggage in activity.Baggage)
            {
                if (!string.IsNullOrEmpty(baggage.Key))
                {
                    var key = "baggage." + baggage.Key;
                    if (!supportProperties.Properties.ContainsKey(key))
                    {
                        supportProperties.Properties[key] = baggage.Value ?? string.Empty;
                    }
                }
            }
        }
    }
}
