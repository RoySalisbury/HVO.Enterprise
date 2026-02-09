using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Dedicated marker type used for idempotency in
    /// <see cref="TelemetryLoggerExtensions.AddTelemetryLoggingEnrichment"/>.
    /// Unlike <see cref="TelemetryLoggerOptions"/>, this type cannot clash with
    /// options registrations made by application code or configuration binding.
    /// </summary>
    internal sealed class TelemetryEnrichmentMarker
    {
        internal static readonly TelemetryEnrichmentMarker Instance = new TelemetryEnrichmentMarker();
    }

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

            // Idempotency guard — uses a dedicated marker type so that separate
            // TelemetryLoggerOptions registrations (e.g. from config binding) do not
            // suppress enrichment, and a second call does not double-wrap the factory.
            if (services.Any(s => s.ServiceType == typeof(TelemetryEnrichmentMarker)))
                return services;

            services.AddSingleton(TelemetryEnrichmentMarker.Instance);

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
                            throw new InvalidOperationException(
                                "AddTelemetryLoggingEnrichment found an ILoggerFactory registration " +
                                "that could not be resolved. Ensure AddLogging() or an equivalent " +
                                "logging registration is called before AddTelemetryLoggingEnrichment().");
                        }

                        var resolvedOptions = sp.GetService<TelemetryLoggerOptions>() ?? options;
                        return new TelemetryEnrichedLoggerFactory(innerFactory, resolvedOptions);
                    },
                    existingDescriptor.Lifetime));
            }
            else
            {
                // No existing ILoggerFactory — fail fast with guidance rather than
                // silently wrapping NullLoggerFactory (which would drop all logs).
                throw new InvalidOperationException(
                    "No ILoggerFactory has been registered. Call AddLogging() or register " +
                    "an ILoggerFactory before calling AddTelemetryLoggingEnrichment().");
            }

            return services;
        }
    }
}
