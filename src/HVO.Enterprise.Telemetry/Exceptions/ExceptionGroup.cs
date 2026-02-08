using System;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Represents a group of exceptions with the same fingerprint.
    /// </summary>
    public sealed class ExceptionGroup
    {
        private static readonly TimeSpan MinimumRateWindow = TimeSpan.FromSeconds(1);
        private long _count;
        private readonly Func<DateTimeOffset> _nowProvider;
        private long _firstOccurrenceTicks;
        private long _lastOccurrenceTicks;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionGroup"/> class.
        /// </summary>
        /// <param name="fingerprint">Fingerprint for the exception group.</param>
        /// <param name="exception">Representative exception.</param>
        /// <param name="nowProvider">Provides the current time for tracking.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        internal ExceptionGroup(string fingerprint, Exception exception, Func<DateTimeOffset> nowProvider)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            if (nowProvider == null)
                throw new ArgumentNullException(nameof(nowProvider));

            _nowProvider = nowProvider;
            var now = _nowProvider();
            _firstOccurrenceTicks = now.UtcTicks;
            _lastOccurrenceTicks = _firstOccurrenceTicks;

            Fingerprint = fingerprint;
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            Message = exception.Message;
            StackTrace = exception.StackTrace;
            _count = 1;
        }

        /// <summary>
        /// Gets the exception fingerprint.
        /// </summary>
        public string Fingerprint { get; }

        /// <summary>
        /// Gets the exception type name.
        /// </summary>
        public string ExceptionType { get; }

        /// <summary>
        /// Gets the representative exception message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the representative stack trace.
        /// </summary>
        public string? StackTrace { get; }

        /// <summary>
        /// Gets the first occurrence timestamp.
        /// </summary>
        public DateTimeOffset FirstOccurrence => new DateTimeOffset(
            Interlocked.Read(ref _firstOccurrenceTicks),
            TimeSpan.Zero);

        /// <summary>
        /// Gets the last occurrence timestamp.
        /// </summary>
        public DateTimeOffset LastOccurrence => new DateTimeOffset(
            Interlocked.Read(ref _lastOccurrenceTicks),
            TimeSpan.Zero);

        internal long LastOccurrenceTicks => Interlocked.Read(ref _lastOccurrenceTicks);

        /// <summary>
        /// Gets the total number of occurrences.
        /// </summary>
        public long Count => Interlocked.Read(ref _count);

        /// <summary>
        /// Records a new occurrence for this exception group.
        /// </summary>
        /// <param name="exception">Exception instance.</param>
        internal void RecordOccurrence(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            Interlocked.Increment(ref _count);
            Interlocked.Exchange(ref _lastOccurrenceTicks, _nowProvider().UtcTicks);
        }

        /// <summary>
        /// Gets the error rate in occurrences per minute.
        /// </summary>
        /// <returns>Error rate per minute.</returns>
        public double GetErrorRate()
        {
            var count = Count;
            if (count == 0)
                return 0;

            var duration = GetDuration();
            var effectiveMinutes = Math.Max(duration.TotalMinutes, MinimumRateWindow.TotalMinutes);

            return count / effectiveMinutes;
        }

        /// <summary>
        /// Gets the error rate in occurrences per hour.
        /// </summary>
        /// <returns>Error rate per hour.</returns>
        public double GetErrorRatePerHour()
        {
            var count = Count;
            if (count == 0)
                return 0;

            var duration = GetDuration();
            var effectiveHours = Math.Max(duration.TotalHours, MinimumRateWindow.TotalHours);

            return count / effectiveHours;
        }

        /// <summary>
        /// Calculates the error rate as a percentage of total operations.
        /// </summary>
        /// <param name="totalOperations">Total operation count.</param>
        /// <returns>Error rate percentage.</returns>
        public double GetErrorRatePercentage(long totalOperations)
        {
            if (totalOperations <= 0)
                return 0;

            return (double)Count / totalOperations * 100.0;
        }

        private TimeSpan GetDuration()
        {
            var firstTicks = Interlocked.Read(ref _firstOccurrenceTicks);
            var lastTicks = Interlocked.Read(ref _lastOccurrenceTicks);
            if (lastTicks <= firstTicks)
                return TimeSpan.Zero;

            return new TimeSpan(lastTicks - firstTicks);
        }
    }
}
