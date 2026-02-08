using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Metric recorder using EventCounters for older runtimes.
    /// </summary>
    internal sealed class EventCounterRecorder : IMetricRecorder
    {
        private readonly TelemetryEventSource _eventSource;
        private readonly MetricTagCardinalityTracker? _cardinalityTracker;

        internal EventCounterRecorder(ILogger<EventCounterRecorder>? logger = null)
        {
            var effectiveLogger = logger ?? NullLogger<EventCounterRecorder>.Instance;

            _eventSource = TelemetryEventSource.Instance;
            if (effectiveLogger.IsEnabled(LogLevel.Warning))
                _cardinalityTracker = new MetricTagCardinalityTracker(effectiveLogger);
        }

        public ICounter<long> CreateCounter(string name, string? unit = null, string? description = null)
        {
            MetricNameValidator.ValidateName(name, nameof(name));
            return new EventCounterCounter(_eventSource, name, _cardinalityTracker);
        }

        public IHistogram<long> CreateHistogram(string name, string? unit = null, string? description = null)
        {
            MetricNameValidator.ValidateName(name, nameof(name));
            return new EventCounterHistogram(_eventSource, name, _cardinalityTracker);
        }

        public IHistogram<double> CreateHistogramDouble(string name, string? unit = null, string? description = null)
        {
            MetricNameValidator.ValidateName(name, nameof(name));
            return new EventCounterHistogramDouble(_eventSource, name, _cardinalityTracker);
        }

        public IDisposable CreateObservableGauge(
            string name,
            Func<double> observeValue,
            string? unit = null,
            string? description = null)
        {
            if (observeValue == null)
                throw new ArgumentNullException(nameof(observeValue));

            MetricNameValidator.ValidateName(name, nameof(name));

            return new EventCounterGauge(_eventSource, name, observeValue);
        }

        [EventSource(Name = "HVO-Enterprise-Telemetry")]
        private sealed class TelemetryEventSource : EventSource
        {
            internal static readonly TelemetryEventSource Instance = new TelemetryEventSource();

            private readonly ConcurrentDictionary<string, EventCounter> _eventCounters;

            private TelemetryEventSource()
            {
                _eventCounters = new ConcurrentDictionary<string, EventCounter>();
            }

            public void RecordValue(string name, double value)
            {
                var counter = _eventCounters.GetOrAdd(name, key => new EventCounter(key, this));
                counter.WriteMetric((float)value);
            }

            public void IncrementValue(string name, double value)
            {
                var counter = _eventCounters.GetOrAdd(name, key => new EventCounter(key, this));
                counter.WriteMetric((float)value);
            }
        }

        private sealed class EventCounterGauge : IDisposable
        {
            private readonly TelemetryEventSource _eventSource;
            private readonly string _metricName;
            private readonly Func<double> _observeValue;
            private readonly Timer _timer;
            private volatile bool _disposed;

            public EventCounterGauge(
                TelemetryEventSource eventSource,
                string metricName,
                Func<double> observeValue)
            {
                _eventSource = eventSource;
                _metricName = metricName;
                _observeValue = observeValue;

                _timer = new Timer(Observe, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }

            private void Observe(object? state)
            {
                if (_disposed)
                    return;

                double value;
                try
                {
                    value = _observeValue();
                }
                catch (Exception)
                {
                    // The _observeValue delegate is user-supplied and may throw for any
                    // reason (e.g., disposed underlying resource, transient I/O error).
                    // This runs inside a System.Threading.Timer callback every 1 second;
                    // an unhandled exception here would crash the process. We catch
                    // Exception (not bare catch) to avoid swallowing SEH / CLR-critical
                    // exceptions on runtimes that propagate them. There is deliberately no
                    // logging to avoid flooding when the delegate is persistently broken
                    // — callers can detect the issue via the stale gauge value.
                    return;
                }

                _eventSource.RecordValue(_metricName, value);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _timer.Dispose();
            }
        }

        private sealed class EventCounterCounter : ICounter<long>
        {
            private readonly TelemetryEventSource _eventSource;
            private readonly string _metricName;
            private readonly MetricTagCardinalityTracker? _cardinalityTracker;
            private readonly ConcurrentDictionary<string, CounterTotal> _totals;
            private readonly int _maxTrackedTotals;

            public EventCounterCounter(
                TelemetryEventSource eventSource,
                string metricName,
                MetricTagCardinalityTracker? cardinalityTracker,
                int maxTrackedTotals = 1000)
            {
                _eventSource = eventSource;
                _metricName = metricName;
                _cardinalityTracker = cardinalityTracker;
                _maxTrackedTotals = maxTrackedTotals;
                _totals = new ConcurrentDictionary<string, CounterTotal>();
            }

            public void Add(long value)
            {
                ValidateNonNegative(value);
                var total = AddToTotal(_metricName, value);
                _eventSource.IncrementValue(_metricName, total);
            }

            public void Add(long value, in MetricTag tag1)
            {
                ValidateNonNegative(value);
                tag1.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1);
                var taggedName = MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1);
                var total = AddToTotal(taggedName, value);
                _eventSource.IncrementValue(taggedName, total);
            }

            public void Add(long value, in MetricTag tag1, in MetricTag tag2)
            {
                ValidateNonNegative(value);
                tag1.Validate();
                tag2.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2);
                var taggedName = MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1, in tag2);
                var total = AddToTotal(taggedName, value);
                _eventSource.IncrementValue(taggedName, total);
            }

            public void Add(long value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
            {
                ValidateNonNegative(value);
                tag1.Validate();
                tag2.Validate();
                tag3.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2, in tag3);
                var taggedName = MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1, in tag2, in tag3);
                var total = AddToTotal(taggedName, value);
                _eventSource.IncrementValue(taggedName, total);
            }

            public void Add(long value, params MetricTag[] tags)
            {
                ValidateNonNegative(value);

                if (tags == null || tags.Length == 0)
                {
                    var total = AddToTotal(_metricName, value);
                    _eventSource.IncrementValue(_metricName, total);
                    return;
                }

                MetricTag.ValidateTags(tags);
                _cardinalityTracker?.Track(_metricName, tags);
                var taggedName = MetricTagKeyBuilder.BuildTaggedName(_metricName, tags);
                var taggedTotal = AddToTotal(taggedName, value);
                _eventSource.IncrementValue(taggedName, taggedTotal);
            }

            private long AddToTotal(string key, long value)
            {
                // Fast path: try to get existing total
                if (_totals.TryGetValue(key, out var counterTotal))
                {
                    return counterTotal.Add(value);
                }

                // Prevent unbounded growth by capping total tracked tag combinations
                if (_totals.Count >= _maxTrackedTotals)
                {
                    // Return the value itself as the total when limit reached
                    return value;
                }

                // Slow path: create new total holder
                counterTotal = _totals.GetOrAdd(key, _ => new CounterTotal());
                return counterTotal.Add(value);
            }

            private static void ValidateNonNegative(long value)
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Counter values must be non-negative.");
            }
        }

        private sealed class CounterTotal
        {
            private long _value;

            public long Add(long increment)
            {
                return Interlocked.Add(ref _value, increment);
            }
        }

        private sealed class EventCounterHistogram : IHistogram<long>
        {
            private readonly TelemetryEventSource _eventSource;
            private readonly string _metricName;
            private readonly MetricTagCardinalityTracker? _cardinalityTracker;

            public EventCounterHistogram(
                TelemetryEventSource eventSource,
                string metricName,
                MetricTagCardinalityTracker? cardinalityTracker)
            {
                _eventSource = eventSource;
                _metricName = metricName;
                _cardinalityTracker = cardinalityTracker;
            }

            public void Record(long value)
            {
                _eventSource.RecordValue(_metricName, value);
            }

            public void Record(long value, in MetricTag tag1)
            {
                tag1.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1);
                _eventSource.RecordValue(MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1), value);
            }

            public void Record(long value, in MetricTag tag1, in MetricTag tag2)
            {
                tag1.Validate();
                tag2.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2);
                _eventSource.RecordValue(MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1, in tag2), value);
            }

            public void Record(long value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
            {
                tag1.Validate();
                tag2.Validate();
                tag3.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2, in tag3);
                _eventSource.RecordValue(
                    MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1, in tag2, in tag3),
                    value);
            }

            public void Record(long value, params MetricTag[] tags)
            {
                if (tags == null || tags.Length == 0)
                {
                    _eventSource.RecordValue(_metricName, value);
                    return;
                }

                MetricTag.ValidateTags(tags);
                _cardinalityTracker?.Track(_metricName, tags);
                _eventSource.RecordValue(MetricTagKeyBuilder.BuildTaggedName(_metricName, tags), value);
            }
        }

        private sealed class EventCounterHistogramDouble : IHistogram<double>
        {
            private readonly TelemetryEventSource _eventSource;
            private readonly string _metricName;
            private readonly MetricTagCardinalityTracker? _cardinalityTracker;

            public EventCounterHistogramDouble(
                TelemetryEventSource eventSource,
                string metricName,
                MetricTagCardinalityTracker? cardinalityTracker)
            {
                _eventSource = eventSource;
                _metricName = metricName;
                _cardinalityTracker = cardinalityTracker;
            }

            public void Record(double value)
            {
                _eventSource.RecordValue(_metricName, value);
            }

            public void Record(double value, in MetricTag tag1)
            {
                tag1.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1);
                _eventSource.RecordValue(MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1), value);
            }

            public void Record(double value, in MetricTag tag1, in MetricTag tag2)
            {
                tag1.Validate();
                tag2.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2);
                _eventSource.RecordValue(MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1, in tag2), value);
            }

            public void Record(double value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
            {
                tag1.Validate();
                tag2.Validate();
                tag3.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2, in tag3);
                _eventSource.RecordValue(
                    MetricTagKeyBuilder.BuildTaggedName(_metricName, in tag1, in tag2, in tag3),
                    value);
            }

            public void Record(double value, params MetricTag[] tags)
            {
                if (tags == null || tags.Length == 0)
                {
                    _eventSource.RecordValue(_metricName, value);
                    return;
                }

                MetricTag.ValidateTags(tags);
                _cardinalityTracker?.Track(_metricName, tags);
                _eventSource.RecordValue(MetricTagKeyBuilder.BuildTaggedName(_metricName, tags), value);
            }
        }
    }
}
