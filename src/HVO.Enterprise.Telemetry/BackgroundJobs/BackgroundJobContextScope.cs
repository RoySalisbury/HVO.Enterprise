using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Scope that restores background job context and creates an Activity for the job execution.
    /// </summary>
    internal sealed class BackgroundJobContextScope : IDisposable
    {
        private readonly IDisposable _correlationScope;
        private readonly Activity? _activity;
        private readonly ActivitySource? _activitySource;
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
                _activitySource = new ActivitySource("HVO.Enterprise.Telemetry.BackgroundJobs", "1.0.0");

                // Parse parent context (using CreateFromString for .NET Standard 2.0 compatibility)
                try
                {
                    var traceId = ActivityTraceId.CreateFromString(context.ParentActivityId.AsSpan());
                    var spanId = ActivitySpanId.CreateFromString(context.ParentSpanId.AsSpan());

                    var parentContext = new ActivityContext(
                        traceId,
                        spanId,
                        ActivityTraceFlags.Recorded);

                    _activity = _activitySource.StartActivity(
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
                    // Invalid trace/span ID format, skip Activity creation
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
            _activitySource?.Dispose();
            _correlationScope.Dispose();
            _disposed = true;
        }
    }
}
