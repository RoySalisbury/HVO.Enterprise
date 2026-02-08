using System;
using HVO.Enterprise.Telemetry.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    [TestClass]
    public class TelemetryLifetimeExtensionsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTelemetryLifetime_WithNullServices_ThrowsException()
        {
            // Act
            TelemetryLifetimeExtensions.AddTelemetryLifetime(null!);
        }

        [TestMethod]
        public void AddTelemetryLifetime_RegistersHostedService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTelemetryLifetime();

            // Assert
            // Verify the hosted service is registered in the collection
            var hasHostedService = false;
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(IHostedService))
                {
                    hasHostedService = true;
                    break;
                }
            }

            Assert.IsTrue(hasHostedService, "IHostedService should be registered");
        }

        [TestMethod]
        public void AddTelemetryLifetime_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTelemetryLifetime();

            // Assert
            Assert.AreSame(services, result, "Should return the same service collection for chaining");
        }

        [TestMethod]
        public void AddTelemetryLifetime_CanBeCalledMultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - Call multiple times (should not throw)
            services.AddTelemetryLifetime();
            services.AddTelemetryLifetime();

            // Assert - Check that services are registered
            var hostedServiceCount = 0;
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(IHostedService))
                {
                    hostedServiceCount++;
                }
            }

            Assert.IsTrue(hostedServiceCount >= 2, "Multiple hosted services should be registered");
        }

        [TestMethod]
        public void AddTelemetryLifetime_SupportsMethodChaining()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTelemetryLifetime();

            // Assert
            Assert.AreSame(services, result, "Should return service collection for chaining");
        }
    }
}
