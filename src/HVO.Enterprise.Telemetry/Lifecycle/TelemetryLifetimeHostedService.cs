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
        /// Stops the hosted service and performs a graceful telemetry shutdown.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancellation token that is triggered when the host is shutting down.
        /// The telemetry shutdown will stop being awaited if this token is canceled.
        /// </param>
        /// <returns>Task representing the asynchronous stop operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry lifetime service stopping - flushing telemetry");

            var shutdownTimeout = TimeSpan.FromSeconds(5);

            try
            {
                var shutdownTask = _lifetimeManager.ShutdownAsync(shutdownTimeout);

                // If already completed, await it directly
                if (shutdownTask.IsCompleted)
                {
                    await shutdownTask.ConfigureAwait(false);
                    return;
                }

                // Wait for either shutdown completion or cancellation
                var cancellationTaskSource = new TaskCompletionSource<object?>();
                using (cancellationToken.Register(
                    state => ((TaskCompletionSource<object?>)state!).TrySetCanceled(),
                    cancellationTaskSource))
                {
                    var completed = await Task
                        .WhenAny(shutdownTask, cancellationTaskSource.Task)
                        .ConfigureAwait(false);

                    if (completed == shutdownTask)
                    {
                        await shutdownTask.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Telemetry shutdown canceled due to host shutdown token cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during telemetry shutdown in StopAsync");
            }
        }

        private void OnStopping()
        {
            _logger.LogInformation("Application stopping event received");
        }

        private void OnStopped()
        {
            _logger.LogInformation("Application stopped");
        }
    }
}
