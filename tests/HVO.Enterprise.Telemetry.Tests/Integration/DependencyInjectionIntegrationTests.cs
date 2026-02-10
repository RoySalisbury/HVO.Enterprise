using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Exceptions;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Metrics;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify the DI container correctly wires up all telemetry
    /// services and they function together as a cohesive system.
    /// </summary>
    [TestClass]
    public class DependencyInjectionIntegrationTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            TestHelpers.ResetStaticTelemetry();
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void AddTelemetry_ResolvesAllCoreServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(options =>
            {
                options.ServiceName = "di-test";
            });

            using var provider = services.BuildServiceProvider();

            // Act & Assert — all core services should resolve
            var telemetryService = provider.GetService<ITelemetryService>();
            Assert.IsNotNull(telemetryService, "ITelemetryService should be registered");

            var scopeFactory = provider.GetService<IOperationScopeFactory>();
            Assert.IsNotNull(scopeFactory, "IOperationScopeFactory should be registered");

            var stats = provider.GetService<ITelemetryStatistics>();
            Assert.IsNotNull(stats, "ITelemetryStatistics should be registered");

            var options = provider.GetService<IOptions<TelemetryOptions>>();
            Assert.IsNotNull(options, "TelemetryOptions should be registered");
            Assert.AreEqual("di-test", options.Value.ServiceName);
        }

        [TestMethod]
        public void AddTelemetry_TelemetryServiceIsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(o => o.ServiceName = "singleton-test");
            using var provider = services.BuildServiceProvider();

            // Act
            var first = provider.GetRequiredService<ITelemetryService>();
            var second = provider.GetRequiredService<ITelemetryService>();

            // Assert
            Assert.AreSame(first, second, "ITelemetryService should be a singleton");
        }

        [TestMethod]
        public void AddTelemetry_OperationScopeFactory_CreatesWorkingScopes()
        {
            // Arrange
            using var listener = TestHelpers.CreateGlobalListener();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(o => o.ServiceName = "scope-factory-test");
            using var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IOperationScopeFactory>();

            // Act
            using var scope = factory.Begin("di-created-operation");

            // Assert
            Assert.IsNotNull(scope);
            Assert.AreEqual("di-created-operation", scope.Name);
            Assert.IsFalse(string.IsNullOrEmpty(scope.CorrelationId));
        }

        [TestMethod]
        public void AddTelemetry_TelemetryService_StartAndShutdown()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(o => o.ServiceName = "lifecycle-test");
            using var provider = services.BuildServiceProvider();

            var telemetryService = provider.GetRequiredService<ITelemetryService>();

            // Act
            telemetryService.Start();
            Assert.IsTrue(telemetryService.IsEnabled);

            telemetryService.TrackEvent("test-event");
            telemetryService.RecordMetric("test.metric", 1.0);
            telemetryService.TrackException(new Exception("test"));

            telemetryService.Shutdown();

            // Assert - verify it doesn't throw after shutdown
        }

        [TestMethod]
        public void AddTelemetry_WithConfiguration_BindsOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(options =>
            {
                options.ServiceName = "config-test";
                options.Enabled = true;
            });
            using var provider = services.BuildServiceProvider();

            // Act
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            // Assert
            Assert.AreEqual("config-test", options.Value.ServiceName);
            Assert.IsTrue(options.Value.Enabled);
        }

        [TestMethod]
        public void AddTelemetry_WithBuilder_ConfiguresViaBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder.Configure(o =>
                {
                    o.ServiceName = "builder-test";
                });
                builder.AddActivitySource("custom-source");
            });
            using var provider = services.BuildServiceProvider();

            // Act
            var telemetryService = provider.GetRequiredService<ITelemetryService>();

            // Assert
            Assert.IsNotNull(telemetryService);
        }

        [TestMethod]
        public void AddTelemetry_MultipleRegistrations_IsIdempotent()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(o => o.ServiceName = "first");
            services.AddTelemetry(o => o.ServiceName = "second"); // Should not replace

            using var provider = services.BuildServiceProvider();

            // Act
            var service = provider.GetRequiredService<ITelemetryService>();

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void AddTelemetry_FullScopeLifecycle_ThroughDI()
        {
            // Arrange
            using var listener = TestHelpers.CreateGlobalListener();
            using var provider = TestHelpers.CreateTelemetryServiceProvider(o =>
            {
                o.ServiceName = "full-lifecycle-test";
            });

            var service = provider.GetRequiredService<ITelemetryService>();
            service.Start();

            // Act — Create operation, nest child, record exception, succeed
            using (var parentScope = service.StartOperation("parent"))
            {
                Assert.IsNotNull(parentScope);

                using (var childScope = parentScope.CreateChild("child"))
                {
                    childScope.WithTag("key", "value");
                    childScope.Succeed();
                }

                parentScope.Succeed();
            }

            // Track telemetry events
            service.TrackEvent("lifecycle-event");
            service.RecordMetric("lifecycle.metric", 99.9);

            var stats = service.Statistics;
            Assert.IsNotNull(stats);

            service.Shutdown();
        }
    }
}
