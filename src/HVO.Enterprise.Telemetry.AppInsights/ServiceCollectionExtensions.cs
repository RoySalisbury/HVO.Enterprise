using System;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Extension methods for registering Application Insights telemetry integration
    /// with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Application Insights integration for HVO telemetry with optional configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="AppInsightsOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// Registers the following services:
        /// </para>
        /// <list type="bullet">
        /// <item><description><see cref="TelemetryConfiguration"/> (singleton) — with HVO initializers</description></item>
        /// <item><description><see cref="TelemetryClient"/> (singleton) — configured with HVO enrichers</description></item>
        /// <item><description><see cref="ApplicationInsightsBridge"/> (singleton) — dual-mode bridge</description></item>
        /// </list>
        /// <para>
        /// This method is idempotent — calling it multiple times will not add duplicate registrations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTelemetry();
        /// services.AddAppInsightsTelemetry(options =>
        /// {
        ///     options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddAppInsightsTelemetry(
            this IServiceCollection services,
            Action<AppInsightsOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(ApplicationInsightsBridge)))
            {
                return services;
            }

            // Configure options
            var optionsBuilder = services.AddOptions<AppInsightsOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register TelemetryConfiguration (singleton)
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AppInsightsOptions>>().Value;
                var connectionString = options.GetEffectiveConnectionString();

                var configuration = TelemetryConfiguration.CreateDefault();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    configuration.ConnectionString = connectionString;
                }

                // Add HVO enrichers
                configuration.AddHvoEnrichers(options);

                return configuration;
            });

            // Register TelemetryClient (singleton)
            services.TryAddSingleton(sp =>
            {
                var configuration = sp.GetRequiredService<TelemetryConfiguration>();
                return new TelemetryClient(configuration);
            });

            // Register ApplicationInsightsBridge (singleton)
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AppInsightsOptions>>().Value;

                if (!options.EnableBridge)
                {
                    // Bridge is disabled — register a no-op instance (OTLP mode = all methods are no-ops)
                    var noOpConfig = TelemetryConfiguration.CreateDefault();
                    var noOpClient = new TelemetryClient(noOpConfig);
                    return new ApplicationInsightsBridge(noOpClient, forceOtlpMode: true);
                }

                var client = sp.GetRequiredService<TelemetryClient>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<ApplicationInsightsBridge>();
                return new ApplicationInsightsBridge(
                    client, logger, options.ForceOtlpMode, options.CorrelationPropertyName);
            });

            return services;
        }

        /// <summary>
        /// Adds Application Insights integration with a connection string.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The Application Insights connection string.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty.</exception>
        /// <example>
        /// <code>
        /// services.AddAppInsightsTelemetry("InstrumentationKey=...;IngestionEndpoint=...");
        /// </code>
        /// </example>
        public static IServiceCollection AddAppInsightsTelemetry(
            this IServiceCollection services,
            string connectionString)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (connectionString.Length == 0)
            {
                throw new ArgumentException(
                    "Connection string cannot be empty.",
                    nameof(connectionString));
            }

            return services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
            });
        }
    }
}
