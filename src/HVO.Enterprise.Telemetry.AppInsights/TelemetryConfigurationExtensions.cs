using System;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Extension methods for configuring Application Insights <see cref="TelemetryConfiguration"/>
    /// with HVO telemetry initializers.
    /// </summary>
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>
        /// Adds HVO telemetry initializers to the Application Insights configuration.
        /// </summary>
        /// <param name="configuration">The Application Insights telemetry configuration.</param>
        /// <param name="options">Optional options to control which initializers are added.</param>
        /// <returns>The configuration for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// Adds the following initializers based on <see cref="AppInsightsOptions"/>:
        /// </para>
        /// <list type="bullet">
        /// <item><description><see cref="ActivityTelemetryInitializer"/> — W3C TraceContext and Activity tags propagation</description></item>
        /// <item><description><see cref="CorrelationTelemetryInitializer"/> — HVO correlation ID enrichment</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // .NET Framework 4.8 usage
        /// TelemetryConfiguration.Active.AddHvoEnrichers();
        ///
        /// // Custom configuration
        /// var configuration = TelemetryConfiguration.CreateDefault();
        /// configuration.AddHvoEnrichers(new AppInsightsOptions
        /// {
        ///     EnableActivityInitializer = true,
        ///     EnableCorrelationInitializer = true,
        ///     CorrelationFallbackToActivity = false
        /// });
        /// </code>
        /// </example>
        public static TelemetryConfiguration AddHvoEnrichers(
            this TelemetryConfiguration configuration,
            AppInsightsOptions? options = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            options = options ?? new AppInsightsOptions();

            if (options.EnableActivityInitializer)
            {
                configuration.TelemetryInitializers.Add(new ActivityTelemetryInitializer());
            }

            if (options.EnableCorrelationInitializer)
            {
                configuration.TelemetryInitializers.Add(
                    new CorrelationTelemetryInitializer(
                        options.CorrelationPropertyName,
                        options.CorrelationFallbackToActivity));
            }

            return configuration;
        }
    }
}
