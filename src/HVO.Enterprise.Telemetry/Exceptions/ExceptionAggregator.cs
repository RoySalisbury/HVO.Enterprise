using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Aggregates exceptions by fingerprint for efficient storage and analysis.
    /// </summary>
    public sealed class ExceptionAggregator
    {
        private readonly ConcurrentDictionary<string, ExceptionGroup> _groups;
        private readonly TimeSpan _expirationWindow;
        private readonly Func<DateTimeOffset> _nowProvider;
        private long _totalCount;
        private long _firstOccurrenceTicks;
        private long _lastOccurrenceTicks;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionAggregator"/> class.
        /// </summary>
        /// <param name="expirationWindow">
        /// Optional expiration window for exception groups. Groups with no activity within this window
        /// are removed during cleanup. Defaults to 24 hours.
        /// </param>
        /// <remarks>
        /// Defaults to a 24-hour window. Pass a different <paramref name="expirationWindow"/> value
        /// to control memory growth in high-volume scenarios.
        /// </remarks>
        public ExceptionAggregator(TimeSpan? expirationWindow = null)
            : this(() => DateTimeOffset.UtcNow, expirationWindow)
        {
        }

        internal ExceptionAggregator(Func<DateTimeOffset> nowProvider, TimeSpan? expirationWindow = null)
        {
            if (nowProvider == null)
                throw new ArgumentNullException(nameof(nowProvider));

            _groups = new ConcurrentDictionary<string, ExceptionGroup>();
            _expirationWindow = expirationWindow ?? TimeSpan.FromHours(24);
            _nowProvider = nowProvider;
        }

        /// <summary>
        /// Gets the total number of exceptions recorded.
        /// </summary>
        public long TotalExceptions => Interlocked.Read(ref _totalCount);

        /// <summary>
        /// Records an exception and returns its group.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        /// <returns>The exception group.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public ExceptionGroup RecordException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var fingerprint = ExceptionFingerprinter.GenerateFingerprint(exception);

            var group = _groups.AddOrUpdate(
                fingerprint,
                key => new ExceptionGroup(fingerprint, exception, _nowProvider),
                (key, existing) =>
                {
                    existing.RecordOccurrence(exception);
                    return existing;
                });

            var nowTicks = _nowProvider().Ticks;
            Interlocked.Increment(ref _totalCount);
            Interlocked.Exchange(ref _lastOccurrenceTicks, nowTicks);
            if (Interlocked.Read(ref _firstOccurrenceTicks) == 0)
                Interlocked.CompareExchange(ref _firstOccurrenceTicks, nowTicks, 0);

            return group;
        }

        /// <summary>
        /// Gets all active exception groups.
        /// </summary>
        /// <returns>Active exception groups.</returns>
        public IReadOnlyCollection<ExceptionGroup> GetGroups()
        {
            CleanupExpiredGroups();
            return _groups.Values.ToList();
        }

        /// <summary>
        /// Gets an exception group by fingerprint.
        /// </summary>
        /// <param name="fingerprint">Fingerprint to look up.</param>
        /// <returns>The matching exception group, or null if not found.</returns>
        public ExceptionGroup? GetGroup(string fingerprint)
        {
            if (string.IsNullOrEmpty(fingerprint))
                return null;

            _groups.TryGetValue(fingerprint, out var group);
            return group;
        }

        /// <summary>
        /// Calculates the global error rate per minute.
        /// </summary>
        /// <returns>Error rate per minute.</returns>
        public double GetGlobalErrorRatePerMinute()
        {
            return GetRatePerMinute(TotalExceptions);
        }

        /// <summary>
        /// Calculates the global error rate per hour.
        /// </summary>
        /// <returns>Error rate per hour.</returns>
        public double GetGlobalErrorRatePerHour()
        {
            return GetRatePerHour(TotalExceptions);
        }

        /// <summary>
        /// Calculates the global error rate as a percentage of total operations.
        /// </summary>
        /// <param name="totalOperations">Total operations tracked.</param>
        /// <returns>Error rate percentage.</returns>
        public double GetGlobalErrorRatePercentage(long totalOperations)
        {
            if (totalOperations <= 0)
                return 0;

            return (double)TotalExceptions / totalOperations * 100.0;
        }

        private double GetRatePerMinute(long count)
        {
            var duration = GetGlobalDuration();
            if (duration.TotalMinutes < 0.01)
                return count;

            return count / duration.TotalMinutes;
        }

        private double GetRatePerHour(long count)
        {
            var duration = GetGlobalDuration();
            if (duration.TotalHours < 0.001)
                return count;

            return count / duration.TotalHours;
        }

        private TimeSpan GetGlobalDuration()
        {
            var firstTicks = Interlocked.Read(ref _firstOccurrenceTicks);
            if (firstTicks == 0)
                return TimeSpan.Zero;

            var lastTicks = Interlocked.Read(ref _lastOccurrenceTicks);
            if (lastTicks == 0 || lastTicks < firstTicks)
                return TimeSpan.Zero;

            return new TimeSpan(lastTicks - firstTicks);
        }

        /// <summary>
        /// Removes expired exception groups.
        /// </summary>
        private void CleanupExpiredGroups()
        {
            var now = _nowProvider();
            var expired = _groups
                .Where(kvp => now - kvp.Value.LastOccurrence > _expirationWindow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _groups.TryRemove(key, out _);
            }
        }
    }
}
