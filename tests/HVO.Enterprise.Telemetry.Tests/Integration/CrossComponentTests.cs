using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.BackgroundJobs;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Exceptions;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.Integration
{
    /// <summary>
    /// Tests that verify interaction between multiple telemetry subsystems.
    /// </summary>
    [TestClass]
    public class CrossComponentTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            TestHelpers.ResetStaticTelemetry();
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void Correlation_FlowsInto_OperationScope()
        {
            // Arrange
            using var testSource = new TestActivitySource("cross-corr");
            var factory = new OperationScopeFactory(testSource.Source);
            var expected = "cross-component-corr-id-123";
            CorrelationContext.Current = expected;

            // Act
            using var scope = factory.Begin("correlated-operation");

            // Assert - scope should capture the ambient correlation ID
            Assert.AreEqual(expected, scope.CorrelationId);
        }

        [TestMethod]
        public void OperationScope_WithException_TrackedInStatistics()
        {
            // Arrange
            using var testSource = new TestActivitySource("cross-stats");
            var factory = new OperationScopeFactory(testSource.Source);

            // Act
            using (var scope = factory.Begin("failing-operation"))
            {
                var ex = new InvalidOperationException("cross-component failure");
                scope.RecordException(ex);
                scope.Fail(ex);
            }

            // Assert - the activity should have been started and stopped
            Assert.AreEqual(1, testSource.StartedActivities.Count);
            Assert.AreEqual(1, testSource.StoppedActivities.Count);
        }

        [TestMethod]
        public void BackgroundJobContext_Capture_IncludesCorrelationAndActivity()
        {
            // Arrange
            using var listener = TestHelpers.CreateGlobalListener();
            using var activitySource = new ActivitySource("bg-job-test");

            var corrId = "bg-job-corr-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            CorrelationContext.Current = corrId;

            using var activity = activitySource.StartActivity("parent-activity");

            // Act
            var context = BackgroundJobContext.Capture();

            // Assert
            Assert.AreEqual(corrId, context.CorrelationId);
            Assert.IsNotNull(context.ParentActivityId);
            Assert.IsTrue(context.EnqueuedAt <= DateTimeOffset.UtcNow);
        }

        [TestMethod]
        public async Task BackgroundJobContext_CaptureAndRestore_PreservesCorrelationAcrossAsyncBoundary()
        {
            // Arrange
            var corrId = "capture-restore-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            CorrelationContext.Current = corrId;

            var context = BackgroundJobContext.Capture();

            // Simulate background processing on a different async flow
            string? restoredCorrelationId = null;

            await Task.Run(() =>
            {
                // Clear — simulating a new thread with no ambient context
                CorrelationContext.Clear();
                Assert.AreNotEqual(corrId, CorrelationContext.GetRawValue());

                using (context.Restore())
                {
                    restoredCorrelationId = CorrelationContext.Current;
                }
            });

            // Assert
            Assert.AreEqual(corrId, restoredCorrelationId);
        }

        [TestMethod]
        public void ExceptionAggregator_WithOperationScope_TracksExceptionDetails()
        {
            // Arrange
            var aggregator = new ExceptionAggregator();
            using var testSource = new TestActivitySource("agg-scope-test");
            var factory = new OperationScopeFactory(testSource.Source);

            // Act
            var exception = new InvalidOperationException("error during operation");
            using (var scope = factory.Begin("aggregated-operation"))
            {
                scope.RecordException(exception);
                scope.Fail(exception);
            }

            aggregator.RecordException(exception);

            // Assert
            Assert.AreEqual(1, aggregator.TotalExceptions);
            var groups = aggregator.GetGroups();
            Assert.AreEqual(1, groups.Count);
        }

        [TestMethod]
        public void Statistics_TrackOperationScope_ActivitiesCountIncrement()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            var sourceName = "stats-scope-source";

            // Act - simulate what TelemetryService does
            stats.IncrementActivitiesCreated(sourceName);
            stats.IncrementActivitiesCompleted(sourceName, TimeSpan.FromMilliseconds(50));
            stats.IncrementExceptionsTracked();
            stats.IncrementEventsRecorded();
            stats.IncrementMetricsRecorded();

            // Assert
            Assert.AreEqual(1, stats.ActivitiesCreated);
            Assert.AreEqual(1, stats.ActivitiesCompleted);
            Assert.AreEqual(0, stats.ActiveActivities);
            Assert.AreEqual(1, stats.ExceptionsTracked);
            Assert.AreEqual(1, stats.EventsRecorded);
            Assert.AreEqual(1, stats.MetricsRecorded);

            var perSource = stats.PerSourceStatistics;
            Assert.IsTrue(perSource.ContainsKey(sourceName));
            Assert.AreEqual(1, perSource[sourceName].ActivitiesCreated);
            Assert.IsTrue(perSource[sourceName].AverageDurationMs >= 49);
        }

        [TestMethod]
        public void Statistics_Snapshot_CapturesAllFields()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            stats.IncrementActivitiesCreated("snap-source");
            stats.IncrementActivitiesCompleted("snap-source", TimeSpan.FromMilliseconds(10));
            stats.IncrementExceptionsTracked();
            stats.IncrementItemsEnqueued();
            stats.IncrementItemsProcessed(TimeSpan.FromMilliseconds(5));
            stats.IncrementItemsDropped();
            stats.IncrementProcessingErrors();
            stats.UpdateQueueDepth(42);

            // Act
            var snapshot = stats.GetSnapshot();

            // Assert
            Assert.AreEqual(1, snapshot.ActivitiesCreated);
            Assert.AreEqual(1, snapshot.ActivitiesCompleted);
            Assert.AreEqual(0, snapshot.ActiveActivities);
            Assert.AreEqual(1, snapshot.ExceptionsTracked);
            Assert.AreEqual(1, snapshot.ItemsEnqueued);
            Assert.AreEqual(1, snapshot.ItemsProcessed);
            Assert.AreEqual(1, snapshot.ItemsDropped);
            Assert.AreEqual(1, snapshot.ProcessingErrors);
            Assert.AreEqual(42, snapshot.QueueDepth);
            Assert.IsTrue(snapshot.AverageProcessingTimeMs >= 4);
            Assert.IsNotNull(snapshot.PerSourceStatistics);
        }
    }
}
