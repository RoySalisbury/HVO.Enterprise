using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Captures telemetry context for background job execution.
    /// Enables correlation and tracing across asynchronous job boundaries.
    /// </summary>
    public sealed class BackgroundJobContext
    {
        /// <summary>
        /// Gets the correlation ID captured at enqueue time.
        /// </summary>
        public string CorrelationId { get; set; } = null!;

        /// <summary>
        /// Gets the parent Activity TraceId if available.
        /// </summary>
        public string? ParentActivityId { get; set; }

        /// <summary>
        /// Gets the parent Activity SpanId if available.
        /// </summary>
        public string? ParentSpanId { get; set; }

        /// <summary>
        /// Gets the user context information (if available).
        /// </summary>
        public Dictionary<string, string>? UserContext { get; set; }

        /// <summary>
        /// Gets the timestamp when the job was enqueued.
        /// </summary>
        public DateTimeOffset EnqueuedAt { get; set; }

        /// <summary>
        /// Gets optional custom metadata for the job.
        /// </summary>
        public Dictionary<string, object>? CustomMetadata { get; set; }

        /// <summary>
        /// Captures the current telemetry context.
        /// </summary>
        /// <returns>A BackgroundJobContext containing current correlation and Activity context.</returns>
        public static BackgroundJobContext Capture()
        {
            var activity = Activity.Current;

            return new BackgroundJobContext
            {
                CorrelationId = CorrelationContext.Current,
                ParentActivityId = activity?.TraceId.ToString(),
                ParentSpanId = activity?.SpanId.ToString(),
                UserContext = CaptureUserContext(),
                EnqueuedAt = DateTimeOffset.UtcNow,
                CustomMetadata = null
            };
        }

        /// <summary>
        /// Captures the current telemetry context with custom metadata.
        /// </summary>
        /// <param name="customMetadata">Custom metadata to include with the context.</param>
        /// <returns>A BackgroundJobContext containing current correlation and Activity context.</returns>
        public static BackgroundJobContext Capture(Dictionary<string, object> customMetadata)
        {
            var context = Capture();
            return new BackgroundJobContext
            {
                CorrelationId = context.CorrelationId,
                ParentActivityId = context.ParentActivityId,
                ParentSpanId = context.ParentSpanId,
                UserContext = context.UserContext,
                EnqueuedAt = context.EnqueuedAt,
                CustomMetadata = customMetadata
            };
        }

        /// <summary>
        /// Restores the captured telemetry context.
        /// </summary>
        /// <returns>An IDisposable that restores the previous context when disposed.</returns>
        public IDisposable Restore()
        {
            return new BackgroundJobContextScope(this);
        }

        private static Dictionary<string, string>? CaptureUserContext()
        {
            // Implementation will be enhanced when UserContextEnricher is available (US-011)
            // For now, we return null as user context enrichment is a future feature
            return null;
        }

        /// <summary>
        /// Creates a BackgroundJobContext from serialized values.
        /// Useful for deserializing from job storage.
        /// </summary>
        public static BackgroundJobContext FromValues(
            string correlationId,
            string? parentActivityId = null,
            string? parentSpanId = null,
            DateTimeOffset? enqueuedAt = null,
            Dictionary<string, string>? userContext = null,
            Dictionary<string, object>? customMetadata = null)
        {
            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentNullException(nameof(correlationId));

            return new BackgroundJobContext
            {
                CorrelationId = correlationId,
                ParentActivityId = parentActivityId,
                ParentSpanId = parentSpanId,
                UserContext = userContext,
                EnqueuedAt = enqueuedAt ?? DateTimeOffset.UtcNow,
                CustomMetadata = customMetadata
            };
        }
    }
}
