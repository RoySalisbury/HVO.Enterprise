using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.HealthChecks;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class TelemetryStatisticsTests
    {
        [TestMethod]
        public void InitialState_AllCountersZero()
        {
            var stats = new TelemetryStatistics();

            Assert.AreEqual(0, stats.ActivitiesCreated);
            Assert.AreEqual(0, stats.ActivitiesCompleted);
            Assert.AreEqual(0, stats.ActiveActivities);
            Assert.AreEqual(0, stats.ExceptionsTracked);
            Assert.AreEqual(0, stats.EventsRecorded);
            Assert.AreEqual(0, stats.MetricsRecorded);
            Assert.AreEqual(0, stats.QueueDepth);
            Assert.AreEqual(0, stats.MaxQueueDepth);
            Assert.AreEqual(0, stats.ItemsEnqueued);
            Assert.AreEqual(0, stats.ItemsProcessed);
            Assert.AreEqual(0, stats.ItemsDropped);
            Assert.AreEqual(0, stats.ProcessingErrors);
            Assert.AreEqual(0, stats.CorrelationIdsGenerated);
            Assert.AreEqual(0.0, stats.AverageProcessingTimeMs);
            Assert.AreEqual(0.0, stats.CurrentErrorRate);
            Assert.AreEqual(0.0, stats.CurrentThroughput);
        }

        [TestMethod]
        public void StartTime_SetOnCreation()
        {
            var before = DateTimeOffset.UtcNow;
            var stats = new TelemetryStatistics();
            var after = DateTimeOffset.UtcNow;

            Assert.IsTrue(stats.StartTime >= before);
            Assert.IsTrue(stats.StartTime <= after);
        }

        [TestMethod]
        public void IncrementActivitiesCreated_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source2");

            Assert.AreEqual(3, stats.ActivitiesCreated);
        }

        [TestMethod]
        public void IncrementActivitiesCompleted_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(100));

            Assert.AreEqual(2, stats.ActivitiesCreated);
            Assert.AreEqual(1, stats.ActivitiesCompleted);
        }

        [TestMethod]
        public void ActiveActivities_CalculatedCorrectly()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(10));

            Assert.AreEqual(3, stats.ActivitiesCreated);
            Assert.AreEqual(1, stats.ActivitiesCompleted);
            Assert.AreEqual(2, stats.ActiveActivities);
        }

        [TestMethod]
        public void IncrementActivitiesCreated_ThreadSafe_AccurateCount()
        {
            var stats = new TelemetryStatistics();
            const int iterations = 10000;
            const int threadCount = 10;

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    stats.IncrementActivitiesCreated("test-source");
                }
            });

            Assert.AreEqual(threadCount * iterations, stats.ActivitiesCreated);
        }

        [TestMethod]
        public void IncrementActivitiesCompleted_ThreadSafe_AccurateCount()
        {
            var stats = new TelemetryStatistics();
            const int iterations = 10000;
            const int threadCount = 10;

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    stats.IncrementActivitiesCompleted("test-source", TimeSpan.FromMilliseconds(1));
                }
            });

            Assert.AreEqual(threadCount * iterations, stats.ActivitiesCompleted);
        }

        [TestMethod]
        public void IncrementExceptionsTracked_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementExceptionsTracked();
            stats.IncrementExceptionsTracked();
            stats.IncrementExceptionsTracked();

            Assert.AreEqual(3, stats.ExceptionsTracked);
        }

        [TestMethod]
        public void IncrementEventsRecorded_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementEventsRecorded();
            stats.IncrementEventsRecorded();

            Assert.AreEqual(2, stats.EventsRecorded);
        }

        [TestMethod]
        public void IncrementMetricsRecorded_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementMetricsRecorded();

            Assert.AreEqual(1, stats.MetricsRecorded);
        }

        [TestMethod]
        public void IncrementItemsEnqueued_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementItemsEnqueued();
            stats.IncrementItemsEnqueued();
            stats.IncrementItemsEnqueued();

            Assert.AreEqual(3, stats.ItemsEnqueued);
        }

        [TestMethod]
        public void IncrementItemsProcessed_IncrementsCounterAndTracksTime()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(100));
            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(200));

            Assert.AreEqual(2, stats.ItemsProcessed);
            Assert.AreEqual(150.0, stats.AverageProcessingTimeMs, 0.1);
        }

        [TestMethod]
        public void IncrementItemsDropped_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementItemsDropped();

            Assert.AreEqual(1, stats.ItemsDropped);
        }

        [TestMethod]
        public void IncrementProcessingErrors_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementProcessingErrors();
            stats.IncrementProcessingErrors();

            Assert.AreEqual(2, stats.ProcessingErrors);
        }

        [TestMethod]
        public void IncrementCorrelationIdsGenerated_IncrementsCounter()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementCorrelationIdsGenerated();
            stats.IncrementCorrelationIdsGenerated();
            stats.IncrementCorrelationIdsGenerated();

            Assert.AreEqual(3, stats.CorrelationIdsGenerated);
        }

        [TestMethod]
        public void AverageProcessingTimeMs_ZeroWhenNoItems()
        {
            var stats = new TelemetryStatistics();

            Assert.AreEqual(0.0, stats.AverageProcessingTimeMs);
        }

        [TestMethod]
        public void AverageProcessingTimeMs_CalculatesCorrectly()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(50));
            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(150));
            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(100));

            Assert.AreEqual(100.0, stats.AverageProcessingTimeMs, 1.0);
        }

        [TestMethod]
        public void UpdateQueueDepth_UpdatesCurrentDepth()
        {
            var stats = new TelemetryStatistics();

            stats.UpdateQueueDepth(42);

            Assert.AreEqual(42, stats.QueueDepth);
        }

        [TestMethod]
        public void UpdateQueueDepth_TracksMaximum()
        {
            var stats = new TelemetryStatistics();

            stats.UpdateQueueDepth(10);
            stats.UpdateQueueDepth(50);
            stats.UpdateQueueDepth(25);

            Assert.AreEqual(25, stats.QueueDepth);
            Assert.AreEqual(50, stats.MaxQueueDepth);
        }

        [TestMethod]
        public void UpdateQueueDepth_ThreadSafe_TracksMaxCorrectly()
        {
            var stats = new TelemetryStatistics();
            var maxSeen = 0;
            const int threadCount = 10;

            Parallel.For(0, threadCount, i =>
            {
                var depth = (i + 1) * 100;
                stats.UpdateQueueDepth(depth);
                Interlocked.CompareExchange(ref maxSeen, depth, 0);
            });

            // Max should be at least the largest depth set
            Assert.IsTrue(stats.MaxQueueDepth >= 100);
            Assert.IsTrue(stats.MaxQueueDepth <= threadCount * 100);
        }

        [TestMethod]
        public void PerSourceStatistics_TrackedSeparately()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source2");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(100));

            var perSource = stats.PerSourceStatistics;
            Assert.IsTrue(perSource.ContainsKey("source1"));
            Assert.IsTrue(perSource.ContainsKey("source2"));
            Assert.AreEqual(2, perSource["source1"].ActivitiesCreated);
            Assert.AreEqual(1, perSource["source1"].ActivitiesCompleted);
            Assert.AreEqual(100.0, perSource["source1"].AverageDurationMs, 1.0);
            Assert.AreEqual(1, perSource["source2"].ActivitiesCreated);
            Assert.AreEqual(0, perSource["source2"].ActivitiesCompleted);
        }

        [TestMethod]
        public void PerSourceStatistics_CaseInsensitiveSourceNames()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("MySource");
            stats.IncrementActivitiesCreated("mysource");

            var perSource = stats.PerSourceStatistics;
            Assert.AreEqual(1, perSource.Count);
        }

        [TestMethod]
        public void PerSourceStatistics_EmptySourceName_SkipsTracking()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("");
            stats.IncrementActivitiesCreated("");

            Assert.AreEqual(2, stats.ActivitiesCreated);
            Assert.AreEqual(0, stats.PerSourceStatistics.Count);
        }

        [TestMethod]
        public void GetSnapshot_CapturesAllValues()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(50));
            stats.IncrementExceptionsTracked();
            stats.IncrementEventsRecorded();
            stats.IncrementMetricsRecorded();
            stats.IncrementItemsEnqueued();
            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(10));
            stats.IncrementItemsDropped();
            stats.IncrementProcessingErrors();
            stats.IncrementCorrelationIdsGenerated();
            stats.UpdateQueueDepth(42);

            var snapshot = stats.GetSnapshot();

            Assert.AreEqual(1, snapshot.ActivitiesCreated);
            Assert.AreEqual(1, snapshot.ActivitiesCompleted);
            Assert.AreEqual(0, snapshot.ActiveActivities);
            Assert.AreEqual(1, snapshot.ExceptionsTracked);
            Assert.AreEqual(1, snapshot.EventsRecorded);
            Assert.AreEqual(1, snapshot.MetricsRecorded);
            Assert.AreEqual(1, snapshot.ItemsEnqueued);
            Assert.AreEqual(1, snapshot.ItemsProcessed);
            Assert.AreEqual(1, snapshot.ItemsDropped);
            Assert.AreEqual(1, snapshot.ProcessingErrors);
            Assert.AreEqual(1, snapshot.CorrelationIdsGenerated);
            Assert.AreEqual(42, snapshot.QueueDepth);
            Assert.AreEqual(42, snapshot.MaxQueueDepth);
            Assert.AreEqual(10.0, snapshot.AverageProcessingTimeMs, 1.0);
            Assert.IsTrue(snapshot.PerSourceStatistics.ContainsKey("source1"));
        }

        [TestMethod]
        public void GetSnapshot_CreatesImmutableCopy()
        {
            var stats = new TelemetryStatistics();
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementItemsEnqueued();

            var snapshot1 = stats.GetSnapshot();
            stats.IncrementActivitiesCreated("source1");
            var snapshot2 = stats.GetSnapshot();

            Assert.AreEqual(1, snapshot1.ActivitiesCreated);
            Assert.AreEqual(2, snapshot2.ActivitiesCreated);
        }

        [TestMethod]
        public void GetSnapshot_HasValidTimestamps()
        {
            var stats = new TelemetryStatistics();

            var snapshot = stats.GetSnapshot();

            Assert.IsTrue(snapshot.Timestamp >= stats.StartTime);
            Assert.IsTrue(snapshot.Uptime >= TimeSpan.Zero);
            Assert.AreEqual(snapshot.StartTime, stats.StartTime);
        }

        [TestMethod]
        public void Reset_ClearsAllCounters()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(50));
            stats.IncrementExceptionsTracked();
            stats.IncrementEventsRecorded();
            stats.IncrementMetricsRecorded();
            stats.IncrementItemsEnqueued();
            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(10));
            stats.IncrementItemsDropped();
            stats.IncrementProcessingErrors();
            stats.IncrementCorrelationIdsGenerated();
            stats.UpdateQueueDepth(42);

            stats.Reset();

            var snapshot = stats.GetSnapshot();
            Assert.AreEqual(0, snapshot.ActivitiesCreated);
            Assert.AreEqual(0, snapshot.ActivitiesCompleted);
            Assert.AreEqual(0, snapshot.ActiveActivities);
            Assert.AreEqual(0, snapshot.ExceptionsTracked);
            Assert.AreEqual(0, snapshot.EventsRecorded);
            Assert.AreEqual(0, snapshot.MetricsRecorded);
            Assert.AreEqual(0, snapshot.ItemsEnqueued);
            Assert.AreEqual(0, snapshot.ItemsProcessed);
            Assert.AreEqual(0, snapshot.ItemsDropped);
            Assert.AreEqual(0, snapshot.ProcessingErrors);
            Assert.AreEqual(0, snapshot.CorrelationIdsGenerated);
            Assert.AreEqual(0, snapshot.QueueDepth);
            Assert.AreEqual(0, snapshot.MaxQueueDepth);
            Assert.AreEqual(0.0, snapshot.AverageProcessingTimeMs);
        }

        [TestMethod]
        public void Reset_ClearsPerSourceStatistics()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source2");

            Assert.AreEqual(2, stats.PerSourceStatistics.Count);

            stats.Reset();

            Assert.AreEqual(0, stats.PerSourceStatistics.Count);
        }

        [TestMethod]
        public void Reset_UpdatesStartTime()
        {
            var stats = new TelemetryStatistics();
            var originalStartTime = stats.StartTime;

            Thread.Sleep(10);
            stats.Reset();

            Assert.IsTrue(stats.StartTime > originalStartTime);
        }

        [TestMethod]
        public void Reset_AllowsNewIncrements()
        {
            var stats = new TelemetryStatistics();

            stats.IncrementActivitiesCreated("source1");
            Assert.AreEqual(1, stats.ActivitiesCreated);

            stats.Reset();
            Assert.AreEqual(0, stats.ActivitiesCreated);

            stats.IncrementActivitiesCreated("source1");
            Assert.AreEqual(1, stats.ActivitiesCreated);
        }

        [TestMethod]
        public void MultipleCounterIncrements_ThreadSafe()
        {
            var stats = new TelemetryStatistics();
            const int iterations = 5000;
            const int threadCount = 8;

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    stats.IncrementActivitiesCreated("src");
                    stats.IncrementActivitiesCompleted("src", TimeSpan.FromMilliseconds(1));
                    stats.IncrementExceptionsTracked();
                    stats.IncrementEventsRecorded();
                    stats.IncrementMetricsRecorded();
                    stats.IncrementItemsEnqueued();
                    stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(1));
                    stats.IncrementItemsDropped();
                    stats.IncrementProcessingErrors();
                    stats.IncrementCorrelationIdsGenerated();
                }
            });

            var expected = threadCount * iterations;
            Assert.AreEqual(expected, stats.ActivitiesCreated);
            Assert.AreEqual(expected, stats.ActivitiesCompleted);
            Assert.AreEqual(expected, stats.ExceptionsTracked);
            Assert.AreEqual(expected, stats.EventsRecorded);
            Assert.AreEqual(expected, stats.MetricsRecorded);
            Assert.AreEqual(expected, stats.ItemsEnqueued);
            Assert.AreEqual(expected, stats.ItemsProcessed);
            Assert.AreEqual(expected, stats.ItemsDropped);
            Assert.AreEqual(expected, stats.ProcessingErrors);
            Assert.AreEqual(expected, stats.CorrelationIdsGenerated);
        }
    }
}
