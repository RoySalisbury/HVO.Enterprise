using System;
using HVO.Enterprise.Telemetry.Wcf.Client;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Wcf.Extensions
{
    /// <summary>
    /// Extension methods for registering WCF telemetry instrumentation with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds WCF client telemetry instrumentation to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="WcfExtensionOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services"/> is null.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Registers the following services:
        /// </para>
        /// <list type="bullet">
        ///   <item><see cref="TelemetryClientEndpointBehavior"/> as a singleton</item>
        ///   <item><see cref="WcfExtensionOptions"/> validated via <see cref="IValidateOptions{TOptions}"/></item>
        /// </list>
        /// <para>
        /// After calling this method, resolve <see cref="TelemetryClientEndpointBehavior"/>
        /// from the service provider and add it to WCF client endpoints.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Startup.cs or Program.cs
        /// services.AddWcfTelemetryInstrumentation(options =&gt;
        /// {
        ///     options.PropagateTraceContextInReply = true;
        ///     options.OperationFilter = op =&gt; !op.Contains("Health");
        /// });
        ///
        /// // Later, when creating WCF clients:
        /// var behavior = serviceProvider.GetRequiredService&lt;TelemetryClientEndpointBehavior&gt;();
        /// endpoint.EndpointBehaviors.Add(behavior);
        /// </code>
        /// </example>
        public static IServiceCollection AddWcfTelemetryInstrumentation(
            this IServiceCollection services,
            Action<WcfExtensionOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Configure options via the established IOptions<T> pattern
            var optionsBuilder = services.AddOptions<WcfExtensionOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register options validator
            services.AddSingleton<IValidateOptions<WcfExtensionOptions>, WcfExtensionOptionsValidator>();

            // Register the client endpoint behavior as a singleton
            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<WcfExtensionOptions>>().Value;
                return new TelemetryClientEndpointBehavior(options);
            });

            return services;
        }
    }
}
