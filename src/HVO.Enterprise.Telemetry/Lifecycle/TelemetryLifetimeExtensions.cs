using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Extension methods for registering telemetry lifetime management with DI.
    /// </summary>
    public static class TelemetryLifetimeExtensions
    {
        /// <summary>
        /// Adds telemetry lifetime management to the service collection.
        /// Registers the background worker, lifetime manager, and hosted service
        /// that integrates with IHostApplicationLifetime to ensure graceful shutdown of telemetry.
        /// This method is idempotent - calling it multiple times will not add duplicate registrations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddTelemetryLifetime(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Check if already registered to make this method idempotent
            if (services.Any(s => s.ServiceType == typeof(TelemetryLifetimeManager)))
            {
                return services;
            }

            // Use factory lambdas to resolve constructor parameters via DI.
            // TelemetryBackgroundWorker has primitive constructor params (capacity, maxRestartAttempts, etc.)
            // that cannot be resolved by type-based registration.
            services.Add(new ServiceDescriptor(typeof(TelemetryBackgroundWorker), sp =>
                new TelemetryBackgroundWorker(logger: sp.GetService<ILogger<TelemetryBackgroundWorker>>()), ServiceLifetime.Singleton));
            services.Add(new ServiceDescriptor(typeof(TelemetryLifetimeManager), sp =>
                new TelemetryLifetimeManager(
                    sp.GetRequiredService<TelemetryBackgroundWorker>(),
                    sp.GetService<ILogger<TelemetryLifetimeManager>>()), ServiceLifetime.Singleton));
            services.Add(new ServiceDescriptor(typeof(ITelemetryLifetime), sp => sp.GetRequiredService<TelemetryLifetimeManager>(), ServiceLifetime.Singleton));
            services.Add(new ServiceDescriptor(typeof(IHostedService), typeof(TelemetryLifetimeHostedService), ServiceLifetime.Singleton));
            
            return services;
        }
    }
}
