using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Instrumentation
{
    /// <summary>
    /// Propagates W3C TraceContext (traceparent / tracestate) through RabbitMQ message headers.
    /// Injects context before publishing and extracts context when consuming.
    /// </summary>
    public static class RabbitMqHeaderPropagator
    {
        /// <summary>
        /// The W3C traceparent header name.
        /// </summary>
        public const string TraceparentHeader = "traceparent";

        /// <summary>
        /// The W3C tracestate header name.
        /// </summary>
        public const string TracestateHeader = "tracestate";

        /// <summary>
        /// Injects the current <see cref="Activity"/> context into message headers.
        /// If no headers dictionary is provided, a new one is created.
        /// </summary>
        /// <param name="headers">The message headers dictionary. Created if null.</param>
        /// <param name="activity">
        /// The activity whose context to inject. If null, <see cref="Activity.Current"/> is used.
        /// </param>
        /// <returns>The headers dictionary with trace context injected.</returns>
        public static IDictionary<string, object> Inject(
            IDictionary<string, object>? headers,
            Activity? activity = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, object>();
            }

            var source = activity ?? Activity.Current;
            if (source == null)
            {
                return headers;
            }

            // Build traceparent: 00-<trace-id>-<span-id>-<flags>
            string traceparent = string.Format(
                CultureInfo.InvariantCulture,
                "00-{0}-{1}-{2}",
                source.TraceId.ToHexString(),
                source.SpanId.ToHexString(),
                (source.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0 ? "01" : "00");

            headers[TraceparentHeader] = Encoding.UTF8.GetBytes(traceparent);

            if (!string.IsNullOrEmpty(source.TraceStateString))
            {
                headers[TracestateHeader] = Encoding.UTF8.GetBytes(source.TraceStateString);
            }

            return headers;
        }

        /// <summary>
        /// Extracts trace context from message headers and returns an <see cref="ActivityContext"/>
        /// that can be used as a parent for a new activity.
        /// </summary>
        /// <param name="headers">The message headers dictionary.</param>
        /// <returns>
        /// An <see cref="ActivityContext"/> if valid traceparent was found; otherwise <c>default</c>.
        /// </returns>
        public static ActivityContext Extract(IDictionary<string, object>? headers)
        {
            if (headers == null)
            {
                return default;
            }

            string? traceparent = GetHeaderString(headers, TraceparentHeader);
            if (string.IsNullOrEmpty(traceparent))
            {
                return default;
            }

            return ParseTraceparent(traceparent, GetHeaderString(headers, TracestateHeader));
        }

        /// <summary>
        /// Extracts a header value as a string from the headers dictionary.
        /// Supports both <see cref="string"/> and <see cref="T:byte[]"/> header values.
        /// </summary>
        /// <param name="headers">The message headers.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The header value as a string, or null if not found.</returns>
        internal static string? GetHeaderString(IDictionary<string, object> headers, string key)
        {
            if (!headers.TryGetValue(key, out object? value) || value == null)
            {
                return null;
            }

            if (value is string stringValue)
            {
                return stringValue;
            }

            if (value is byte[] bytes)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return value.ToString();
        }

        /// <summary>
        /// Parses a W3C traceparent header value into an <see cref="ActivityContext"/>.
        /// Expected format: <c>00-{traceId}-{spanId}-{flags}</c>.
        /// </summary>
        /// <param name="traceparent">The traceparent header value.</param>
        /// <param name="tracestate">Optional tracestate header value.</param>
        /// <returns>
        /// A valid <see cref="ActivityContext"/> if parsing succeeds; otherwise <c>default</c>.
        /// </returns>
        internal static ActivityContext ParseTraceparent(string? traceparent, string? tracestate)
        {
            if (string.IsNullOrEmpty(traceparent))
            {
                return default;
            }

            // Format: "00-<32 hex trace id>-<16 hex span id>-<2 hex flags>"
            // Minimum length: 2 + 1 + 32 + 1 + 16 + 1 + 2 = 55
            string[] parts = traceparent!.Split('-');
            if (parts.Length < 4)
            {
                return default;
            }

            // Validate version
            if (parts[0] != "00")
            {
                return default;
            }

            // Parse trace ID (32 hex chars)
            if (parts[1].Length != 32)
            {
                return default;
            }

            // Parse span ID (16 hex chars)
            if (parts[2].Length != 16)
            {
                return default;
            }

            // Parse flags (2 hex chars)
            if (parts[3].Length < 2)
            {
                return default;
            }

            try
            {
                var traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
                var spanId = ActivitySpanId.CreateFromString(parts[2].AsSpan());
                var flags = parts[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;

                return new ActivityContext(traceId, spanId, flags, tracestate);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
