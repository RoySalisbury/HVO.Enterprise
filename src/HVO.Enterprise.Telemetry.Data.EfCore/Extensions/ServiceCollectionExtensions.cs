using System;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Extensions
{
    /// <summary>
    /// Extension methods for registering EF Core telemetry services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds EF Core telemetry services including options validation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for <see cref="EfCoreTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddEfCoreTelemetry(
            this IServiceCollection services,
            Action<EfCoreTelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingleton<IValidateOptions<EfCoreTelemetryOptions>, EfCoreTelemetryOptionsValidator>();

            return services;
        }
    }
}
