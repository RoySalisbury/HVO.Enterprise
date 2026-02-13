using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Grpc.Client;
using HVO.Enterprise.Telemetry.Grpc.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Grpc.Extensions
{
    /// <summary>
    /// Extension methods for registering gRPC telemetry interceptors with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds gRPC telemetry interceptors (server and client) for automatic distributed tracing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="GrpcTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// Registers the following services:
        /// </para>
        /// <list type="bullet">
        ///   <item><see cref="TelemetryServerInterceptor"/> as a singleton</item>
        ///   <item><see cref="TelemetryClientInterceptor"/> as a singleton</item>
        ///   <item><see cref="GrpcTelemetryOptions"/> validated via <see cref="IValidateOptions{TOptions}"/></item>
        /// </list>
        /// <para>
        /// This method is idempotent — calling it multiple times will not register duplicate services.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddGrpcTelemetry(options =&gt;
        /// {
        ///     options.SuppressHealthChecks = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddGrpcTelemetry(
            this IServiceCollection services,
            Action<GrpcTelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(TelemetryServerInterceptor)))
                return services;

            // Configure options via the established IOptions<T> pattern
            var optionsBuilder = services.AddOptions<GrpcTelemetryOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register options validator
            services.AddSingleton<IValidateOptions<GrpcTelemetryOptions>, GrpcTelemetryOptionsValidator>();

            // Register the server interceptor as a singleton
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GrpcTelemetryOptions>>().Value;
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<TelemetryServerInterceptor>();
                return new TelemetryServerInterceptor(options, logger);
            });

            // Register the client interceptor as a singleton
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GrpcTelemetryOptions>>().Value;
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<TelemetryClientInterceptor>();
                return new TelemetryClientInterceptor(options, logger);
            });

            return services;
        }
    }
}
