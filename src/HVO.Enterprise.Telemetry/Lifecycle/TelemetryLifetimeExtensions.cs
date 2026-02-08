using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Extension methods for registering telemetry lifetime management with DI.
    /// </summary>
    public static class TelemetryLifetimeExtensions
    {
        /// <summary>
        /// Adds telemetry lifetime management to the service collection.
        /// Registers a hosted service that integrates with IHostApplicationLifetime
        /// to ensure graceful shutdown of telemetry.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddTelemetryLifetime(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IHostedService, TelemetryLifetimeHostedService>();
            return services;
        }
    }
}
