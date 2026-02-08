using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Sampling;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Scope that restores background job context and creates an Activity for the job execution.
    /// </summary>
    internal sealed class BackgroundJobContextScope : IDisposable
    {
        private readonly IDisposable _correlationScope;
        private readonly Activity? _activity;
        private bool _disposed;

        /// <summary>
        /// Creates a new background job context scope.
        /// </summary>
        /// <param name="context">The context to restore.</param>
        public BackgroundJobContextScope(BackgroundJobContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Restore correlation ID
            _correlationScope = CorrelationContext.BeginScope(context.CorrelationId);

            // Create Activity with parent link if parent context is available
            if (!string.IsNullOrEmpty(context.ParentActivityId) &&
                !string.IsNullOrEmpty(context.ParentSpanId))
            {
                var activitySource = SamplingActivitySourceExtensions.CreateWithSampling(
                    "HVO.Enterprise.Telemetry.BackgroundJobs",
                    "1.0.0");

                // Parse parent context (using CreateFromString for .NET Standard 2.0 compatibility)
                try
                {
                    var traceId = ActivityTraceId.CreateFromString(context.ParentActivityId.AsSpan());
                    var spanId = ActivitySpanId.CreateFromString(context.ParentSpanId.AsSpan());

                    var parentContext = new ActivityContext(
                        traceId,
                        spanId,
                        ActivityTraceFlags.Recorded);

                    _activity = activitySource.StartActivity(
                        "BackgroundJob",
                        ActivityKind.Internal,
                        parentContext);

                    // Add job metadata to Activity
                    if (_activity != null)
                    {
                        _activity.SetTag("job.enqueued_at", context.EnqueuedAt.ToString("O"));

                        var executionDelay = DateTimeOffset.UtcNow - context.EnqueuedAt;
                        _activity.SetTag("job.execution_delay_ms", executionDelay.TotalMilliseconds);

                        // Add custom metadata as tags
                        if (context.CustomMetadata != null)
                        {
                            foreach (var kvp in context.CustomMetadata)
                            {
                                _activity.SetTag($"job.metadata.{kvp.Key}", kvp.Value);
                            }
                        }
                    }
                }
                catch (FormatException)
                {
                    // Invalid trace/span ID format — skip Activity creation.
                    // This occurs when a caller supplies a malformed W3C trace-id or span-id,
                    // which can happen with legacy systems or misconfigured producers.
                    // We intentionally swallow FormatException (not a broader catch) so the
                    // background job still executes; the only impact is loss of distributed
                    // trace correlation for this particular job invocation.
                    // Suppression: no logging here to stay allocation-free on the hot path;
                    // callers can detect missing correlation via the null Activity.
                }
            }
        }

        /// <summary>
        /// Disposes the scope, stopping the Activity and restoring the previous correlation context.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _activity?.Dispose();
            _correlationScope.Dispose();
            _disposed = true;
        }
    }
}
