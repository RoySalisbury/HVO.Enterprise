using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    [TestClass]
    public class MetricRecorderTests
    {
        [TestMethod]
        public void MetricRecorderFactory_Instance_UsesMeterApiOnNet8()
        {
            var recorder = MetricRecorderFactory.Instance;

            Assert.IsInstanceOfType(recorder, typeof(MeterApiRecorder));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MetricTag_WithEmptyKey_ThrowsException()
        {
            _ = new MetricTag(string.Empty, "value");
        }

        [TestMethod]
        public void MeterCounter_Add_EmitsMeasurementsWithTags()
        {
            var measurements = new List<long>();
            KeyValuePair<string, object?>[]? lastTags = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                measurements.Add(measurement);
                lastTags = tags.ToArray();
            });

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            var counter = recorder.CreateCounter("test.counter");

            counter.Add(5);
            counter.Add(3, new MetricTag("status", 200));

            Assert.AreEqual(2, measurements.Count);
            Assert.IsNotNull(lastTags);
            Assert.IsTrue(ContainsTag(lastTags!, "status", 200));
        }

        [TestMethod]
        public void MeterHistogram_Record_EmitsMeasurementsWithTags()
        {
            var measurements = new List<double>();
            KeyValuePair<string, object?>[]? lastTags = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                measurements.Add(measurement);
                lastTags = tags.ToArray();
            });

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            var histogram = recorder.CreateHistogramDouble("test.histogram");

            histogram.Record(10.5);
            histogram.Record(42.0, new MetricTag("endpoint", "/api"));

            Assert.AreEqual(2, measurements.Count);
            Assert.IsNotNull(lastTags);
            Assert.IsTrue(ContainsTag(lastTags!, "endpoint", "/api"));
        }

        [TestMethod]
        public void ObservableGauge_InvokesCallback()
        {
            var observeCount = 0;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            using var gauge = recorder.CreateObservableGauge("test.gauge", () =>
            {
                Interlocked.Increment(ref observeCount);
                return 1.0;
            });

            listener.RecordObservableInstruments();

            Assert.IsTrue(observeCount > 0, "Gauge callback should be invoked.");
        }

        [TestMethod]
        public void CardinalityTracker_LogsWarningAfterThreshold()
        {
            var logger = new ListLogger<MeterApiRecorder>();
            var recorder = new MeterApiRecorder(logger);
            var counter = recorder.CreateCounter("test.cardinality");

            for (var i = 0; i < 105; i++)
            {
                counter.Add(1, new MetricTag("user", i));
            }

            Assert.IsTrue(logger.ContainsWarning("tag cardinality"), "Expected cardinality warning to be logged.");
        }

        [TestMethod]
        public void EventCounterRecorder_AllowsBasicOperations()
        {
            var recorder = new EventCounterRecorder();

            var counter = recorder.CreateCounter("legacy.counter");
            var histogram = recorder.CreateHistogram("legacy.histogram");
            var histogramDouble = recorder.CreateHistogramDouble("legacy.histogram.double");

            counter.Add(1);
            counter.Add(2, new MetricTag("region", "east"));

            histogram.Record(10);
            histogram.Record(20, new MetricTag("status", 200));

            histogramDouble.Record(1.5);
            histogramDouble.Record(2.5, new MetricTag("type", "latency"));
        }

        private static bool ContainsTag(KeyValuePair<string, object?>[] tags, string key, object value)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Key == key && Equals(tags[i].Value, value))
                    return true;
            }

            return false;
        }

        private sealed class ListLogger<T> : ILogger<T>
        {
            private readonly List<(LogLevel Level, string Message)> _entries;

            public ListLogger()
            {
                _entries = new List<(LogLevel, string)>();
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }

            public bool ContainsWarning(string containsText)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Level == LogLevel.Warning &&
                        _entries[i].Message.IndexOf(containsText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}
