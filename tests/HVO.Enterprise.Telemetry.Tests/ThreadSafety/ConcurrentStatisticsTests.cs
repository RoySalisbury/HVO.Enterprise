using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.ThreadSafety
{
    /// <summary>
    /// Validates that <see cref="TelemetryStatistics"/> handles concurrent increments
    /// and reads correctly using its Interlocked-based implementation.
    /// </summary>
    [TestClass]
    public class ConcurrentStatisticsTests
    {
        [TestMethod]
        public void TelemetryStatistics_ConcurrentIncrements_AreAccurate()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            const int threadCount = 20;
            const int incrementsPerThread = 1000;

            // Act
            TestHelpers.RunConcurrently(threadCount, _ =>
            {
                for (int i = 0; i < incrementsPerThread; i++)
                {
                    stats.IncrementActivitiesCreated("source1");
                    stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(1));
                    stats.IncrementExceptionsTracked();
                    stats.IncrementEventsRecorded();
                    stats.IncrementMetricsRecorded();
                    stats.IncrementItemsEnqueued();
                    stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(1));
                    stats.IncrementCorrelationIdsGenerated();
                }
            });

            // Assert — verify exact counts
            long expected = threadCount * incrementsPerThread;
            Assert.AreEqual(expected, stats.ActivitiesCreated);
            Assert.AreEqual(expected, stats.ActivitiesCompleted);
            Assert.AreEqual(expected, stats.ExceptionsTracked);
            Assert.AreEqual(expected, stats.EventsRecorded);
            Assert.AreEqual(expected, stats.MetricsRecorded);
            Assert.AreEqual(expected, stats.ItemsEnqueued);
            Assert.AreEqual(expected, stats.ItemsProcessed);
            Assert.AreEqual(expected, stats.CorrelationIdsGenerated);
            Assert.AreEqual(0, stats.ActiveActivities); // Created == Completed
        }

        [TestMethod]
        public void TelemetryStatistics_ConcurrentQueueDepthUpdates_TracksMaximum()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            const int threadCount = 20;
            const int maxDepth = 100;

            // Act - each thread writes increasing then decreasing depths
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                for (int depth = 0; depth <= maxDepth; depth++)
                {
                    stats.UpdateQueueDepth(depth);
                }
                for (int depth = maxDepth; depth >= 0; depth--)
                {
                    stats.UpdateQueueDepth(depth);
                }
            });

            // Assert
            Assert.IsTrue(stats.MaxQueueDepth >= maxDepth,
                $"MaxQueueDepth should be at least {maxDepth}, was {stats.MaxQueueDepth}");
        }

        [TestMethod]
        public void TelemetryStatistics_ConcurrentPerSourceStats_AccumulateCorrectly()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            const int threadCount = 10;
            const int opsPerThread = 200;
            var sources = new[] { "source-A", "source-B", "source-C" };

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                var source = sources[index % sources.Length];
                for (int i = 0; i < opsPerThread; i++)
                {
                    stats.IncrementActivitiesCreated(source);
                    stats.IncrementActivitiesCompleted(source, TimeSpan.FromMilliseconds(5));
                }
            });

            // Assert - total across all sources
            long totalExpected = threadCount * opsPerThread;
            Assert.AreEqual(totalExpected, stats.ActivitiesCreated);

            var perSource = stats.PerSourceStatistics;
            Assert.IsTrue(perSource.Count <= sources.Length);

            long sumCreated = perSource.Values.Sum(s => s.ActivitiesCreated);
            Assert.AreEqual(totalExpected, sumCreated);
        }

        [TestMethod]
        public void TelemetryStatistics_ConcurrentSnapshotWhileIncrementing_DoesNotThrow()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            const int writerThreads = 10;
            const int readerThreads = 5;
            const int opsPerThread = 500;
            var snapshots = new ConcurrentBag<TelemetryStatisticsSnapshot>();
            var cts = new CancellationTokenSource();

            // Act - writers increment while readers take snapshots
            var writers = new Thread[writerThreads];
            var readers = new Thread[readerThreads];

            for (int i = 0; i < writerThreads; i++)
            {
                writers[i] = new Thread(() =>
                {
                    for (int j = 0; j < opsPerThread; j++)
                    {
                        stats.IncrementActivitiesCreated("snapshot-test");
                        stats.IncrementMetricsRecorded();
                    }
                });
                writers[i].Start();
            }

            for (int i = 0; i < readerThreads; i++)
            {
                readers[i] = new Thread(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        snapshots.Add(stats.GetSnapshot());
                        Thread.SpinWait(10);
                    }
                });
                readers[i].Start();
            }

            foreach (var w in writers) w.Join();
            cts.Cancel();
            foreach (var r in readers) r.Join();

            // Assert
            Assert.IsTrue(snapshots.Count > 0, "Should have captured at least one snapshot");
            var finalSnapshot = stats.GetSnapshot();
            Assert.AreEqual(writerThreads * opsPerThread, finalSnapshot.ActivitiesCreated);
            Assert.AreEqual(writerThreads * opsPerThread, finalSnapshot.MetricsRecorded);
        }

        [TestMethod]
        public void TelemetryStatistics_ResetDuringConcurrentIncrements_ResetsToZero()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            const int threadCount = 10;
            const int opsPerThread = 500;

            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                stats.IncrementActivitiesCreated("pre");
                stats.IncrementExceptionsTracked();
            }

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                for (int i = 0; i < opsPerThread; i++)
                {
                    stats.IncrementActivitiesCreated("post");

                    // One thread resets midway
                    if (index == 0 && i == opsPerThread / 2)
                    {
                        stats.Reset();
                    }
                }
            });

            // Assert - after reset, counts should be less than if no reset happened
            long maxPossible = 100 + (threadCount * opsPerThread);
            Assert.IsTrue(stats.ActivitiesCreated < maxPossible,
                "Reset should have zeroed some counters");
        }
    }
}
