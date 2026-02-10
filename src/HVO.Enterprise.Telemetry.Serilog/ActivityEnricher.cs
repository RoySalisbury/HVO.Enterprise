using System;
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace HVO.Enterprise.Telemetry.Serilog
{
    /// <summary>
    /// Enriches Serilog log events with Activity tracing information (TraceId, SpanId, ParentId).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads from <see cref="Activity.Current"/> and adds W3C TraceContext properties to log events.
    /// Also supports hierarchical Activity ID format as a fallback for legacy systems.
    /// </para>
    /// <para>
    /// Gracefully handles missing Activity context — when no Activity is active, no properties are added
    /// and no exceptions are thrown.
    /// </para>
    /// <para>
    /// Property names are customizable to match different log schema conventions
    /// (e.g., <c>"trace_id"</c> for snake_case schemas, or <c>"TraceId"</c> for PascalCase).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Log.Logger = new LoggerConfiguration()
    ///     .Enrich.WithActivity()
    ///     .WriteTo.Console()
    ///     .CreateLogger();
    /// </code>
    /// </example>
    public sealed class ActivityEnricher : ILogEventEnricher
    {
        /// <summary>
        /// The default zero-value W3C TraceId (32 hex zeroes).
        /// </summary>
        private const string ZeroTraceId = "00000000000000000000000000000000";

        /// <summary>
        /// The default zero-value W3C SpanId (16 hex zeroes).
        /// </summary>
        private const string ZeroSpanId = "0000000000000000";

        private readonly string _traceIdPropertyName;
        private readonly string _spanIdPropertyName;
        private readonly string _parentIdPropertyName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEnricher"/> class with default property names.
        /// </summary>
        /// <param name="traceIdPropertyName">Property name for TraceId. Default: <c>"TraceId"</c>.</param>
        /// <param name="spanIdPropertyName">Property name for SpanId. Default: <c>"SpanId"</c>.</param>
        /// <param name="parentIdPropertyName">Property name for ParentId. Default: <c>"ParentId"</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any property name is <see langword="null"/> or empty.
        /// </exception>
        public ActivityEnricher(
            string traceIdPropertyName = "TraceId",
            string spanIdPropertyName = "SpanId",
            string parentIdPropertyName = "ParentId")
        {
            if (string.IsNullOrEmpty(traceIdPropertyName))
            {
                throw new ArgumentNullException(nameof(traceIdPropertyName));
            }
            if (string.IsNullOrEmpty(spanIdPropertyName))
            {
                throw new ArgumentNullException(nameof(spanIdPropertyName));
            }
            if (string.IsNullOrEmpty(parentIdPropertyName))
            {
                throw new ArgumentNullException(nameof(parentIdPropertyName));
            }

            _traceIdPropertyName = traceIdPropertyName;
            _spanIdPropertyName = spanIdPropertyName;
            _parentIdPropertyName = parentIdPropertyName;
        }

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

            // Cache Activity.Current to avoid race conditions between reads
            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                EnrichFromW3C(logEvent, propertyFactory, activity);
            }
            else if (!string.IsNullOrEmpty(activity.Id))
            {
                EnrichFromHierarchical(logEvent, propertyFactory, activity);
            }
        }

        /// <summary>
        /// Enriches from W3C TraceContext format Activity.
        /// </summary>
        private void EnrichFromW3C(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, Activity activity)
        {
            var traceId = activity.TraceId.ToString();
            if (!string.IsNullOrEmpty(traceId) && traceId != ZeroTraceId)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(_traceIdPropertyName, traceId));
            }

            var spanId = activity.SpanId.ToString();
            if (!string.IsNullOrEmpty(spanId) && spanId != ZeroSpanId)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(_spanIdPropertyName, spanId));
            }

            var parentSpanId = activity.ParentSpanId.ToString();
            if (!string.IsNullOrEmpty(parentSpanId) && parentSpanId != ZeroSpanId)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(_parentIdPropertyName, parentSpanId));
            }
        }

        /// <summary>
        /// Enriches from hierarchical Activity ID format (legacy fallback).
        /// </summary>
        private void EnrichFromHierarchical(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, Activity activity)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(_traceIdPropertyName, activity.RootId ?? activity.Id));
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(_spanIdPropertyName, activity.Id));

            if (!string.IsNullOrEmpty(activity.ParentId))
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(_parentIdPropertyName, activity.ParentId));
            }
        }
    }
}
