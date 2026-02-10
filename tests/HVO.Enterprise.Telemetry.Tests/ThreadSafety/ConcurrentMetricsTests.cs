using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using HVO.Enterprise.Telemetry.Metrics;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.ThreadSafety
{
    /// <summary>
    /// Validates that metric recording operations are thread-safe under high contention.
    /// </summary>
    [TestClass]
    public class ConcurrentMetricsTests
    {
        [TestMethod]
        public void MeterApiRecorder_ConcurrentCounterAdd_NoExceptions()
        {
            // Arrange
            using var recorder = new MeterApiRecorder();
            var counter = recorder.CreateCounter("test.concurrent.counter");
            const int threadCount = 20;
            const int incrementsPerThread = 1000;

            // Act - hammer the counter from many threads simultaneously
            TestHelpers.RunConcurrently(threadCount, _ =>
            {
                for (int i = 0; i < incrementsPerThread; i++)
                {
                    counter.Add(1);
                }
            });

            // Assert - no exception means thread-safe
            // (Counter<T> increments are inherently atomic in M.E.D.Metrics)
        }

        [TestMethod]
        public void MeterApiRecorder_ConcurrentCounterWithTags_NoExceptions()
        {
            // Arrange
            using var recorder = new MeterApiRecorder();
            var counter = recorder.CreateCounter("test.concurrent.tagged.counter");
            const int threadCount = 10;
            const int incrementsPerThread = 500;

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                var tag = new MetricTag("thread", index.ToString());
                for (int i = 0; i < incrementsPerThread; i++)
                {
                    counter.Add(1, tag);
                }
            });

            // Assert - no exception means thread-safe
        }

        [TestMethod]
        public void MeterApiRecorder_ConcurrentHistogramRecord_NoExceptions()
        {
            // Arrange
            using var recorder = new MeterApiRecorder();
            var histogram = recorder.CreateHistogram("test.concurrent.histogram");
            const int threadCount = 20;
            const int recordsPerThread = 500;

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                for (int i = 0; i < recordsPerThread; i++)
                {
                    histogram.Record(index * 100 + i);
                }
            });

            // Assert - no exceptions
        }

        [TestMethod]
        public void MeterApiRecorder_ConcurrentHistogramDouble_NoExceptions()
        {
            // Arrange
            using var recorder = new MeterApiRecorder();
            var histogram = recorder.CreateHistogramDouble("test.concurrent.histogram.double");
            const int threadCount = 20;
            const int recordsPerThread = 500;

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                for (int i = 0; i < recordsPerThread; i++)
                {
                    histogram.Record(index + i * 0.001);
                }
            });

            // Assert - no exceptions
        }

        [TestMethod]
        public void MeterApiRecorder_ConcurrentCreateCounters_NoExceptions()
        {
            // Arrange
            using var recorder = new MeterApiRecorder();
            const int threadCount = 20;

            // Act - create many counters concurrently
            var counters = new ConcurrentBag<ICounter<long>>();
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                var counter = recorder.CreateCounter($"test.counter.{index}");
                counters.Add(counter);
                counter.Add(1);
            });

            // Assert
            Assert.AreEqual(threadCount, counters.Count);
        }

        [TestMethod]
        public void MeterApiRecorder_ConcurrentObservableGaugeCreation_NoExceptions()
        {
            // Arrange
            using var recorder = new MeterApiRecorder();
            const int threadCount = 10;
            var disposables = new ConcurrentBag<IDisposable>();

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                var gauge = recorder.CreateObservableGauge(
                    $"test.gauge.{index}",
                    () => index * 1.0,
                    description: $"Gauge {index}");
                disposables.Add(gauge);
            });

            // Assert
            Assert.AreEqual(threadCount, disposables.Count);

            // Cleanup
            foreach (var d in disposables) { d.Dispose(); }
        }

        [TestMethod]
        public void MetricRecorderFactory_ConcurrentAccess_ReturnsSameInstance()
        {
            // Arrange
            const int threadCount = 50;
            var instances = new ConcurrentBag<IMetricRecorder>();

            // Act
            TestHelpers.RunConcurrently(threadCount, _ =>
            {
                instances.Add(MetricRecorderFactory.Instance);
            });

            // Assert - all threads got the same singleton
            var first = instances.First();
            Assert.IsTrue(instances.All(i => ReferenceEquals(i, first)),
                "MetricRecorderFactory.Instance should return the same singleton across all threads");
        }
    }
}
