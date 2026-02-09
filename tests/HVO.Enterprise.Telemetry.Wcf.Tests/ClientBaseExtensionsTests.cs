using System;
using System.ServiceModel.Description;
using HVO.Enterprise.Telemetry.Wcf.Client;
using HVO.Enterprise.Telemetry.Wcf.Configuration;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class ClientBaseExtensionsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTelemetryBehavior_NullEndpoint_ThrowsArgumentNullException()
        {
            ClientBaseExtensions.AddTelemetryBehavior(null!);
        }

        [TestMethod]
        public void AddTelemetryBehavior_ReturnsEndpointForChaining()
        {
            // Arrange
            var endpoint = CreateTestEndpoint();

            // Act
            var result = endpoint.AddTelemetryBehavior();

            // Assert
            Assert.AreSame(endpoint, result);
        }

        [TestMethod]
        public void AddTelemetryBehavior_AddsBehaviorToEndpoint()
        {
            // Arrange
            var endpoint = CreateTestEndpoint();

            // Act
            endpoint.AddTelemetryBehavior();

            // Assert
            var hasTelemetryBehavior = false;
            foreach (var behavior in endpoint.EndpointBehaviors)
            {
                if (behavior is TelemetryClientEndpointBehavior)
                {
                    hasTelemetryBehavior = true;
                    break;
                }
            }
            Assert.IsTrue(hasTelemetryBehavior, "TelemetryClientEndpointBehavior should be added");
        }

        [TestMethod]
        public void AddTelemetryBehavior_WithOptions_PassesOptionsThrough()
        {
            // Arrange
            var endpoint = CreateTestEndpoint();
            var options = new WcfExtensionOptions
            {
                PropagateTraceContextInReply = false
            };

            // Act
            endpoint.AddTelemetryBehavior(options);

            // Assert
            var hasTelemetryBehavior = false;
            foreach (var behavior in endpoint.EndpointBehaviors)
            {
                if (behavior is TelemetryClientEndpointBehavior)
                {
                    hasTelemetryBehavior = true;
                    break;
                }
            }
            Assert.IsTrue(hasTelemetryBehavior);
        }

        private static ServiceEndpoint CreateTestEndpoint()
        {
            // Create a minimal ServiceEndpoint for testing
            var contract = new ContractDescription("ITestService");
            var endpoint = new ServiceEndpoint(contract);
            return endpoint;
        }
    }
}
