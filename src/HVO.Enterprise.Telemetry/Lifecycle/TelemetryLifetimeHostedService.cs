using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// IHostedService for ASP.NET Core lifecycle integration.
    /// Integrates telemetry shutdown with application lifetime events.
    /// </summary>
    internal sealed class TelemetryLifetimeHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly TelemetryLifetimeManager _lifetimeManager;
        private readonly ILogger<TelemetryLifetimeHostedService> _logger;

        /// <summary>
        /// Creates a new telemetry lifetime hosted service.
        /// </summary>
        /// <param name="appLifetime">Application lifetime service.</param>
        /// <param name="lifetimeManager">Telemetry lifetime manager.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when appLifetime or lifetimeManager is null.
        /// </exception>
        public TelemetryLifetimeHostedService(
            IHostApplicationLifetime appLifetime,
            TelemetryLifetimeManager lifetimeManager,
            ILogger<TelemetryLifetimeHostedService>? logger = null)
        {
            if (appLifetime == null)
                throw new ArgumentNullException(nameof(appLifetime));
            if (lifetimeManager == null)
                throw new ArgumentNullException(nameof(lifetimeManager));

            _appLifetime = appLifetime;
            _lifetimeManager = lifetimeManager;
            _logger = logger ?? NullLogger<TelemetryLifetimeHostedService>.Instance;
        }

        /// <summary>
        /// Starts the hosted service and registers for application lifetime events.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed task.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            _logger.LogInformation("Telemetry lifetime service started");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the hosted service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry lifetime service stopping");
            return Task.CompletedTask;
        }

        private void OnStopping()
        {
            _logger.LogInformation("Application stopping - flushing telemetry");
            try
            {
                _lifetimeManager.ShutdownAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during telemetry shutdown on application stopping");
            }
        }

        private void OnStopped()
        {
            _logger.LogInformation("Application stopped");
        }
    }
}
