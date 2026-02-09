using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Wcf.Propagation
{
    /// <summary>
    /// Extracts and injects W3C Trace Context from/to string representations
    /// for SOAP header propagation.
    /// </summary>
    /// <remarks>
    /// Implements the W3C Trace Context specification for traceparent and tracestate headers.
    /// See: https://www.w3.org/TR/trace-context/
    /// </remarks>
    public static class W3CTraceContextPropagator
    {
        /// <summary>
        /// Parses a W3C traceparent header string into its constituent parts.
        /// </summary>
        /// <param name="traceparent">
        /// The traceparent header value. Format: <c>00-{trace-id}-{parent-id}-{trace-flags}</c>
        /// </param>
        /// <param name="traceId">When this method returns, contains the 32-character hex trace ID.</param>
        /// <param name="spanId">When this method returns, contains the 16-character hex span ID.</param>
        /// <param name="traceFlags">When this method returns, contains the parsed trace flags.</param>
        /// <returns><c>true</c> if the traceparent was parsed successfully; <c>false</c> otherwise.</returns>
        public static bool TryParseTraceParent(
            string? traceparent,
            out string traceId,
            out string spanId,
            out ActivityTraceFlags traceFlags)
        {
            traceId = string.Empty;
            spanId = string.Empty;
            traceFlags = ActivityTraceFlags.None;

            if (string.IsNullOrWhiteSpace(traceparent))
                return false;

            var parts = traceparent!.Split('-');
            if (parts.Length != TraceContextConstants.TraceParentPartCount)
                return false;

            // Version must be "00"
            if (parts[0] != TraceContextConstants.TraceParentVersion)
                return false;

            // Validate trace-id (32 hex chars, not all zeros)
            if (parts[1].Length != TraceContextConstants.TraceIdHexLength || !IsValidHex(parts[1]))
                return false;
            if (IsAllZeros(parts[1]))
                return false;

            // Validate parent-id / span-id (16 hex chars, not all zeros)
            if (parts[2].Length != TraceContextConstants.SpanIdHexLength || !IsValidHex(parts[2]))
                return false;
            if (IsAllZeros(parts[2]))
                return false;

            // Parse trace-flags (2 hex chars)
            if (parts[3].Length != TraceContextConstants.TraceFlagsHexLength || !IsValidHex(parts[3]))
                return false;

            traceId = parts[1];
            spanId = parts[2];

            if (byte.TryParse(parts[3], System.Globalization.NumberStyles.HexNumber, null, out var flags))
            {
                traceFlags = (ActivityTraceFlags)flags;
            }

            return true;
        }

        /// <summary>
        /// Creates a W3C traceparent header string from an <see cref="Activity"/>.
        /// </summary>
        /// <param name="activity">The activity to extract trace context from.</param>
        /// <returns>The formatted traceparent header string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> is null.</exception>
        public static string CreateTraceParent(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            return string.Format(
                "{0}-{1}-{2}-{3:x2}",
                TraceContextConstants.TraceParentVersion,
                activity.TraceId.ToHexString(),
                activity.SpanId.ToHexString(),
                (byte)activity.ActivityTraceFlags);
        }

        /// <summary>
        /// Creates a W3C traceparent header string from explicit trace components.
        /// </summary>
        /// <param name="traceId">The 32-character hex trace ID.</param>
        /// <param name="spanId">The 16-character hex span ID.</param>
        /// <param name="traceFlags">The trace flags.</param>
        /// <returns>The formatted traceparent header string.</returns>
        public static string CreateTraceParent(string traceId, string spanId, ActivityTraceFlags traceFlags)
        {
            if (string.IsNullOrEmpty(traceId))
                throw new ArgumentException("Trace ID cannot be null or empty.", nameof(traceId));
            if (string.IsNullOrEmpty(spanId))
                throw new ArgumentException("Span ID cannot be null or empty.", nameof(spanId));

            return string.Format(
                "{0}-{1}-{2}-{3:x2}",
                TraceContextConstants.TraceParentVersion,
                traceId,
                spanId,
                (byte)traceFlags);
        }

        /// <summary>
        /// Gets the tracestate string from an <see cref="Activity"/>, or <c>null</c> if none.
        /// </summary>
        /// <param name="activity">The activity to extract tracestate from.</param>
        /// <returns>The tracestate string, or <c>null</c> if not set.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> is null.</exception>
        public static string? GetTraceState(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            return activity.TraceStateString;
        }

        private static bool IsValidHex(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        private static bool IsAllZeros(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '0')
                    return false;
            }
            return true;
        }
    }
}
