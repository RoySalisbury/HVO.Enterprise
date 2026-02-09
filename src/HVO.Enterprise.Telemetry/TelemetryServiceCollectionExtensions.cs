using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Lifecycle;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Extension methods for registering HVO telemetry services with dependency injection.
    /// </summary>
    public static class TelemetryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds HVO telemetry services to the service collection with an optional configuration delegate.
        /// Registers the core telemetry service, statistics, correlation, operation scopes,
        /// lifetime management, and a hosted service for automatic startup/shutdown.
        /// This method is idempotent — calling it multiple times will not add duplicate registrations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="TelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddTelemetry(
            this IServiceCollection services,
            Action<TelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(TelemetryService)))
                return services;

            // Configure options
            var optionsBuilder = services.AddOptions<TelemetryOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register options validator
            services.AddSingleton<IValidateOptions<TelemetryOptions>, TelemetryOptionsValidator>();

            // Register statistics (reuse existing extension for consistency)
            services.AddTelemetryStatistics();

            // Register correlation
            services.TryAddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

            // Register operation scope factory
            services.TryAddSingleton<IOperationScopeFactory>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<TelemetryOptions>>().Value;
                var loggerFactory = sp.GetService<ILoggerFactory>();

                var sourceName = options.ActivitySources != null && options.ActivitySources.Count > 0
                    ? options.ActivitySources[0]
                    : "HVO.Enterprise.Telemetry";

                return new OperationScopeFactory(
                    sourceName,
                    options.ServiceVersion,
                    loggerFactory);
            });

            // Register TelemetryService as both concrete and interface
            services.AddSingleton<TelemetryService>();
            services.AddSingleton<ITelemetryService>(sp => sp.GetRequiredService<TelemetryService>());

            // Register hosted service for lifecycle management
            services.AddSingleton<TelemetryHostedService>();
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp =>
                sp.GetRequiredService<TelemetryHostedService>());

            // Compose existing lifetime infrastructure
            services.AddTelemetryLifetime();

            return services;
        }

        /// <summary>
        /// Adds HVO telemetry services using configuration from an <see cref="IConfiguration"/> section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration section for telemetry options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
        public static IServiceCollection AddTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return services.AddTelemetry((Action<TelemetryOptions>)(options => configuration.Bind(options)));
        }

        /// <summary>
        /// Adds HVO telemetry services with a builder pattern for advanced configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Delegate to configure telemetry via <see cref="TelemetryBuilder"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
        public static IServiceCollection AddTelemetry(
            this IServiceCollection services,
            Action<TelemetryBuilder> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Register core services first
            services.AddTelemetry();

            // Apply builder configuration
            var builder = new TelemetryBuilder(services);
            configure(builder);

            return services;
        }
    }
}
