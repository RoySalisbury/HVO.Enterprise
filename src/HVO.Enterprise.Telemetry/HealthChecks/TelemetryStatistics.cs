using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HVO.Enterprise.Telemetry.Abstractions;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// Thread-safe implementation of telemetry statistics tracking.
    /// Uses lock-free <see cref="Interlocked"/> operations for all counter updates
    /// to achieve less than 10ns overhead per increment with no contention deadlocks.
    /// </summary>
    internal sealed class TelemetryStatistics : ITelemetryStatistics
    {
        private DateTimeOffset _startTime;
        private readonly ConcurrentDictionary<string, SourceStats> _sourceStats;
        private readonly RollingWindow _errorWindow;
        private readonly RollingWindow _throughputWindow;

        // Atomic counters for thread-safe increments
        private long _activitiesCreated;
        private long _activitiesCompleted;
        private long _exceptionsTracked;
        private long _eventsRecorded;
        private long _metricsRecorded;
        private long _itemsEnqueued;
        private long _itemsProcessed;
        private long _itemsDropped;
        private long _processingErrors;
        private long _correlationIdsGenerated;
        private int _queueDepth;
        private int _maxQueueDepth;

        // For average calculation
        private long _totalProcessingTimeTicks;
        private long _processingCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryStatistics"/> class.
        /// </summary>
        public TelemetryStatistics()
            : this(DateTimeOffset.UtcNow)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryStatistics"/> class
        /// with a specific start time (for testing).
        /// </summary>
        /// <param name="startTime">The start time for statistics collection.</param>
        internal TelemetryStatistics(DateTimeOffset startTime)
        {
            _startTime = startTime;
            _sourceStats = new ConcurrentDictionary<string, SourceStats>(StringComparer.OrdinalIgnoreCase);
            _errorWindow = new RollingWindow(TimeSpan.FromMinutes(1));
            _throughputWindow = new RollingWindow(TimeSpan.FromMinutes(1));
        }

        /// <inheritdoc />
        public DateTimeOffset StartTime
        {
            get { return _startTime; }
        }

        /// <inheritdoc />
        public long ActivitiesCreated
        {
            get { return Interlocked.Read(ref _activitiesCreated); }
        }

        /// <inheritdoc />
        public long ActivitiesCompleted
        {
            get { return Interlocked.Read(ref _activitiesCompleted); }
        }

        /// <inheritdoc />
        public long ActiveActivities
        {
            get { return ActivitiesCreated - ActivitiesCompleted; }
        }

        /// <inheritdoc />
        public long ExceptionsTracked
        {
            get { return Interlocked.Read(ref _exceptionsTracked); }
        }

        /// <inheritdoc />
        public long EventsRecorded
        {
            get { return Interlocked.Read(ref _eventsRecorded); }
        }

        /// <inheritdoc />
        public long MetricsRecorded
        {
            get { return Interlocked.Read(ref _metricsRecorded); }
        }

        /// <inheritdoc />
        public int QueueDepth
        {
            get { return Interlocked.CompareExchange(ref _queueDepth, 0, 0); }
        }

        /// <inheritdoc />
        public int MaxQueueDepth
        {
            get { return Interlocked.CompareExchange(ref _maxQueueDepth, 0, 0); }
        }

        /// <inheritdoc />
        public long ItemsEnqueued
        {
            get { return Interlocked.Read(ref _itemsEnqueued); }
        }

        /// <inheritdoc />
        public long ItemsProcessed
        {
            get { return Interlocked.Read(ref _itemsProcessed); }
        }

        /// <inheritdoc />
        public long ItemsDropped
        {
            get { return Interlocked.Read(ref _itemsDropped); }
        }

        /// <inheritdoc />
        public long ProcessingErrors
        {
            get { return Interlocked.Read(ref _processingErrors); }
        }

        /// <inheritdoc />
        public long CorrelationIdsGenerated
        {
            get { return Interlocked.Read(ref _correlationIdsGenerated); }
        }

        /// <inheritdoc />
        public double AverageProcessingTimeMs
        {
            get
            {
                var count = Interlocked.Read(ref _processingCount);
                if (count == 0)
                    return 0;

                var totalTicks = Interlocked.Read(ref _totalProcessingTimeTicks);
                return new TimeSpan(totalTicks).TotalMilliseconds / count;
            }
        }

        /// <inheritdoc />
        public double CurrentErrorRate
        {
            get { return _errorWindow.GetRate(); }
        }

        /// <inheritdoc />
        public double CurrentThroughput
        {
            get { return _throughputWindow.GetRate(); }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, ActivitySourceStatistics> PerSourceStatistics
        {
            get
            {
                return _sourceStats.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ActivitySourceStatistics
                    {
                        SourceName = kvp.Key,
                        ActivitiesCreated = kvp.Value.Created,
                        ActivitiesCompleted = kvp.Value.Completed,
                        AverageDurationMs = kvp.Value.AverageDurationMs
                    });
            }
        }

        // --- Internal increment methods (called by telemetry system) ---

        /// <summary>
        /// Records that an activity was created for the specified source.
        /// </summary>
        /// <param name="sourceName">Name of the activity source.</param>
        internal void IncrementActivitiesCreated(string sourceName)
        {
            Interlocked.Increment(ref _activitiesCreated);
            _throughputWindow.Record(DateTimeOffset.UtcNow);

            if (!string.IsNullOrEmpty(sourceName))
            {
                var stats = _sourceStats.GetOrAdd(sourceName, _ => new SourceStats());
                stats.IncrementCreated();
            }
        }

        /// <summary>
        /// Records that an activity was completed for the specified source.
        /// </summary>
        /// <param name="sourceName">Name of the activity source.</param>
        /// <param name="duration">Duration of the activity.</param>
        internal void IncrementActivitiesCompleted(string sourceName, TimeSpan duration)
        {
            Interlocked.Increment(ref _activitiesCompleted);

            if (!string.IsNullOrEmpty(sourceName) && _sourceStats.TryGetValue(sourceName, out var stats))
            {
                stats.IncrementCompleted(duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Records that an exception was tracked.
        /// </summary>
        internal void IncrementExceptionsTracked()
        {
            Interlocked.Increment(ref _exceptionsTracked);
            _errorWindow.Record(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Records that a custom event was recorded.
        /// </summary>
        internal void IncrementEventsRecorded()
        {
            Interlocked.Increment(ref _eventsRecorded);
        }

        /// <summary>
        /// Records that a metric measurement was recorded.
        /// </summary>
        internal void IncrementMetricsRecorded()
        {
            Interlocked.Increment(ref _metricsRecorded);
        }

        /// <summary>
        /// Records that an item was enqueued for background processing.
        /// </summary>
        internal void IncrementItemsEnqueued()
        {
            Interlocked.Increment(ref _itemsEnqueued);
        }

        /// <summary>
        /// Records that an item was processed and the time taken.
        /// </summary>
        /// <param name="processingTime">Time taken to process the item.</param>
        internal void IncrementItemsProcessed(TimeSpan processingTime)
        {
            Interlocked.Increment(ref _itemsProcessed);
            Interlocked.Add(ref _totalProcessingTimeTicks, processingTime.Ticks);
            Interlocked.Increment(ref _processingCount);
        }

        /// <summary>
        /// Records that an item was dropped due to queue overflow.
        /// </summary>
        internal void IncrementItemsDropped()
        {
            Interlocked.Increment(ref _itemsDropped);
        }

        /// <summary>
        /// Records that a background processing error occurred.
        /// </summary>
        internal void IncrementProcessingErrors()
        {
            Interlocked.Increment(ref _processingErrors);
            _errorWindow.Record(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Records that a correlation ID was auto-generated.
        /// </summary>
        internal void IncrementCorrelationIdsGenerated()
        {
            Interlocked.Increment(ref _correlationIdsGenerated);
        }

        /// <summary>
        /// Updates the current queue depth and tracks the maximum.
        /// </summary>
        /// <param name="newDepth">The new queue depth.</param>
        internal void UpdateQueueDepth(int newDepth)
        {
            Interlocked.Exchange(ref _queueDepth, newDepth);

            // Update max if needed using compare-and-swap loop
            int currentMax;
            do
            {
                currentMax = Interlocked.CompareExchange(ref _maxQueueDepth, 0, 0);
                if (newDepth <= currentMax)
                    break;
            }
            while (Interlocked.CompareExchange(ref _maxQueueDepth, newDepth, currentMax) != currentMax);
        }

        /// <inheritdoc />
        public TelemetryStatisticsSnapshot GetSnapshot()
        {
            return new TelemetryStatisticsSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                StartTime = StartTime,
                ActivitiesCreated = ActivitiesCreated,
                ActivitiesCompleted = ActivitiesCompleted,
                ActiveActivities = ActiveActivities,
                ExceptionsTracked = ExceptionsTracked,
                EventsRecorded = EventsRecorded,
                MetricsRecorded = MetricsRecorded,
                QueueDepth = QueueDepth,
                MaxQueueDepth = MaxQueueDepth,
                ItemsEnqueued = ItemsEnqueued,
                ItemsProcessed = ItemsProcessed,
                ItemsDropped = ItemsDropped,
                ProcessingErrors = ProcessingErrors,
                AverageProcessingTimeMs = AverageProcessingTimeMs,
                CorrelationIdsGenerated = CorrelationIdsGenerated,
                CurrentErrorRate = CurrentErrorRate,
                CurrentThroughput = CurrentThroughput,
                PerSourceStatistics = PerSourceStatistics
            };
        }

        /// <inheritdoc />
        public void Reset()
        {
            Interlocked.Exchange(ref _activitiesCreated, 0);
            Interlocked.Exchange(ref _activitiesCompleted, 0);
            Interlocked.Exchange(ref _exceptionsTracked, 0);
            Interlocked.Exchange(ref _eventsRecorded, 0);
            Interlocked.Exchange(ref _metricsRecorded, 0);
            Interlocked.Exchange(ref _itemsEnqueued, 0);
            Interlocked.Exchange(ref _itemsProcessed, 0);
            Interlocked.Exchange(ref _itemsDropped, 0);
            Interlocked.Exchange(ref _processingErrors, 0);
            Interlocked.Exchange(ref _correlationIdsGenerated, 0);
            Interlocked.Exchange(ref _queueDepth, 0);
            Interlocked.Exchange(ref _maxQueueDepth, 0);
            Interlocked.Exchange(ref _totalProcessingTimeTicks, 0);
            Interlocked.Exchange(ref _processingCount, 0);

            _sourceStats.Clear();
            _errorWindow.Clear();
            _throughputWindow.Clear();

            _startTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Per-source tracking with lock-free counters.
        /// </summary>
        private sealed class SourceStats
        {
            private long _created;
            private long _completed;
            private long _totalDurationTicks;

            public long Created
            {
                get { return Interlocked.Read(ref _created); }
            }

            public long Completed
            {
                get { return Interlocked.Read(ref _completed); }
            }

            public double AverageDurationMs
            {
                get
                {
                    var count = Interlocked.Read(ref _completed);
                    if (count == 0)
                        return 0;

                    var totalTicks = Interlocked.Read(ref _totalDurationTicks);
                    return new TimeSpan(totalTicks).TotalMilliseconds / count;
                }
            }

            public void IncrementCreated()
            {
                Interlocked.Increment(ref _created);
            }

            public void IncrementCompleted(double durationMs)
            {
                Interlocked.Increment(ref _completed);
                var ticks = TimeSpan.FromMilliseconds(durationMs).Ticks;
                Interlocked.Add(ref _totalDurationTicks, ticks);
            }
        }

        /// <summary>
        /// Lock-free rolling window for rate-per-second calculations over a configurable duration.
        /// Uses <see cref="ConcurrentQueue{T}"/> to maintain a bounded set of timestamps.
        /// </summary>
        internal sealed class RollingWindow
        {
            private readonly TimeSpan _duration;
            private readonly ConcurrentQueue<long> _timestamps;

            /// <summary>
            /// Initializes a new rolling window with the specified duration.
            /// </summary>
            /// <param name="duration">The window duration over which to calculate rates.</param>
            public RollingWindow(TimeSpan duration)
            {
                if (duration <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

                _duration = duration;
                _timestamps = new ConcurrentQueue<long>();
            }

            /// <summary>
            /// Records an event at the given timestamp.
            /// </summary>
            /// <param name="timestamp">The timestamp of the event.</param>
            public void Record(DateTimeOffset timestamp)
            {
                _timestamps.Enqueue(timestamp.UtcTicks);
                CleanOld(timestamp.UtcTicks);
            }

            /// <summary>
            /// Calculates the events-per-second rate over the rolling window.
            /// </summary>
            /// <returns>Events per second.</returns>
            public double GetRate()
            {
                var nowTicks = DateTimeOffset.UtcNow.UtcTicks;
                CleanOld(nowTicks);
                var count = _timestamps.Count;
                return count / _duration.TotalSeconds;
            }

            /// <summary>
            /// Clears all timestamps from the window.
            /// </summary>
            public void Clear()
            {
                while (_timestamps.TryDequeue(out _))
                {
                    // Drain the queue
                }
            }

            private void CleanOld(long nowTicks)
            {
                var cutoffTicks = nowTicks - _duration.Ticks;
                while (_timestamps.TryPeek(out var timestamp) && timestamp < cutoffTicks)
                {
                    _timestamps.TryDequeue(out _);
                }
            }
        }
    }
}
