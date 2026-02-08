using System;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Metrics for operation scopes.
    /// </summary>
    public static class OperationScopeMetrics
    {
        private static readonly IHistogram<double> DurationHistogram;
        private static readonly ICounter<long> ErrorCounter;

        static OperationScopeMetrics()
        {
            var recorder = MetricRecorderFactory.Instance;

            DurationHistogram = recorder.CreateHistogramDouble(
                "telemetry.operation.duration",
                "ms",
                "Operation duration in milliseconds");

            ErrorCounter = recorder.CreateCounter(
                "telemetry.operation.errors",
                "errors",
                "Total operation errors");
        }

        /// <summary>
        /// Records duration and status for an operation.
        /// </summary>
        /// <param name="operationName">Operation name.</param>
        /// <param name="duration">Operation duration.</param>
        /// <param name="failed">Whether the operation failed.</param>
        public static void RecordDuration(string operationName, TimeSpan duration, bool failed)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name must be non-empty.", nameof(operationName));

            var operationTag = new MetricTag("operation", operationName);
            var statusTag = new MetricTag("status", failed ? "error" : "ok");

            DurationHistogram.Record(duration.TotalMilliseconds, in operationTag, in statusTag);
        }

        /// <summary>
        /// Records an operation error.
        /// </summary>
        /// <param name="operationName">Operation name.</param>
        /// <param name="exception">Exception that caused the failure.</param>
        public static void RecordError(string operationName, Exception exception)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name must be non-empty.", nameof(operationName));
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var operationTag = new MetricTag("operation", operationName);
            var exceptionTag = new MetricTag("exception.type", exception.GetType().Name);

            ErrorCounter.Add(1, in operationTag, in exceptionTag);
        }
    }
}
