using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Extension methods for registering Datadog telemetry integration with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Datadog telemetry integration with dual-mode OTLP / DogStatsD support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="DatadogOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>Registers the following services (singleton lifetime):</para>
        /// <list type="bullet">
        /// <item><description><see cref="DatadogMetricsExporter"/> — DogStatsD metric client</description></item>
        /// <item><description><see cref="DatadogTraceExporter"/> — Activity enrichment and propagation</description></item>
        /// </list>
        /// <para>
        /// This method is idempotent — calling it multiple times will not add duplicate registrations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTelemetry();
        /// services.AddDatadogTelemetry(options =>
        /// {
        ///     options.ServiceName = "my-service";
        ///     options.Environment = "production";
        ///     options.Mode = DatadogExportMode.DogStatsD;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddDatadogTelemetry(
            this IServiceCollection services,
            Action<DatadogOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(DatadogMetricsExporter)))
            {
                return services;
            }

            // Configure options
            var optionsBuilder = services.AddOptions<DatadogOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register DatadogTraceExporter (singleton — always registered for activity enrichment)
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DatadogOptions>>().Value;
                options.ApplyEnvironmentDefaults();

                if (!options.EnableTraceExporter)
                {
                    // Return a trace exporter with defaults only — lightweight, no side effects
                    return new DatadogTraceExporter(new DatadogOptions());
                }

                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<DatadogTraceExporter>();
                return new DatadogTraceExporter(options, logger);
            });

            // Register DatadogMetricsExporter (singleton — configures DogStatsD connection)
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DatadogOptions>>().Value;
                options.ApplyEnvironmentDefaults();

                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<DatadogMetricsExporter>();
                return new DatadogMetricsExporter(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds Datadog telemetry integration with environment-variable-only configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// Reads all configuration from Datadog environment variables
        /// (<c>DD_SERVICE</c>, <c>DD_ENV</c>, <c>DD_VERSION</c>,
        /// <c>DD_AGENT_HOST</c>, <c>DD_DOGSTATSD_PORT</c>, <c>DD_DOGSTATSD_SOCKET</c>).
        /// </remarks>
        /// <example>
        /// <code>
        /// // Uses DD_* environment variables for all configuration
        /// services.AddDatadogTelemetry();
        /// </code>
        /// </example>
        public static IServiceCollection AddDatadogTelemetryFromEnvironment(
            this IServiceCollection services)
        {
            return services.AddDatadogTelemetry(configure: null);
        }
    }
}
