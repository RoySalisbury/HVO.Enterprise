using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Metric recorder using System.Diagnostics.Metrics for modern runtimes.
    /// </summary>
    internal sealed class MeterApiRecorder : IMetricRecorder, IDisposable
    {
        internal const string MeterName = "HVO.Enterprise.Telemetry";
        internal const string MeterVersion = "1.0.0";

        private readonly Meter _meter;
        private readonly MetricTagCardinalityTracker? _cardinalityTracker;

        internal MeterApiRecorder(ILogger<MeterApiRecorder>? logger = null)
        {
            var effectiveLogger = logger ?? NullLogger<MeterApiRecorder>.Instance;

            _meter = new Meter(MeterName, MeterVersion);
            if (effectiveLogger.IsEnabled(LogLevel.Warning))
                _cardinalityTracker = new MetricTagCardinalityTracker(effectiveLogger);
        }

        public ICounter<long> CreateCounter(string name, string? unit = null, string? description = null)
        {
            MetricNameValidator.ValidateName(name, nameof(name));

            var counter = _meter.CreateCounter<long>(name, unit, description);
            return new MeterCounter(counter, name, _cardinalityTracker);
        }

        public IHistogram<long> CreateHistogram(string name, string? unit = null, string? description = null)
        {
            MetricNameValidator.ValidateName(name, nameof(name));

            var histogram = _meter.CreateHistogram<long>(name, unit, description);
            return new MeterHistogram(histogram, name, _cardinalityTracker);
        }

        public IHistogram<double> CreateHistogramDouble(string name, string? unit = null, string? description = null)
        {
            MetricNameValidator.ValidateName(name, nameof(name));

            var histogram = _meter.CreateHistogram<double>(name, unit, description);
            return new MeterHistogramDouble(histogram, name, _cardinalityTracker);
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

            var state = new ObservableGaugeState(observeValue);
            _ = _meter.CreateObservableGauge(name, state.Observe, unit, description);
            return new ObservableGaugeHandle(state);
        }

        /// <summary>
        /// Disposes the underlying <see cref="Meter"/> and releases resources.
        /// </summary>
        public void Dispose()
        {
            _meter.Dispose();
        }

        private sealed class ObservableGaugeState
        {
            private readonly Func<double> _observeValue;
            private volatile bool _disposed;

            public ObservableGaugeState(Func<double> observeValue)
            {
                if (observeValue == null)
                    throw new ArgumentNullException(nameof(observeValue));

                _observeValue = observeValue;
            }

            public double Observe()
            {
                if (_disposed)
                    return 0d;

                return _observeValue();
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }

        private sealed class ObservableGaugeHandle : IDisposable
        {
            private readonly ObservableGaugeState _state;

            public ObservableGaugeHandle(ObservableGaugeState state)
            {
                _state = state ?? throw new ArgumentNullException(nameof(state));
            }

            public void Dispose()
            {
                _state.Dispose();
            }
        }

        private sealed class MeterCounter : ICounter<long>
        {
            private readonly Counter<long> _counter;
            private readonly string _metricName;
            private readonly MetricTagCardinalityTracker? _cardinalityTracker;

            public MeterCounter(
                Counter<long> counter,
                string metricName,
                MetricTagCardinalityTracker? cardinalityTracker)
            {
                _counter = counter;
                _metricName = metricName;
                _cardinalityTracker = cardinalityTracker;
            }

            public void Add(long value)
            {
                ValidateNonNegative(value);
                _counter.Add(value);
            }

            public void Add(long value, in MetricTag tag1)
            {
                ValidateNonNegative(value);
                tag1.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1);
                _counter.Add(value, new KeyValuePair<string, object?>(tag1.Key, tag1.Value));
            }

            public void Add(long value, in MetricTag tag1, in MetricTag tag2)
            {
                ValidateNonNegative(value);
                tag1.Validate();
                tag2.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2);
                _counter.Add(value,
                    new KeyValuePair<string, object?>(tag1.Key, tag1.Value),
                    new KeyValuePair<string, object?>(tag2.Key, tag2.Value));
            }

            public void Add(long value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
            {
                ValidateNonNegative(value);
                tag1.Validate();
                tag2.Validate();
                tag3.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2, in tag3);
                _counter.Add(value,
                    new KeyValuePair<string, object?>(tag1.Key, tag1.Value),
                    new KeyValuePair<string, object?>(tag2.Key, tag2.Value),
                    new KeyValuePair<string, object?>(tag3.Key, tag3.Value));
            }

            public void Add(long value, params MetricTag[] tags)
            {
                ValidateNonNegative(value);

                if (tags == null || tags.Length == 0)
                {
                    _counter.Add(value);
                    return;
                }

                MetricTag.ValidateTags(tags);
                _cardinalityTracker?.Track(_metricName, tags);

                var pairs = new KeyValuePair<string, object?>[tags.Length];
                for (int i = 0; i < tags.Length; i++)
                {
                    pairs[i] = new KeyValuePair<string, object?>(tags[i].Key, tags[i].Value);
                }

                _counter.Add(value, pairs);
            }

            private static void ValidateNonNegative(long value)
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Counter values must be non-negative.");
            }
        }

        private sealed class MeterHistogram : IHistogram<long>
        {
            private readonly Histogram<long> _histogram;
            private readonly string _metricName;
            private readonly MetricTagCardinalityTracker? _cardinalityTracker;

            public MeterHistogram(
                Histogram<long> histogram,
                string metricName,
                MetricTagCardinalityTracker? cardinalityTracker)
            {
                _histogram = histogram;
                _metricName = metricName;
                _cardinalityTracker = cardinalityTracker;
            }

            public void Record(long value)
            {
                _histogram.Record(value);
            }

            public void Record(long value, in MetricTag tag1)
            {
                tag1.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1);
                _histogram.Record(value, new KeyValuePair<string, object?>(tag1.Key, tag1.Value));
            }

            public void Record(long value, in MetricTag tag1, in MetricTag tag2)
            {
                tag1.Validate();
                tag2.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2);
                _histogram.Record(value,
                    new KeyValuePair<string, object?>(tag1.Key, tag1.Value),
                    new KeyValuePair<string, object?>(tag2.Key, tag2.Value));
            }

            public void Record(long value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
            {
                tag1.Validate();
                tag2.Validate();
                tag3.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2, in tag3);
                _histogram.Record(value,
                    new KeyValuePair<string, object?>(tag1.Key, tag1.Value),
                    new KeyValuePair<string, object?>(tag2.Key, tag2.Value),
                    new KeyValuePair<string, object?>(tag3.Key, tag3.Value));
            }

            public void Record(long value, params MetricTag[] tags)
            {
                if (tags == null || tags.Length == 0)
                {
                    _histogram.Record(value);
                    return;
                }

                MetricTag.ValidateTags(tags);
                _cardinalityTracker?.Track(_metricName, tags);

                var pairs = new KeyValuePair<string, object?>[tags.Length];
                for (int i = 0; i < tags.Length; i++)
                {
                    pairs[i] = new KeyValuePair<string, object?>(tags[i].Key, tags[i].Value);
                }

                _histogram.Record(value, pairs);
            }
        }

        private sealed class MeterHistogramDouble : IHistogram<double>
        {
            private readonly Histogram<double> _histogram;
            private readonly string _metricName;
            private readonly MetricTagCardinalityTracker? _cardinalityTracker;

            public MeterHistogramDouble(
                Histogram<double> histogram,
                string metricName,
                MetricTagCardinalityTracker? cardinalityTracker)
            {
                _histogram = histogram;
                _metricName = metricName;
                _cardinalityTracker = cardinalityTracker;
            }

            public void Record(double value)
            {
                _histogram.Record(value);
            }

            public void Record(double value, in MetricTag tag1)
            {
                tag1.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1);
                _histogram.Record(value, new KeyValuePair<string, object?>(tag1.Key, tag1.Value));
            }

            public void Record(double value, in MetricTag tag1, in MetricTag tag2)
            {
                tag1.Validate();
                tag2.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2);
                _histogram.Record(value,
                    new KeyValuePair<string, object?>(tag1.Key, tag1.Value),
                    new KeyValuePair<string, object?>(tag2.Key, tag2.Value));
            }

            public void Record(double value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
            {
                tag1.Validate();
                tag2.Validate();
                tag3.Validate();
                _cardinalityTracker?.Track(_metricName, in tag1, in tag2, in tag3);
                _histogram.Record(value,
                    new KeyValuePair<string, object?>(tag1.Key, tag1.Value),
                    new KeyValuePair<string, object?>(tag2.Key, tag2.Value),
                    new KeyValuePair<string, object?>(tag3.Key, tag3.Value));
            }

            public void Record(double value, params MetricTag[] tags)
            {
                if (tags == null || tags.Length == 0)
                {
                    _histogram.Record(value);
                    return;
                }

                MetricTag.ValidateTags(tags);
                _cardinalityTracker?.Track(_metricName, tags);

                var pairs = new KeyValuePair<string, object?>[tags.Length];
                for (int i = 0; i < tags.Length; i++)
                {
                    pairs[i] = new KeyValuePair<string, object?>(tags[i].Key, tags[i].Value);
                }

                _histogram.Record(value, pairs);
            }
        }
    }
}
