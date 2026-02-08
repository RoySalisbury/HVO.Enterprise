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
        private volatile bool _isShuttingDown;
        private volatile bool _disposed;
        private object? _registeredObject;

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
        public bool IsShuttingDown => _isShuttingDown;

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
            try
            {
                // Check if HostingEnvironment is available (ASP.NET)
                var hostingEnvironmentType = Type.GetType(
                    "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

                if (hostingEnvironmentType != null)
                {
                    var registerMethod = hostingEnvironmentType.GetMethod(
                        "RegisterObject",
                        BindingFlags.Public | BindingFlags.Static);

                    if (registerMethod != null)
                    {
                        // Create the registered object
                        _registeredObject = new TelemetryRegisteredObject(this);

                        // Register with HostingEnvironment
                        registerMethod.Invoke(null, new object[] { _registeredObject });

                        _logger.LogDebug("Registered with IIS HostingEnvironment");
                    }
                }
            }
            catch (Exception ex)
            {
                // Not fatal - we'll still get AppDomain events
                _logger.LogWarning(ex, "Failed to register with IIS HostingEnvironment");
            }
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
            _logger.LogError("Unhandled exception - flushing telemetry before termination");

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
            if (_isShuttingDown)
            {
                return new ShutdownResult
                {
                    Success = false,
                    Reason = "Shutdown already in progress"
                };
            }

            _isShuttingDown = true;

            try
            {
                _logger.LogInformation("Initiating telemetry shutdown (timeout: {Timeout})", timeout);

                var sw = Stopwatch.StartNew();

                // Close all open activities
                CloseOpenActivities();

                // Flush background queue
                var flushResult = await _worker.FlushAsync(timeout, cancellationToken);

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
            if (_isShuttingDown)
                return;

            try
            {
                // Synchronous shutdown for event handlers
                ShutdownAsync(timeout).GetAwaiter().GetResult();
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

            // Unregister from IIS if we registered
            UnregisterFromIIS();
        }

        private void UnregisterFromIIS()
        {
            if (_registeredObject == null)
                return;

            try
            {
                var hostingEnvironmentType = Type.GetType(
                    "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

                var unregisterMethod = hostingEnvironmentType?.GetMethod(
                    "UnregisterObject",
                    BindingFlags.Public | BindingFlags.Static);

                unregisterMethod?.Invoke(null, new object[] { _registeredObject });

                _logger.LogDebug("Unregistered from IIS HostingEnvironment");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unregister from IIS HostingEnvironment");
            }
        }
    }
}
