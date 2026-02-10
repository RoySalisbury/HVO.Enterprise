using System;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Extension methods for integrating Application Insights telemetry
    /// with the <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds Application Insights integration to the telemetry builder.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="AppInsightsOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithAppInsights(options =>
        ///     {
        ///         options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithAppInsights(
            this TelemetryBuilder builder,
            Action<AppInsightsOptions>? configure = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddAppInsightsTelemetry(configure);
            return builder;
        }

        /// <summary>
        /// Adds Application Insights integration with a connection string.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="connectionString">The Application Insights connection string.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty.</exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithAppInsights("InstrumentationKey=...;IngestionEndpoint=...");
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithAppInsights(
            this TelemetryBuilder builder,
            string connectionString)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            builder.Services.AddAppInsightsTelemetry(connectionString);
            return builder;
        }
    }
}
