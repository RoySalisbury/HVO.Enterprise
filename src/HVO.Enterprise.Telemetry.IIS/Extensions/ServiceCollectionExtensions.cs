using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.IIS.Extensions
{
    /// <summary>
    /// Extension methods for registering IIS telemetry integration with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds IIS lifecycle management for HVO.Enterprise.Telemetry.
        /// Only registers services if the application is running under IIS.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="IisExtensionOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// This method is safe to call in all environments. When the application is not
        /// hosted under IIS, no services are registered and the method returns immediately.
        /// </para>
        /// <para>
        /// When <see cref="IisExtensionOptions.AutoInitialize"/> is <c>true</c> (default),
        /// an <see cref="IHostedService"/> is registered to automatically initialize the
        /// lifecycle manager when the host starts.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Startup.cs or Program.cs
        /// services.AddTelemetry();
        /// services.AddIisTelemetryIntegration(options =>
        /// {
        ///     options.ShutdownTimeout = TimeSpan.FromSeconds(20);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddIisTelemetryIntegration(
            this IServiceCollection services,
            Action<IisExtensionOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Only register if running under IIS
            if (!IisHostingEnvironment.IsIisHosted)
                return services;

            // Configure options
            var options = new IisExtensionOptions();
            configure?.Invoke(options);
            options.Validate();

            services.TryAddSingleton(options);

            // Register shutdown handler
            services.TryAddSingleton(sp =>
            {
                var telemetryService = sp.GetService<ITelemetryService>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<IisShutdownHandler>();
                return new IisShutdownHandler(telemetryService, options, logger);
            });

            // Register lifecycle manager
            services.TryAddSingleton(sp =>
            {
                var telemetryService = sp.GetService<ITelemetryService>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<IisLifecycleManager>();
                return new IisLifecycleManager(telemetryService, options, logger);
            });

            // Auto-initialize via hosted service if requested
            if (options.AutoInitialize)
            {
                services.AddSingleton<IHostedService, IisLifecycleManagerHostedService>();
            }

            return services;
        }
    }

    /// <summary>
    /// Hosted service that initializes the <see cref="IisLifecycleManager"/> on host startup
    /// and disposes it on host stop.
    /// </summary>
    internal sealed class IisLifecycleManagerHostedService : IHostedService
    {
        private readonly IisLifecycleManager _lifecycleManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisLifecycleManagerHostedService"/> class.
        /// </summary>
        /// <param name="lifecycleManager">The IIS lifecycle manager.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="lifecycleManager"/> is null.</exception>
        public IisLifecycleManagerHostedService(IisLifecycleManager lifecycleManager)
        {
            _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_lifecycleManager.IsInitialized)
            {
                _lifecycleManager.Initialize();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _lifecycleManager.Dispose();
            return Task.CompletedTask;
        }
    }
}
