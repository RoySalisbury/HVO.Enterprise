using System;
using System.Diagnostics;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Extension methods for exception tracking.
    /// </summary>
    public static class TelemetryExceptionExtensions
    {
        private static readonly ExceptionAggregator Aggregator = new ExceptionAggregator();
        private static ExceptionTrackingOptions _options = new ExceptionTrackingOptions();

        /// <summary>
        /// Records an exception with the telemetry system.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static void RecordException(this Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var group = Aggregator.RecordException(exception);
            ExceptionMetrics.RecordException(group.ExceptionType);
            ExceptionMetrics.RecordErrorRatePerMinute(group.GetErrorRate());
            ExceptionMetrics.RecordErrorRatePerHour(group.GetErrorRatePerHour());

            var activity = Activity.Current;
            if (activity == null)
                return;

            var options = Options;
            var statusDescription = options.IncludeMessageInActivityStatus
                ? exception.Message
                : exception.GetType().Name;

            activity.SetStatus(ActivityStatusCode.Error, statusDescription);
            activity.AddTag("exception.type", exception.GetType().FullName ?? exception.GetType().Name);
            activity.AddTag("exception.fingerprint", group.Fingerprint);

            ActivityTagsCollection? eventTags = null;
            if (options.CaptureMessage)
            {
                eventTags ??= new ActivityTagsCollection();
                eventTags.Add("exception.message", exception.Message);
            }

            if (options.CaptureStackTrace && !string.IsNullOrEmpty(exception.StackTrace))
            {
                eventTags ??= new ActivityTagsCollection();
                eventTags.Add("exception.stacktrace", exception.StackTrace);
            }

            if (eventTags != null && eventTags.Count > 0)
            {
                activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, eventTags));
            }
            else
            {
                activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow));
            }
        }

        /// <summary>
        /// Gets the current exception tracking options.
        /// </summary>
        public static ExceptionTrackingOptions Options => Volatile.Read(ref _options);

        /// <summary>
        /// Updates the exception tracking options.
        /// </summary>
        /// <param name="options">Options to apply.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public static void Configure(ExceptionTrackingOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Interlocked.Exchange(ref _options, options);
        }

        /// <summary>
        /// Gets the exception aggregator for querying statistics.
        /// </summary>
        /// <returns>Exception aggregator instance.</returns>
        public static ExceptionAggregator GetAggregator()
        {
            return Aggregator;
        }
    }
}
