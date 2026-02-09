using System;
using HVO.Enterprise.Telemetry.Data.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Extensions
{
    /// <summary>
    /// Extension methods for registering shared data telemetry services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds base data telemetry services including options validation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for <see cref="DataExtensionOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddDataTelemetryBase(
            this IServiceCollection services,
            Action<DataExtensionOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingleton<IValidateOptions<DataExtensionOptions>, DataExtensionOptionsValidator>();

            return services;
        }
    }
}
