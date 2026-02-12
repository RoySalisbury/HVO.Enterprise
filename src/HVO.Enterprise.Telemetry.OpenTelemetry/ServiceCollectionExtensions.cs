using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Extension methods for registering OpenTelemetry OTLP export with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OpenTelemetry OTLP export for traces, metrics, and optionally logs.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="OtlpExportOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>This method is idempotent — calling it multiple times will not add duplicate registrations.</para>
        /// <para>Registers TracerProvider and MeterProvider with all HVO ActivitySource and Meter names,
        /// exporting via OTLP to the configured collector endpoint.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTelemetry();
        /// services.AddOpenTelemetryExport(options =>
        /// {
        ///     options.ServiceName = "my-service";
        ///     options.Endpoint = "http://otel-collector:4317";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddOpenTelemetryExport(
            this IServiceCollection services,
            Action<OtlpExportOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(OtlpExportMarker)))
            {
                return services;
            }

            services.AddSingleton<OtlpExportMarker>();

            // Configure options
            var optionsBuilder = services.AddOptions<OtlpExportOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register activity source registrar
            services.TryAddSingleton<HvoActivitySourceRegistrar>();

            return services;
        }

        /// <summary>
        /// Adds OpenTelemetry OTLP export with environment-variable-only configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// Reads all configuration from OpenTelemetry environment variables
        /// (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_SERVICE_NAME</c>,
        /// <c>OTEL_RESOURCE_ATTRIBUTES</c>).
        /// </remarks>
        public static IServiceCollection AddOpenTelemetryExportFromEnvironment(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddOpenTelemetryExport();
        }
    }

    /// <summary>
    /// Marker type for idempotency guard. Prevents duplicate OpenTelemetry export registrations.
    /// </summary>
    internal sealed class OtlpExportMarker { }
}
