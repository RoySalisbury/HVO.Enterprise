using System;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        /// services.AddIisTelemetryIntegration(options =&gt;
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

            // Configure options via the established IOptions<T> pattern
            var optionsBuilder = services.AddOptions<IisExtensionOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register options validator
            services.AddSingleton<IValidateOptions<IisExtensionOptions>, IisExtensionOptionsValidator>();

            // Register shutdown handler
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<IisExtensionOptions>>().Value;
                var telemetryService = sp.GetService<ITelemetryService>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<IisShutdownHandler>();
                return new IisShutdownHandler(telemetryService, options, logger);
            });

            // Register lifecycle manager
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<IisExtensionOptions>>().Value;
                var telemetryService = sp.GetService<ITelemetryService>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<IisLifecycleManager>();
                return new IisLifecycleManager(telemetryService, options, logger);
            });

            // Register hosted service for auto-initialization (checks AutoInitialize at StartAsync)
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, IisLifecycleManagerHostedService>());

            return services;
        }
    }
}
