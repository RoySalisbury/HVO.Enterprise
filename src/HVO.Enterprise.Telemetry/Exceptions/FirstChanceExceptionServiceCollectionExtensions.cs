using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Extension methods for registering first-chance exception monitoring
    /// with dependency injection.
    /// </summary>
    public static class FirstChanceExceptionServiceCollectionExtensions
    {
        /// <summary>
        /// Adds first-chance exception monitoring to the service collection.
        /// When enabled, subscribes to <see cref="AppDomain.CurrentDomain"/>
        /// <c>FirstChanceException</c> to detect exceptions the instant they are thrown,
        /// including those that are subsequently caught and suppressed.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="FirstChanceExceptionOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// This method is idempotent — calling it multiple times will not add duplicate registrations.
        /// </para>
        /// <para>
        /// The monitor is disabled by default. Set <see cref="FirstChanceExceptionOptions.Enabled"/>
        /// to <c>true</c> in code or via <c>appsettings.json</c> to activate it. Configuration
        /// changes are detected at runtime via <c>IOptionsMonitor</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddFirstChanceExceptionMonitoring(options =>
        /// {
        ///     options.Enabled = true;
        ///     options.MaxEventsPerSecond = 50;
        ///     options.IncludeExceptionTypes.Add("System.InvalidOperationException");
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddFirstChanceExceptionMonitoring(
            this IServiceCollection services,
            Action<FirstChanceExceptionOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(FirstChanceExceptionMonitor)))
                return services;

            // Configure options
            var optionsBuilder = services.AddOptions<FirstChanceExceptionOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Register monitor as singleton
            services.TryAddSingleton<FirstChanceExceptionMonitor>();

            // Register as hosted service for automatic start/stop
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp =>
                sp.GetRequiredService<FirstChanceExceptionMonitor>());

            return services;
        }
    }
}
