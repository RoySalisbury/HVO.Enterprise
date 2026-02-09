using System;
using System.Diagnostics;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using HVO.Enterprise.Telemetry.Wcf.Client;
using HVO.Enterprise.Telemetry.Wcf.Configuration;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class TelemetryClientEndpointBehaviorTests
    {
        [TestMethod]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var behavior = new TelemetryClientEndpointBehavior();

            // Assert
            Assert.IsNotNull(behavior);
        }

        [TestMethod]
        public void Constructor_WithOptions_CreatesInstance()
        {
            // Arrange
            var options = new WcfExtensionOptions
            {
                PropagateTraceContextInReply = false,
                RecordMessageBodies = true
            };

            // Act
            var behavior = new TelemetryClientEndpointBehavior(options);

            // Assert
            Assert.IsNotNull(behavior);
        }

        [TestMethod]
        public void Constructor_WithActivitySource_CreatesInstance()
        {
            // Arrange
            using var source = new ActivitySource("test.behavior");

            // Act
            var behavior = new TelemetryClientEndpointBehavior(source);

            // Assert
            Assert.IsNotNull(behavior);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullActivitySource_ThrowsArgumentNullException()
        {
            new TelemetryClientEndpointBehavior(null!, null);
        }

        [TestMethod]
        public void AddBindingParameters_DoesNotThrow()
        {
            // Arrange
            var behavior = new TelemetryClientEndpointBehavior();

            // Act & Assert - should not throw
            behavior.AddBindingParameters(null!, null!);
        }

        [TestMethod]
        public void Validate_DoesNotThrow()
        {
            // Arrange
            var behavior = new TelemetryClientEndpointBehavior();

            // Act & Assert - should not throw
            behavior.Validate(null!);
        }

        [TestMethod]
        public void ApplyDispatchBehavior_DoesNotThrow()
        {
            // Arrange
            var behavior = new TelemetryClientEndpointBehavior();

            // Act & Assert - should not throw (no-op for client behavior)
            behavior.ApplyDispatchBehavior(null!, null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ApplyClientBehavior_NullClientRuntime_ThrowsArgumentNullException()
        {
            // Arrange
            var behavior = new TelemetryClientEndpointBehavior();

            // Act
            behavior.ApplyClientBehavior(null!, null!);
        }

        [TestMethod]
        public void ImplementsIEndpointBehavior()
        {
            // Arrange
            var behavior = new TelemetryClientEndpointBehavior();

            // Assert
            Assert.IsInstanceOfType(behavior, typeof(IEndpointBehavior));
        }
    }
}
