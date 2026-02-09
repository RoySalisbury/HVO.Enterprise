using System;
using HVO.Enterprise.Telemetry.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// Extension methods for registering telemetry statistics and health checks with the DI container.
    /// </summary>
    public static class TelemetryHealthCheckExtensions
    {
        /// <summary>
        /// Registers <see cref="ITelemetryStatistics"/> as a singleton in the service collection.
        /// The internal <see cref="TelemetryStatistics"/> implementation is also registered for
        /// direct injection by the telemetry system itself.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddTelemetryStatistics(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var stats = new TelemetryStatistics();
            services.AddSingleton<ITelemetryStatistics>(stats);
            services.AddSingleton(stats);
            return services;
        }

        /// <summary>
        /// Registers the <see cref="TelemetryHealthCheck"/> as a singleton in the service collection.
        /// Requires <see cref="ITelemetryStatistics"/> to be registered first via
        /// <see cref="AddTelemetryStatistics"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional health check threshold configuration. Uses defaults if null.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// This registers the <see cref="TelemetryHealthCheck"/> as a service. For ASP.NET Core
        /// health check middleware integration, consumers should use the standard
        /// <c>IHealthChecksBuilder.AddCheck&lt;TelemetryHealthCheck&gt;()</c> pattern:
        /// </para>
        /// <code>
        /// services.AddTelemetryStatistics();
        /// services.AddTelemetryHealthCheck();
        /// services.AddHealthChecks().AddCheck&lt;TelemetryHealthCheck&gt;("telemetry");
        /// </code>
        /// </remarks>
        public static IServiceCollection AddTelemetryHealthCheck(
            this IServiceCollection services,
            TelemetryHealthCheckOptions? options = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton(sp => new TelemetryHealthCheck(
                sp.GetRequiredService<ITelemetryStatistics>(),
                options));

            return services;
        }
    }
}
