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
        private static readonly TimeSpan MinimumRateWindow = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, ExceptionGroup> _groups;
        private readonly TimeSpan _expirationWindow;
        private readonly Func<DateTimeOffset> _nowProvider;
        private readonly long _cleanupIntervalTicks;
        private long _totalCount;
        private long _firstOccurrenceTicks;
        private long _lastOccurrenceTicks;
        private long _lastCleanupTicks;

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
        /// Cleanup is sampled; expired groups are removed at most every five minutes or sooner if the
        /// expiration window is shorter.
        /// </remarks>
        public ExceptionAggregator(TimeSpan? expirationWindow = null)
            : this(() => DateTimeOffset.UtcNow, expirationWindow)
        {
        }

        internal ExceptionAggregator(Func<DateTimeOffset> nowProvider, TimeSpan? expirationWindow = null)
        {
            if (nowProvider == null)
                throw new ArgumentNullException(nameof(nowProvider));

            var window = expirationWindow ?? TimeSpan.FromHours(24);
            if (window <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(expirationWindow), "Expiration window must be greater than zero.");

            _groups = new ConcurrentDictionary<string, ExceptionGroup>();
            _nowProvider = nowProvider;
            _expirationWindow = window;
            _cleanupIntervalTicks = Math.Min(window.Ticks, DefaultCleanupInterval.Ticks);
            _lastCleanupTicks = _nowProvider().UtcTicks;
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

            var nowTicks = _nowProvider().UtcTicks;
            Interlocked.Increment(ref _totalCount);
            Interlocked.Exchange(ref _lastOccurrenceTicks, nowTicks);
            if (Interlocked.Read(ref _firstOccurrenceTicks) == 0)
                Interlocked.CompareExchange(ref _firstOccurrenceTicks, nowTicks, 0);

            TryCleanup(nowTicks);
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
            var effectiveMinutes = Math.Max(duration.TotalMinutes, MinimumRateWindow.TotalMinutes);
            if (effectiveMinutes <= 0)
                return 0;

            return count / effectiveMinutes;
        }

        private double GetRatePerHour(long count)
        {
            var duration = GetGlobalDuration();
            var effectiveHours = Math.Max(duration.TotalHours, MinimumRateWindow.TotalHours);
            if (effectiveHours <= 0)
                return 0;

            return count / effectiveHours;
        }

        private TimeSpan GetGlobalDuration()
        {
            var firstTicks = Interlocked.Read(ref _firstOccurrenceTicks);
            if (firstTicks == 0)
                return TimeSpan.Zero;

            var lastTicks = Interlocked.Read(ref _lastOccurrenceTicks);
            if (lastTicks == 0 || lastTicks <= firstTicks)
                return TimeSpan.Zero;

            return new TimeSpan(lastTicks - firstTicks);
        }

        private void TryCleanup(long nowTicks)
        {
            var lastCleanupTicks = Interlocked.Read(ref _lastCleanupTicks);
            if (nowTicks - lastCleanupTicks < _cleanupIntervalTicks)
                return;

            if (Interlocked.CompareExchange(ref _lastCleanupTicks, nowTicks, lastCleanupTicks) != lastCleanupTicks)
                return;

            CleanupExpiredGroups(nowTicks);
        }

        /// <summary>
        /// Removes expired exception groups.
        /// </summary>
        private void CleanupExpiredGroups(long? nowTicks = null)
        {
            var currentTicks = nowTicks ?? _nowProvider().UtcTicks;
            var expired = _groups
                .Where(kvp => currentTicks - kvp.Value.LastOccurrenceTicks > _expirationWindow.Ticks)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _groups.TryRemove(key, out _);
            }
        }
    }
}
