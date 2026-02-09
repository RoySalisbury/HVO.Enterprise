using System;
using HVO.Enterprise.Telemetry.IIS.Configuration;

namespace HVO.Enterprise.Telemetry.IIS.Extensions
{
    /// <summary>
    /// Extension methods for integrating IIS telemetry with the <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds IIS lifecycle management to the telemetry builder.
        /// Only registers services if the application is running under IIS.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="IisExtensionOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithIisIntegration(options =>
        ///     {
        ///         options.ShutdownTimeout = TimeSpan.FromSeconds(20);
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithIisIntegration(
            this TelemetryBuilder builder,
            Action<IisExtensionOptions>? configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddIisTelemetryIntegration(configure);
            return builder;
        }
    }
}
