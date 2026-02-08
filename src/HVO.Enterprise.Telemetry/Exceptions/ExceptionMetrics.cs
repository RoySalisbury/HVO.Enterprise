using System;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Exposes exception metrics for monitoring.
    /// </summary>
    public static class ExceptionMetrics
    {
        private static readonly ICounter<long> ExceptionCounter;
        private static readonly IHistogram<long> ExceptionRatePerMinuteHistogram;
        private static readonly IHistogram<long> ExceptionRatePerHourHistogram;

        static ExceptionMetrics()
        {
            var recorder = MetricRecorderFactory.Instance;

            ExceptionCounter = recorder.CreateCounter(
                "exceptions.total",
                "exceptions",
                "Total number of exceptions");

            ExceptionRatePerMinuteHistogram = recorder.CreateHistogram(
                "exceptions.rate.per_minute",
                "exceptions/min",
                "Exception rate per minute");

            ExceptionRatePerHourHistogram = recorder.CreateHistogram(
                "exceptions.rate.per_hour",
                "exceptions/hour",
                "Exception rate per hour");
        }

        /// <summary>
        /// Records an exception occurrence.
        /// </summary>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="fingerprint">Exception fingerprint.</param>
        public static void RecordException(string exceptionType, string fingerprint)
        {
            if (string.IsNullOrEmpty(exceptionType))
                throw new ArgumentException("Exception type must be non-empty.", nameof(exceptionType));
            if (string.IsNullOrEmpty(fingerprint))
                throw new ArgumentException("Exception fingerprint must be non-empty.", nameof(fingerprint));

            ExceptionCounter.Add(1,
                new MetricTag("type", exceptionType),
                new MetricTag("fingerprint", fingerprint));
        }

        /// <summary>
        /// Records error rate per minute for monitoring.
        /// </summary>
        /// <param name="exceptionsPerMinute">Exceptions per minute.</param>
        public static void RecordErrorRatePerMinute(double exceptionsPerMinute)
        {
            ExceptionRatePerMinuteHistogram.Record(ToLong(exceptionsPerMinute));
        }

        /// <summary>
        /// Records error rate per hour for monitoring.
        /// </summary>
        /// <param name="exceptionsPerHour">Exceptions per hour.</param>
        public static void RecordErrorRatePerHour(double exceptionsPerHour)
        {
            ExceptionRatePerHourHistogram.Record(ToLong(exceptionsPerHour));
        }

        private static long ToLong(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            return Convert.ToInt64(Math.Round(value));
        }
    }
}
