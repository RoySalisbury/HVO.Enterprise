using System;
using System.Threading;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.IIS
{
    /// <summary>
    /// Manages HVO.Enterprise.Telemetry lifecycle for IIS-hosted applications.
    /// Provides automatic IIS detection, <c>IRegisteredObject</c> registration for
    /// graceful shutdown on .NET Framework, and AppDomain lifecycle hooks as a fallback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For DI-based applications, use the <c>AddIisTelemetryIntegration</c>
    /// extension method on <c>IServiceCollection</c>
    /// which handles initialization via a hosted service.
    /// </para>
    /// <para>
    /// For non-DI applications (.NET Framework Global.asax), create an instance directly
    /// and call <see cref="Initialize"/>.
    /// </para>
    /// </remarks>
    public sealed class IisLifecycleManager : IDisposable
    {
        private readonly ITelemetryService? _telemetryService;
        private readonly IisShutdownHandler _shutdownHandler;
        private readonly IisExtensionOptions _options;
        private readonly ILogger _logger;
        private object? _registeredProxy;
        private volatile bool _initialized;
        private int _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisLifecycleManager"/> class.
        /// </summary>
        /// <param name="telemetryService">The telemetry service to manage. Can be null if telemetry is not yet configured.</param>
        /// <param name="options">Extension options. Uses defaults if null.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the application is not running under IIS.
        /// Use <see cref="IisHostingEnvironment.IsIisHosted"/> to check before creating.
        /// </exception>
        public IisLifecycleManager(
            ITelemetryService? telemetryService,
            IisExtensionOptions? options = null,
            ILogger<IisLifecycleManager>? logger = null)
            : this(telemetryService, options, logger, requireIis: true)
        {
        }

        /// <summary>
        /// Initializes a new instance with an option to skip the IIS hosting requirement.
        /// Used internally for testing and for scenarios where IIS detection is handled externally.
        /// </summary>
        internal IisLifecycleManager(
            ITelemetryService? telemetryService,
            IisExtensionOptions? options,
            ILogger<IisLifecycleManager>? logger,
            bool requireIis)
        {
            if (requireIis && !IisHostingEnvironment.IsIisHosted)
            {
                throw new InvalidOperationException(
                    "IisLifecycleManager can only be used in IIS-hosted applications. " +
                    "Use IisHostingEnvironment.IsIisHosted to check before creating.");
            }

            _telemetryService = telemetryService;
            _options = options ?? new IisExtensionOptions();
            _options.Validate();
            _logger = logger ?? (ILogger)NullLogger<IisLifecycleManager>.Instance;
            _shutdownHandler = new IisShutdownHandler(telemetryService, _options, logger);
        }

        /// <summary>
        /// Gets the shutdown handler used by this lifecycle manager.
        /// </summary>
        internal IisShutdownHandler ShutdownHandler => _shutdownHandler;

        /// <summary>
        /// Gets whether the lifecycle manager has been initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Initializes IIS integration and registers for shutdown notifications.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when already initialized.</exception>
        public void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException("IIS lifecycle manager is already initialized.");

            _logger.LogInformation(
                "Initializing HVO.Enterprise.Telemetry IIS extension (PID: {ProcessId})",
                IisHostingEnvironment.WorkerProcessId ?? System.Diagnostics.Process.GetCurrentProcess().Id);

            // Hook AppDomain unload as fallback for all platforms
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

            // Attempt to register with IIS HostingEnvironment via reflection + DispatchProxy
            if (_options.RegisterWithHostingEnvironment)
            {
                RegisterWithHostingEnvironment();
            }

            _initialized = true;
            _logger.LogInformation("HVO.Enterprise.Telemetry IIS extension initialized");
        }

        private void RegisterWithHostingEnvironment()
        {
            if (!IisRegisteredObjectFactory.IsSystemWebAvailable)
            {
                _logger.LogDebug(
                    "System.Web is not available at runtime - " +
                    "using AppDomain.DomainUnload for shutdown notifications");
                return;
            }

            if (!IisRegisteredObjectFactory.TryCreate(
                _shutdownHandler,
                _options.ShutdownTimeout,
                out var proxy) || proxy == null)
            {
                _logger.LogWarning(
                    "Failed to create IRegisteredObject proxy - " +
                    "using AppDomain.DomainUnload for shutdown notifications");
                return;
            }

            if (!IisRegisteredObjectFactory.TryRegister(proxy))
            {
                _logger.LogWarning(
                    "Failed to register with HostingEnvironment - " +
                    "using AppDomain.DomainUnload for shutdown notifications");
                return;
            }

            _registeredProxy = proxy;
            _logger.LogInformation(
                "Registered with IIS HostingEnvironment for graceful shutdown (timeout: {TimeoutSeconds}s)",
                _options.ShutdownTimeout.TotalSeconds);
        }

        private void OnDomainUnload(object? sender, EventArgs e)
        {
            _logger.LogInformation("AppDomain unloading - flushing telemetry");

            try
            {
                var cts = new CancellationTokenSource(_options.ShutdownTimeout);
                _shutdownHandler.OnGracefulShutdownAsync(cts.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Telemetry flush timed out during AppDomain unload");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AppDomain unload telemetry flush");
            }
        }

        /// <summary>
        /// Disposes the lifecycle manager, unregistering from IIS and AppDomain events.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;

            if (_registeredProxy != null)
            {
                IisRegisteredObjectFactory.TryUnregister(_registeredProxy);
                _registeredProxy = null;
            }

            _logger.LogInformation("HVO.Enterprise.Telemetry IIS extension disposed");
        }
    }
}
