using System;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Extensions
{
    /// <summary>
    /// Extension methods for registering ADO.NET telemetry services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ADO.NET telemetry services including options validation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for <see cref="AdoNetTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddAdoNetTelemetry(
            this IServiceCollection services,
            Action<AdoNetTelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingleton<IValidateOptions<AdoNetTelemetryOptions>, AdoNetTelemetryOptionsValidator>();

            return services;
        }
    }
}
