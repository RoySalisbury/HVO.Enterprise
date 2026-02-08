using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Manages telemetry library lifecycle and graceful shutdown.
    /// Provides automatic AppDomain event hooks and optional IIS integration.
    /// </summary>
    internal sealed class TelemetryLifetimeManager : ITelemetryLifetime, IDisposable
    {
        private readonly TelemetryBackgroundWorker _worker;
        private readonly ILogger<TelemetryLifetimeManager> _logger;
        private int _isShuttingDown; // 0 = not shutting down, 1 = shutting down
        private volatile bool _disposed;

        // Default shutdown timeout
        private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Creates a new telemetry lifetime manager.
        /// </summary>
        /// <param name="worker">The background worker to manage.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when worker is null.</exception>
        public TelemetryLifetimeManager(
            TelemetryBackgroundWorker worker,
            ILogger<TelemetryLifetimeManager>? logger = null)
        {
            if (worker == null)
                throw new ArgumentNullException(nameof(worker));

            _worker = worker;
            _logger = logger ?? NullLogger<TelemetryLifetimeManager>.Instance;

            RegisterLifecycleHooks();
        }

        /// <summary>
        /// Gets whether shutdown is in progress.
        /// </summary>
        public bool IsShuttingDown => Interlocked.CompareExchange(ref _isShuttingDown, 0, 0) == 1;

        private void RegisterLifecycleHooks()
        {
            // Register AppDomain events
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Register IIS shutdown (if running in IIS)
            RegisterIISShutdownHook();

            _logger.LogDebug("Telemetry lifecycle hooks registered");
        }

        private void RegisterIISShutdownHook()
        {
            // IIS HostingEnvironment integration is disabled in this build because
            // HostingEnvironment.RegisterObject requires System.Web.Hosting.IRegisteredObject,
            // which is not available in our .NET Standard 2.0 target. The reflection-based
            // approach causes a type mismatch when invoking RegisterObject.
            // AppDomain events (DomainUnload, ProcessExit, UnhandledException) are still
            // used to trigger telemetry shutdown in all hosting environments, including IIS.
            _logger.LogDebug("IIS HostingEnvironment integration is not enabled; relying on AppDomain lifecycle events for telemetry shutdown.");
        }

        private void OnDomainUnload(object? sender, EventArgs e)
        {
            _logger.LogInformation("AppDomain unloading - initiating telemetry shutdown");
            ShutdownInternal(DefaultShutdownTimeout);
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            _logger.LogInformation("Process exiting - initiating telemetry shutdown");
            ShutdownInternal(DefaultShutdownTimeout);
        }

        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger.LogError(exception, "Unhandled exception - flushing telemetry before termination");

            // Attempt quick flush on unhandled exception
            ShutdownInternal(TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Initiates graceful shutdown with timeout.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for shutdown to complete.</param>
        /// <param name="cancellationToken">Cancellation token for early abort.</param>
        /// <returns>Result indicating success, items flushed, and items remaining.</returns>
        public async Task<ShutdownResult> ShutdownAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            // Use atomic operation to ensure only one shutdown occurs
            if (Interlocked.CompareExchange(ref _isShuttingDown, 1, 0) == 1)
            {
                return new ShutdownResult
                {
                    Success = false,
                    Reason = "Shutdown already in progress"
                };
            }

            try
            {
                _logger.LogInformation("Initiating telemetry shutdown (timeout: {Timeout})", timeout);

                var sw = Stopwatch.StartNew();

                // Close all open activities
                CloseOpenActivities();

                // Flush background queue
                var flushResult = await _worker.FlushAsync(timeout, cancellationToken).ConfigureAwait(false);

                sw.Stop();

                _logger.LogInformation(
                    "Telemetry shutdown complete. Flushed: {Flushed}, Remaining: {Remaining}, Duration: {Duration}ms",
                    flushResult.ItemsFlushed,
                    flushResult.ItemsRemaining,
                    sw.ElapsedMilliseconds);

                return new ShutdownResult
                {
                    Success = flushResult.Success,
                    ItemsFlushed = flushResult.ItemsFlushed,
                    ItemsRemaining = flushResult.ItemsRemaining,
                    Duration = sw.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during telemetry shutdown");

                return new ShutdownResult
                {
                    Success = false,
                    Reason = $"Shutdown failed: {ex.Message}"
                };
            }
        }

        private void ShutdownInternal(TimeSpan timeout)
        {
            // Check if shutdown already in progress
            if (Interlocked.CompareExchange(ref _isShuttingDown, 0, 0) == 1)
                return;

            try
            {
                // Use Task.Run to avoid sync-over-async deadlock when a SynchronizationContext
                // is present (e.g., ASP.NET classic, WPF). Task.Run schedules onto the thread pool,
                // ensuring ConfigureAwait(false) is not needed to avoid capturing the calling context.
                Task.Run(() => ShutdownAsync(timeout)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during synchronous shutdown");
            }
        }

        private void CloseOpenActivities()
        {
            // Stop current activity and all parents
            var activity = Activity.Current;
            while (activity != null)
            {
                var parent = activity.Parent;
                activity.Dispose();
                activity = parent;
            }
        }

        /// <summary>
        /// Disposes the lifetime manager and unregisters event handlers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Unregister event handlers
            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }
    }
}
