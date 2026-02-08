using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Integrates sampling with ActivitySource creation.
    /// </summary>
    public static class SamplingActivitySourceExtensions
    {
        private static readonly object ListenerLock = new object();
        private static ISampler _globalSampler = new ProbabilisticSampler(1.0);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ActivitySource> _activitySources 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, ActivitySource>();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ActivityListener> _listeners 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, ActivityListener>();

        /// <summary>
        /// Configures the global sampler.
        /// </summary>
        /// <param name="sampler">Sampler to use globally.</param>
        public static void ConfigureSampling(ISampler sampler)
        {
            _globalSampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
        }

        /// <summary>
        /// Builds a sampler from telemetry options and configuration overrides.
        /// </summary>
        /// <param name="options">Telemetry options.</param>
        /// <param name="configurationProvider">Optional configuration provider.</param>
        /// <returns>Configured sampler.</returns>
        internal static ISampler BuildSampler(TelemetryOptions options, ConfigurationProvider? configurationProvider = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var provider = configurationProvider ?? ConfigurationProvider.Instance;
            var globalConfig = provider.GetEffectiveConfiguration();
            
            // Only use configuration provider rate if a global override is explicitly configured
            double defaultRate;
            if (globalConfig.SamplingRate.HasValue)
            {
                defaultRate = globalConfig.SamplingRate.Value;
            }
            else
            {
                defaultRate = options.DefaultSamplingRate;
            }

            var perSourceSampler = new PerSourceSampler(new ProbabilisticSampler(defaultRate));

            foreach (var kvp in options.Sampling)
            {
                var samplingOptions = kvp.Value;
                var baseSampler = new ProbabilisticSampler(samplingOptions.Rate);
                ISampler sampler = samplingOptions.AlwaysSampleErrors
                    ? (ISampler)new ConditionalSampler(baseSampler, alwaysSampleErrors: true)
                    : baseSampler;

                perSourceSampler.ConfigureSource(kvp.Key, sampler);
            }

            return perSourceSampler;
        }

        /// <summary>
        /// Configures global sampling from telemetry options.
        /// </summary>
        /// <param name="options">Telemetry options.</param>
        /// <param name="configurationProvider">Optional configuration provider.</param>
        public static void ConfigureFromOptions(TelemetryOptions options, ConfigurationProvider? configurationProvider = null)
        {
            ConfigureSampling(BuildSampler(options, configurationProvider));
        }

        /// <summary>
        /// Configures sampling from <see cref="IOptionsMonitor{TOptions}"/> updates.
        /// </summary>
        /// <param name="monitor">Options monitor.</param>
        /// <param name="configurationProvider">Optional configuration provider.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable ConfigureFromOptionsMonitor(
            IOptionsMonitor<TelemetryOptions> monitor,
            ConfigurationProvider? configurationProvider = null)
        {
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            ConfigureFromOptions(monitor.CurrentValue, configurationProvider);

            var subscription = monitor.OnChange((options, _) => ConfigureFromOptions(options, configurationProvider));
            return subscription ?? NullDisposable.Instance;
        }

        /// <summary>
        /// Configures sampling from file-based hot reload events.
        /// </summary>
        /// <param name="reloader">File configuration reloader.</param>
        /// <param name="configurationProvider">Optional configuration provider.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable ConfigureFromFileReloader(
            FileConfigurationReloader reloader,
            ConfigurationProvider? configurationProvider = null)
        {
            if (reloader == null)
                throw new ArgumentNullException(nameof(reloader));

            ConfigureFromOptions(reloader.CurrentOptions, configurationProvider);

            EventHandler<ConfigurationChangedEventArgs>? handler = (_, args) =>
            {
                ConfigureFromOptions(args.NewConfiguration, configurationProvider);
            };

            reloader.ConfigurationChanged += handler;

            return new CallbackDisposable(() => reloader.ConfigurationChanged -= handler);
        }

        /// <summary>
        /// Creates an ActivitySource with sampling configuration.
        /// Caches ActivitySource instances by name to avoid listener leaks.
        /// </summary>
        /// <param name="name">ActivitySource name.</param>
        /// <param name="version">Optional version.</param>
        /// <param name="sampler">Optional sampler override.</param>
        /// <param name="logger">Optional logger for debug decisions.</param>
        /// <returns>Configured ActivitySource.</returns>
        public static ActivitySource CreateWithSampling(
            string name,
            string? version = null,
            ISampler? sampler = null,
            ILogger? logger = null)
        {
            var key = string.Concat(name, ":", version ?? string.Empty);
            
            // Return cached ActivitySource if it exists
            if (_activitySources.TryGetValue(key, out var existingSource))
            {
                return existingSource;
            }

            var source = new ActivitySource(name, version);
            var effectiveLogger = logger ?? NullLogger.Instance;

            // Only add listener once per ActivitySource name
            if (!_listeners.ContainsKey(name))
            {
                lock (ListenerLock)
                {
                    // Use TryAdd to safely handle race conditions
                    if (!_listeners.ContainsKey(name))
                    {
                        var listener = new ActivityListener
                        {
                            ShouldListenTo = activitySource => string.Equals(activitySource.Name, name, StringComparison.Ordinal),
                            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                            {
                                var effectiveSampler = sampler ?? _globalSampler;
                                var context = new SamplingContext(
                                    options.TraceId,
                                    options.Name,
                                    name,
                                    options.Kind,
                                    options.Tags);

                                var result = effectiveSampler.ShouldSample(context);
                                SamplingMetrics.RecordDecision(result, name);

                                if (effectiveLogger.IsEnabled(LogLevel.Debug))
                                {
                                    effectiveLogger.LogDebug(
                                        "Sampling decision {Decision} for {ActivityName} ({Source}): {Reason}",
                                        result.Decision,
                                        options.Name,
                                        name,
                                        result.Reason ?? string.Empty);
                                }

                                return result.Decision == SamplingDecision.RecordAndSample
                                    ? ActivitySamplingResult.AllDataAndRecorded
                                    : ActivitySamplingResult.PropagationData;
                            }
                        };

                        ActivitySource.AddActivityListener(listener);
                        // Only add to dictionary if we successfully added the listener
                        _listeners.TryAdd(name, listener);
                    }
                }
            }

            _activitySources.TryAdd(key, source);
            return source;
        }

        private sealed class CallbackDisposable : IDisposable
        {
            private readonly Action _disposeAction;
            private bool _disposed;

            public CallbackDisposable(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _disposeAction();
            }
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();

            private NullDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
