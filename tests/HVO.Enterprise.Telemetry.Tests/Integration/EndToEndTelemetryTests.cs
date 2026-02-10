using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests that exercise the full telemetry pipeline:
    /// initialization, correlation, operations, metrics, exceptions, and shutdown.
    /// </summary>
    [TestClass]
    public class EndToEndTelemetryTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            TestHelpers.ResetStaticTelemetry();
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void StaticTelemetry_InitializeOperateShutdown_FullLifecycle()
        {
            // Arrange
            var options = new TelemetryOptions
            {
                ServiceName = "e2e-test-service"
            };

            // Act - Initialize
            var initialized = Telemetry.Initialize(options);
            Assert.IsTrue(initialized, "Telemetry should initialize successfully");
            Assert.IsTrue(Telemetry.IsInitialized, "Telemetry.IsInitialized should be true");

            // Act - Create an operation scope
            using (var scope = Telemetry.StartOperation("end-to-end-operation"))
            {
                Assert.IsNotNull(scope);
                Assert.AreEqual("end-to-end-operation", scope.Name);
                Assert.IsFalse(string.IsNullOrEmpty(scope.CorrelationId));

                scope.WithTag("testKey", "testValue");
                scope.Succeed();
            }

            // Act - Track exception
            Telemetry.TrackException(new InvalidOperationException("test exception"));

            // Act - Track event
            Telemetry.TrackEvent("test-event");

            // Act - Record metric
            Telemetry.RecordMetric("test.metric", 42.0);

            // Act - Verify statistics
            var stats = Telemetry.Statistics;
            Assert.IsNotNull(stats);

            // Act - Shutdown
            Telemetry.Shutdown();
            Assert.IsFalse(Telemetry.IsInitialized, "Should not be initialized after shutdown");
        }

        [TestMethod]
        public void StaticTelemetry_CorrelationFlows_ThroughOperationScope()
        {
            // Arrange
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "corr-flow-test" });

            // Act
            using (Telemetry.SetCorrelationId("manual-correlation-123"))
            {
                Assert.AreEqual("manual-correlation-123", Telemetry.CurrentCorrelationId);

                using (var scope = Telemetry.StartOperation("correlated-operation"))
                {
                    // The scope should pick up the current correlation ID
                    Assert.AreEqual("manual-correlation-123", scope.CorrelationId);
                }
            }
        }

        [TestMethod]
        public void StaticTelemetry_BeginCorrelation_AutoGeneratesId()
        {
            // Arrange
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "auto-corr-test" });

            // Act
            using (Telemetry.BeginCorrelation())
            {
                var corrId = Telemetry.CurrentCorrelationId;
                Assert.IsFalse(string.IsNullOrEmpty(corrId),
                    "BeginCorrelation should auto-generate a correlation ID");
                Assert.IsTrue(Guid.TryParse(corrId, out _),
                    "Auto-generated ID should be a valid GUID");
            }
        }

        [TestMethod]
        public void StaticTelemetry_DoubleInitialize_SecondCallReturnsFalse()
        {
            // Arrange & Act
            var first = Telemetry.Initialize(new TelemetryOptions { ServiceName = "double-init" });
            var second = Telemetry.Initialize(new TelemetryOptions { ServiceName = "double-init-2" });

            // Assert
            Assert.IsTrue(first);
            Assert.IsFalse(second, "Second Initialize should return false");
        }

        [TestMethod]
        public void StaticTelemetry_ExceptionAggregator_TracksExceptions()
        {
            // Arrange
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "agg-test" });

            // Act
            for (int i = 0; i < 5; i++)
            {
                Telemetry.TrackException(new InvalidOperationException("repeated error"));
            }

            var aggregator = Telemetry.GetExceptionAggregator();

            // Assert
            Assert.IsNotNull(aggregator);
            Assert.IsTrue(aggregator.TotalExceptions >= 5);
        }

        [TestMethod]
        public async Task StaticTelemetry_OperationsWithNestedScopes_FlowCorrectly()
        {
            // Arrange
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "nested-test" });

            // Act
            using (var parent = Telemetry.StartOperation("parent-operation"))
            {
                parent.WithTag("level", "parent");

                using (var child = parent.CreateChild("child-operation"))
                {
                    child.WithTag("level", "child");

                    await Task.Yield();

                    // Correlation should still be consistent
                    Assert.AreEqual(parent.CorrelationId, child.CorrelationId);
                    child.Succeed();
                }

                parent.Succeed();
            }
        }
    }
}
