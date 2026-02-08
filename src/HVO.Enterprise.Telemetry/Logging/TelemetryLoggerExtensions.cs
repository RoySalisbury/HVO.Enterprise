using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Extension methods for adding telemetry logging enrichment via dependency injection.
    /// </summary>
    /// <remarks>
    /// <para>These extensions register a <see cref="TelemetryEnrichedLoggerFactory"/>
    /// that wraps the existing <see cref="ILoggerFactory"/> to automatically enrich
    /// all log entries with Activity TraceId, SpanId, and CorrelationId.</para>
    /// <para>For standalone (non-DI) usage, see <see cref="TelemetryLogger"/>.</para>
    /// </remarks>
    public static class TelemetryLoggerExtensions
    {
        /// <summary>
        /// Adds telemetry enrichment to the logging pipeline.
        /// All loggers resolved from the container will automatically include
        /// Activity trace context and correlation ID in their log scopes.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configure">Optional action to configure enrichment options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>This method is idempotent — calling it multiple times will not
        /// register duplicate enrichment wrappers.</para>
        /// <para>The enrichment wrapper is registered as a decorator around the existing
        /// <see cref="ILoggerFactory"/>. It must be called <b>after</b> other logging
        /// registrations (e.g., <c>AddLogging()</c>, <c>AddSerilog()</c>) to ensure
        /// the inner factory is available.</para>
        /// </remarks>
        public static IServiceCollection AddTelemetryLoggingEnrichment(
            this IServiceCollection services,
            Action<TelemetryLoggerOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Idempotency guard — check if already registered
            if (services.Any(s => s.ServiceType == typeof(TelemetryLoggerOptions)))
                return services;

            var options = new TelemetryLoggerOptions();
            configure?.Invoke(options);

            // Register options as singleton for potential injection
            services.AddSingleton(options);

            // Decorate the existing ILoggerFactory with our enrichment wrapper.
            // We find and replace the existing ILoggerFactory registration to wrap it.
            var existingDescriptor = services.LastOrDefault(s => s.ServiceType == typeof(ILoggerFactory));
            if (existingDescriptor != null)
            {
                services.Remove(existingDescriptor);

                services.Add(new ServiceDescriptor(
                    typeof(ILoggerFactory),
                    sp =>
                    {
                        // Resolve the original factory using the captured descriptor
                        ILoggerFactory innerFactory;
                        if (existingDescriptor.ImplementationFactory != null)
                        {
                            innerFactory = (ILoggerFactory)existingDescriptor.ImplementationFactory(sp);
                        }
                        else if (existingDescriptor.ImplementationInstance != null)
                        {
                            innerFactory = (ILoggerFactory)existingDescriptor.ImplementationInstance;
                        }
                        else if (existingDescriptor.ImplementationType != null)
                        {
                            innerFactory = (ILoggerFactory)ActivatorUtilities.CreateInstance(
                                sp, existingDescriptor.ImplementationType);
                        }
                        else
                        {
                            // Fallback — use NullLoggerFactory (from Abstractions) as a safe default
                            innerFactory = NullLoggerFactory.Instance;
                        }

                        var resolvedOptions = sp.GetService<TelemetryLoggerOptions>() ?? options;
                        return new TelemetryEnrichedLoggerFactory(innerFactory, resolvedOptions);
                    },
                    existingDescriptor.Lifetime));
            }
            else
            {
                // No existing ILoggerFactory — register our wrapper with a default inner factory
                services.AddSingleton<ILoggerFactory>(sp =>
                {
                    var resolvedOptions = sp.GetService<TelemetryLoggerOptions>() ?? options;
                    return new TelemetryEnrichedLoggerFactory(NullLoggerFactory.Instance, resolvedOptions);
                });
            }

            return services;
        }
    }
}
