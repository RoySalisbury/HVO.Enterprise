using System;
using HVO.Enterprise.Telemetry.Proxies;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering telemetry-instrumented services with
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    public static class TelemetryInstrumentationExtensions
    {
        /// <summary>
        /// Registers the <see cref="ITelemetryProxyFactory"/> as a singleton in the service collection.
        /// Must be called before any <c>AddInstrumented*</c> methods.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddTelemetryProxyFactory(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<ITelemetryProxyFactory, TelemetryProxyFactory>();
            return services;
        }

        /// <summary>
        /// Registers an instrumented transient service. The implementation is resolved and
        /// wrapped in a <see cref="TelemetryDispatchProxy{T}"/> on each request.
        /// </summary>
        /// <typeparam name="TInterface">The interface type (must be an interface).</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional instrumentation options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddInstrumentedTransient<TInterface, TImplementation>(
            this IServiceCollection services,
            InstrumentationOptions? options = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<TImplementation>();
            services.AddTransient<TInterface>(sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var factory = sp.GetRequiredService<ITelemetryProxyFactory>();
                return factory.CreateProxy<TInterface>(implementation, options);
            });

            return services;
        }

        /// <summary>
        /// Registers an instrumented scoped service. Both the implementation and the
        /// proxy share the same scope lifetime.
        /// </summary>
        /// <typeparam name="TInterface">The interface type (must be an interface).</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional instrumentation options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddInstrumentedScoped<TInterface, TImplementation>(
            this IServiceCollection services,
            InstrumentationOptions? options = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<TImplementation>();
            services.AddScoped<TInterface>(sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var factory = sp.GetRequiredService<ITelemetryProxyFactory>();
                return factory.CreateProxy<TInterface>(implementation, options);
            });

            return services;
        }

        /// <summary>
        /// Registers an instrumented singleton service. Both the implementation and the
        /// proxy live for the lifetime of the application.
        /// </summary>
        /// <typeparam name="TInterface">The interface type (must be an interface).</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional instrumentation options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddInstrumentedSingleton<TInterface, TImplementation>(
            this IServiceCollection services,
            InstrumentationOptions? options = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<TImplementation>();
            services.AddSingleton<TInterface>(sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var factory = sp.GetRequiredService<ITelemetryProxyFactory>();
                return factory.CreateProxy<TInterface>(implementation, options);
            });

            return services;
        }
    }
}
