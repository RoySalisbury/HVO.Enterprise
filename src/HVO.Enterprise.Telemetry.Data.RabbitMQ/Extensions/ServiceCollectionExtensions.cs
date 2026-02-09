using System;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Extensions
{
    /// <summary>
    /// Extension methods for registering RabbitMQ telemetry services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds RabbitMQ telemetry services including options validation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for <see cref="RabbitMqTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddRabbitMqTelemetry(
            this IServiceCollection services,
            Action<RabbitMqTelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingleton<IValidateOptions<RabbitMqTelemetryOptions>, RabbitMqTelemetryOptionsValidator>();

            return services;
        }
    }
}
