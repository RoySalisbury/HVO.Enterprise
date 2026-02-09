using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Http
{
    /// <summary>
    /// Extension methods for <see cref="Activity"/> to support exception recording
    /// and HTTP-specific enrichment following OpenTelemetry semantic conventions.
    /// </summary>
    public static class ActivityExtensions
    {
        /// <summary>
        /// Records an exception on the activity following OpenTelemetry semantic conventions.
        /// Sets exception type, message, and stack trace tags, and adds an
        /// <c>exception</c> activity event.
        /// </summary>
        /// <param name="activity">The activity to record the exception on.</param>
        /// <param name="exception">The exception to record.</param>
        /// <returns>The activity for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="activity"/> or <paramref name="exception"/> is null.
        /// </exception>
        public static Activity RecordException(this Activity activity, Exception exception)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var tags = new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName ?? exception.GetType().Name },
                { "exception.message", exception.Message }
            };

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                tags.Add("exception.stacktrace", exception.StackTrace);
            }

            activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));

            return activity;
        }
    }
}
