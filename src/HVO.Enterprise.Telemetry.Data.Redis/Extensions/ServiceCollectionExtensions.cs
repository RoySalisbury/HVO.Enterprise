using System;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Redis.Extensions
{
    /// <summary>
    /// Extension methods for registering Redis telemetry services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Redis telemetry services including options validation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for <see cref="RedisTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddRedisTelemetry(
            this IServiceCollection services,
            Action<RedisTelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingleton<IValidateOptions<RedisTelemetryOptions>, RedisTelemetryOptionsValidator>();

            return services;
        }
    }
}
