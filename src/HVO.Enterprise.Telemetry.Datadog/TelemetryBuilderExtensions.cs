using System;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Extension methods for integrating Datadog telemetry with the
    /// <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds Datadog telemetry integration to the telemetry builder.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="DatadogOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithDatadog(options =>
        ///     {
        ///         options.ServiceName = "my-service";
        ///         options.Environment = "production";
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithDatadog(
            this TelemetryBuilder builder,
            Action<DatadogOptions>? configure = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddDatadogTelemetry(configure);
            return builder;
        }
    }
}
