using System;
using System.Diagnostics;
using Grpc.Core;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.Grpc
{
    /// <summary>
    /// Provides helper methods for gRPC metadata operations including
    /// W3C TraceContext extraction/injection and correlation ID propagation.
    /// </summary>
    internal static class GrpcMetadataHelper
    {
        /// <summary>
        /// Extracts W3C TraceContext (<c>traceparent</c>/<c>tracestate</c>) from gRPC metadata.
        /// </summary>
        /// <param name="headers">The gRPC metadata headers to extract from.</param>
        /// <returns>
        /// The parsed <see cref="ActivityContext"/>, or <see langword="default"/>
        /// if no valid traceparent header is found.
        /// </returns>
        internal static ActivityContext ExtractTraceContext(Metadata? headers)
        {
            var traceparent = GetMetadataValue(headers, "traceparent");
            if (string.IsNullOrEmpty(traceparent))
                return default;

            if (ActivityContext.TryParse(traceparent, GetMetadataValue(headers, "tracestate"), out var context))
            {
                return context;
            }

            return default;
        }

        /// <summary>
        /// Injects W3C TraceContext (<c>traceparent</c>/<c>tracestate</c>) into gRPC metadata.
        /// </summary>
        /// <param name="activity">The current activity to extract trace context from.</param>
        /// <param name="metadata">The gRPC metadata to inject headers into.</param>
        internal static void InjectTraceContext(Activity? activity, Metadata metadata)
        {
            if (activity == null) return;

            var traceparent = string.Format(
                "00-{0}-{1}-{2}",
                activity.TraceId.ToString(),
                activity.SpanId.ToString(),
                activity.Recorded ? "01" : "00");
            metadata.Add("traceparent", traceparent);

            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                metadata.Add("tracestate", activity.TraceStateString!);
            }
        }

        /// <summary>
        /// Injects the current correlation ID into gRPC metadata.
        /// </summary>
        /// <param name="metadata">The gRPC metadata to inject the header into.</param>
        /// <param name="headerName">The metadata key name for the correlation ID.</param>
        internal static void InjectCorrelation(Metadata metadata, string headerName)
        {
            if (CorrelationContext.TryGetExplicitCorrelationId(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                metadata.Add(headerName, correlationId!);
            }
            else
            {
                // Fallback to Current which may auto-generate
                var current = CorrelationContext.Current;
                if (!string.IsNullOrEmpty(current))
                {
                    metadata.Add(headerName, current);
                }
            }
        }

        /// <summary>
        /// Gets a metadata value by key from gRPC headers.
        /// </summary>
        /// <param name="headers">The gRPC metadata headers.</param>
        /// <param name="key">The header key to look up (case-insensitive).</param>
        /// <returns>The header value, or <see langword="null"/> if not found.</returns>
        internal static string? GetMetadataValue(Metadata? headers, string key)
        {
            if (headers == null) return null;

            foreach (var entry in headers)
            {
                if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                    return entry.Value;
            }

            return null;
        }
    }
}
