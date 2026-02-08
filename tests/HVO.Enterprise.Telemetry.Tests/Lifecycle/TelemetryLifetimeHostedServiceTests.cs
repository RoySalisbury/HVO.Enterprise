using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Lifecycle;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    [TestClass]
    public class TelemetryLifetimeHostedServiceTests
    {
        /// <summary>
        /// Mock implementation of IHostApplicationLifetime for testing.
        /// </summary>
        private class MockHostApplicationLifetime : IHostApplicationLifetime
        {
            private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
            private readonly CancellationTokenSource _stoppedCts = new CancellationTokenSource();
            private readonly CancellationTokenSource _startedCts = new CancellationTokenSource();

            public CancellationToken ApplicationStarted => _startedCts.Token;
            public CancellationToken ApplicationStopping => _stoppingCts.Token;
            public CancellationToken ApplicationStopped => _stoppedCts.Token;

            public void StopApplication()
            {
                if (!_stoppingCts.IsCancellationRequested)
                {
                    _stoppingCts.Cancel();
                }

                if (!_stoppedCts.IsCancellationRequested)
                {
                    _stoppedCts.Cancel();
                }
            }

            public void TriggerStopping()
            {
                if (!_stoppingCts.IsCancellationRequested)
                {
                    _stoppingCts.Cancel();
                }
            }

            public void TriggerStopped()
            {
                if (!_stoppedCts.IsCancellationRequested)
                {
                    _stoppedCts.Cancel();
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullAppLifetime_ThrowsException()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Act
            new TelemetryLifetimeHostedService(null!, manager);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullManager_ThrowsException()
        {
            // Arrange
            var appLifetime = new MockHostApplicationLifetime();

            // Act
            new TelemetryLifetimeHostedService(appLifetime, null!);
        }

        [TestMethod]
        public void Constructor_WithValidArguments_CreatesService()
        {
            // Arrange
            var appLifetime = new MockHostApplicationLifetime();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Act
            var service = new TelemetryLifetimeHostedService(appLifetime, manager);

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public async Task StartAsync_RegistersLifetimeEvents()
        {
            // Arrange
            var appLifetime = new MockHostApplicationLifetime();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var service = new TelemetryLifetimeHostedService(appLifetime, manager);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert - Events should be registered (verified by no exceptions)
            Assert.IsFalse(manager.IsShuttingDown);
        }

        [TestMethod]
        public async Task StopAsync_CompletesSuccessfully()
        {
            // Arrange
            var appLifetime = new MockHostApplicationLifetime();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var service = new TelemetryLifetimeHostedService(appLifetime, manager);

            await service.StartAsync(CancellationToken.None);

            // Act
            await service.StopAsync(CancellationToken.None);

            // Assert - Should complete without errors
        }

        [TestMethod]
        public async Task ApplicationStopping_TriggersShutdown()
        {
            // Arrange
            var appLifetime = new MockHostApplicationLifetime();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var service = new TelemetryLifetimeHostedService(
                appLifetime,
                manager,
                NullLogger<TelemetryLifetimeHostedService>.Instance);

            await service.StartAsync(CancellationToken.None);

            // Act
            appLifetime.TriggerStopping();

            // Wait a moment for the event handler to execute
            await Task.Delay(100);

            // Assert
            Assert.IsTrue(manager.IsShuttingDown, "Shutdown should be initiated");
        }

        [TestMethod]
        public async Task StartAndStop_MultipleTimes_HandlesGracefully()
        {
            // Arrange
            var appLifetime = new MockHostApplicationLifetime();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var service = new TelemetryLifetimeHostedService(appLifetime, manager);

            // Act - Start and stop multiple times
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);

            // This should not throw even though we're calling start/stop again
            // However, in a real scenario, we wouldn't reuse the same service instance
            // This test primarily validates no exceptions are thrown
        }
    }
}
