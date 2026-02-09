using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.IIS
{
    /// <summary>
    /// Coordinates telemetry shutdown during IIS app pool recycle or application shutdown.
    /// Provides thread-safe shutdown with configurable pre/post shutdown handlers.
    /// </summary>
    public sealed class IisShutdownHandler
    {
        private readonly ITelemetryService? _telemetryService;
        private readonly IisExtensionOptions _options;
        private readonly ILogger _logger;
        private int _shutdownStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisShutdownHandler"/> class.
        /// </summary>
        /// <param name="telemetryService">The telemetry service to shut down. Can be null if telemetry is not configured.</param>
        /// <param name="options">Extension options. Uses defaults if null.</param>
        /// <param name="logger">Logger for diagnostics. Uses null logger if not provided.</param>
        public IisShutdownHandler(
            ITelemetryService? telemetryService,
            IisExtensionOptions? options = null,
            ILogger? logger = null)
        {
            _telemetryService = telemetryService;
            _options = options ?? new IisExtensionOptions();
            _logger = logger ?? (ILogger)NullLogger<IisShutdownHandler>.Instance;
        }

        /// <summary>
        /// Gets whether shutdown has been initiated.
        /// </summary>
        public bool IsShutdownStarted => Interlocked.CompareExchange(ref _shutdownStarted, 0, 0) == 1;

        /// <summary>
        /// Called when IIS requests graceful shutdown (e.g., app pool recycle).
        /// Flushes pending telemetry within the configured timeout.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for timeout or abort.</param>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        public async Task OnGracefulShutdownAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _shutdownStarted, 1) == 1)
                return; // Already shutting down

            var sw = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("IIS graceful shutdown initiated");

                // Invoke pre-shutdown handlers
                await InvokeHandlerAsync(_options.OnPreShutdown, "pre-shutdown", cancellationToken)
                    .ConfigureAwait(false);

                // Shut down the telemetry service (this stops accepting new operations and flushes)
                if (_telemetryService != null)
                {
                    _telemetryService.Shutdown();
                    _logger.LogDebug("Telemetry service shutdown completed");
                }

                // Invoke post-shutdown handlers
                await InvokeHandlerAsync(_options.OnPostShutdown, "post-shutdown", cancellationToken)
                    .ConfigureAwait(false);

                sw.Stop();
                _logger.LogInformation(
                    "IIS graceful shutdown completed in {ElapsedMs}ms",
                    sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "IIS graceful shutdown timed out after {ElapsedMs}ms",
                    sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during IIS graceful shutdown after {ElapsedMs}ms",
                    sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Called when IIS forces immediate shutdown. Performs minimal best-effort cleanup.
        /// </summary>
        public void OnImmediateShutdown()
        {
            if (Interlocked.Exchange(ref _shutdownStarted, 1) == 1)
                return; // Already shutting down

            try
            {
                _logger.LogWarning("IIS immediate shutdown initiated - performing best-effort cleanup");

                // Best-effort shutdown - no async, no waiting
                _telemetryService?.Shutdown();

                _logger.LogInformation("IIS immediate shutdown cleanup completed");
            }
            catch (Exception ex)
            {
                // Use Trace as last resort - ILogger may not work during immediate shutdown
                Trace.WriteLine($"[HVO.Enterprise.Telemetry.IIS] Immediate shutdown error: {ex}");
            }
        }

        private async Task InvokeHandlerAsync(
            Func<CancellationToken, Task>? handler,
            string handlerName,
            CancellationToken cancellationToken)
        {
            if (handler == null)
                return;

            try
            {
                await handler(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IIS {HandlerName} handler failed", handlerName);
                // Swallow non-cancellation exceptions from handlers - don't let them
                // prevent the rest of the shutdown sequence
            }
        }
    }
}
