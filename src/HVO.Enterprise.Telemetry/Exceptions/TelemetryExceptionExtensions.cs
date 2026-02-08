using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Extension methods for exception tracking.
    /// </summary>
    public static class TelemetryExceptionExtensions
    {
        private static readonly ExceptionAggregator Aggregator = new ExceptionAggregator();

        /// <summary>
        /// Records an exception with the telemetry system.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        public static void RecordException(this Exception exception)
        {
            if (exception == null)
                return;

            var group = Aggregator.RecordException(exception);
            ExceptionMetrics.RecordException(group.ExceptionType, group.Fingerprint);
            ExceptionMetrics.RecordErrorRatePerMinute(group.GetErrorRate());
            ExceptionMetrics.RecordErrorRatePerHour(group.GetErrorRatePerHour());

            var activity = Activity.Current;
            if (activity == null)
                return;

            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddTag("exception.type", exception.GetType().FullName ?? exception.GetType().Name);
            activity.AddTag("exception.message", exception.Message);
            activity.AddTag("exception.fingerprint", group.Fingerprint);

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                activity.AddTag("exception.stacktrace", exception.StackTrace);
            }

            var tags = new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName ?? exception.GetType().Name },
                { "exception.message", exception.Message },
                { "exception.fingerprint", group.Fingerprint }
            };

            activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
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
