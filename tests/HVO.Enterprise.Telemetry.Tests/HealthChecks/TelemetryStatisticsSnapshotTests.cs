using System;
using HVO.Enterprise.Telemetry.HealthChecks;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class TelemetryStatisticsSnapshotTests
    {
        [TestMethod]
        public void Uptime_CalculatedFromTimestamps()
        {
            var startTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var timestamp = new DateTimeOffset(2026, 1, 1, 2, 30, 0, TimeSpan.Zero);

            var snapshot = new TelemetryStatisticsSnapshot
            {
                StartTime = startTime,
                Timestamp = timestamp
            };

            Assert.AreEqual(TimeSpan.FromHours(2.5), snapshot.Uptime);
        }

        [TestMethod]
        public void ToFormattedString_ContainsAllSections()
        {
            var snapshot = new TelemetryStatisticsSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                ActivitiesCreated = 1000,
                ActivitiesCompleted = 950,
                ActiveActivities = 50,
                ExceptionsTracked = 5,
                EventsRecorded = 200,
                MetricsRecorded = 500,
                QueueDepth = 42,
                MaxQueueDepth = 100,
                ItemsEnqueued = 10000,
                ItemsProcessed = 9990,
                ItemsDropped = 10,
                ProcessingErrors = 3,
                AverageProcessingTimeMs = 5.5,
                CorrelationIdsGenerated = 300,
                CurrentErrorRate = 0.5,
                CurrentThroughput = 16.7
            };

            var formatted = snapshot.ToFormattedString();

            Assert.IsTrue(formatted.Contains("Telemetry Statistics"));
            Assert.IsTrue(formatted.Contains("Activities:"));
            Assert.IsTrue(formatted.Contains("Created: 1,000"));
            Assert.IsTrue(formatted.Contains("Completed: 950"));
            Assert.IsTrue(formatted.Contains("Active: 50"));
            Assert.IsTrue(formatted.Contains("Queue:"));
            Assert.IsTrue(formatted.Contains("Current Depth: 42"));
            Assert.IsTrue(formatted.Contains("Max Depth: 100"));
            Assert.IsTrue(formatted.Contains("Avg Processing: 5.50ms"));
            Assert.IsTrue(formatted.Contains("Errors & Events:"));
            Assert.IsTrue(formatted.Contains("Exceptions: 5"));
            Assert.IsTrue(formatted.Contains("Rates:"));
            Assert.IsTrue(formatted.Contains("Error Rate: 0.50/sec"));
            Assert.IsTrue(formatted.Contains("Throughput: 16.70/sec"));
        }

        [TestMethod]
        public void ToFormattedString_HandlesZeroValues()
        {
            var snapshot = new TelemetryStatisticsSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                StartTime = DateTimeOffset.UtcNow
            };

            var formatted = snapshot.ToFormattedString();

            Assert.IsNotNull(formatted);
            Assert.IsTrue(formatted.Contains("Created: 0"));
            Assert.IsTrue(formatted.Contains("Throughput: 0.00/sec"));
        }

        [TestMethod]
        public void DefaultPerSourceStatistics_IsEmptyDictionary()
        {
            var snapshot = new TelemetryStatisticsSnapshot();

            Assert.IsNotNull(snapshot.PerSourceStatistics);
            Assert.AreEqual(0, snapshot.PerSourceStatistics.Count);
        }
    }
}
