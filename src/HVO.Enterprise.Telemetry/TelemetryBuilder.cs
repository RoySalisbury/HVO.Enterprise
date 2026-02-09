using System;
using HVO.Enterprise.Telemetry.Configuration;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Fluent builder for configuring telemetry services.
    /// Obtained via <see cref="TelemetryServiceCollectionExtensions.AddTelemetry(IServiceCollection, Action{TelemetryBuilder})"/>.
    /// </summary>
    public sealed class TelemetryBuilder
    {
        /// <summary>
        /// Gets the underlying service collection.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        internal TelemetryBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Configures telemetry options.
        /// </summary>
        /// <param name="configure">Delegate to configure options.</param>
        /// <returns>This builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
        public TelemetryBuilder Configure(Action<TelemetryOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            Services.Configure(configure);
            return this;
        }

        /// <summary>
        /// Adds an activity source name to the list of enabled sources.
        /// </summary>
        /// <param name="name">The activity source name.</param>
        /// <returns>This builder for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public TelemetryBuilder AddActivitySource(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Activity source name cannot be null or empty.", nameof(name));

            Services.Configure<TelemetryOptions>(options =>
            {
                if (options.ActivitySources == null)
                    options.ActivitySources = new List<string>();

                if (!options.ActivitySources.Contains(name))
                    options.ActivitySources.Add(name);
            });

            return this;
        }

        /// <summary>
        /// Configures HTTP client instrumentation options.
        /// </summary>
        /// <param name="configure">Optional delegate to configure HTTP instrumentation options.</param>
        /// <returns>This builder for chaining.</returns>
        public TelemetryBuilder AddHttpInstrumentation(Action<HttpInstrumentationOptions>? configure = null)
        {
            if (configure != null)
            {
                Services.Configure(configure);
            }

            Services.Configure<TelemetryOptions>(options =>
            {
                options.Features ??= new FeatureFlags();
                options.Features.EnableHttpInstrumentation = true;
            });

            return this;
        }
    }
}
