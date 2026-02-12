using System;

namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Extension methods for integrating OpenTelemetry export with the
    /// <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds OpenTelemetry OTLP export to the telemetry builder.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="OtlpExportOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithOpenTelemetry(options =>
        ///     {
        ///         options.Endpoint = "http://otel-collector:4317";
        ///         options.ServiceName = "my-service";
        ///         options.EnableMetricsExport = true;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithOpenTelemetry(
            this TelemetryBuilder builder,
            Action<OtlpExportOptions>? configure = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddOpenTelemetryExport(configure);
            return builder;
        }

        /// <summary>
        /// Adds a Prometheus scrape endpoint for exposing HVO metrics.
        /// Requires ASP.NET Core (.NET 6+). No-op on .NET Framework.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="path">The endpoint path. Default: <c>"/metrics"</c>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static TelemetryBuilder WithPrometheusEndpoint(
            this TelemetryBuilder builder,
            string path = "/metrics")
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddOpenTelemetryExport(options =>
            {
                options.EnablePrometheusEndpoint = true;
                options.PrometheusEndpointPath = path;
            });
            return builder;
        }

        /// <summary>
        /// Enables OTLP log export alongside trace and metrics export.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static TelemetryBuilder WithOtlpLogExport(
            this TelemetryBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddOpenTelemetryExport(options =>
            {
                options.EnableLogExport = true;
            });
            return builder;
        }
    }
}
