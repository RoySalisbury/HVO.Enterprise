using System;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Fluent configurator for method-specific settings.
    /// </summary>
    public sealed class MethodConfigurator
    {
        private readonly ConfigurationProvider _provider;
        private readonly MethodInfo _method;
        private readonly OperationConfiguration _config = new OperationConfiguration();

        internal MethodConfigurator(ConfigurationProvider provider, MethodInfo method)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        /// <summary>
        /// Sets the method sampling rate.
        /// </summary>
        /// <param name="rate">Sampling rate (0.0 to 1.0).</param>
        /// <returns>The configurator.</returns>
        public MethodConfigurator SamplingRate(double rate)
        {
            _config.SamplingRate = rate;
            return this;
        }

        /// <summary>
        /// Enables or disables instrumentation for the method.
        /// </summary>
        /// <param name="enabled">Whether instrumentation is enabled.</param>
        /// <returns>The configurator.</returns>
        public MethodConfigurator Enabled(bool enabled)
        {
            _config.Enabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets parameter capture mode for the method.
        /// </summary>
        /// <param name="mode">Parameter capture mode.</param>
        /// <returns>The configurator.</returns>
        public MethodConfigurator CaptureParameters(ParameterCaptureMode mode)
        {
            _config.ParameterCapture = mode;
            return this;
        }

        /// <summary>
        /// Controls exception recording for the method.
        /// </summary>
        /// <param name="record">Whether to record exceptions.</param>
        /// <returns>The configurator.</returns>
        public MethodConfigurator RecordExceptions(bool record)
        {
            _config.RecordExceptions = record;
            return this;
        }

        /// <summary>
        /// Sets the timeout threshold for the method.
        /// </summary>
        /// <param name="milliseconds">Timeout threshold in milliseconds.</param>
        /// <returns>The configurator.</returns>
        public MethodConfigurator TimeoutThreshold(int milliseconds)
        {
            _config.TimeoutThresholdMs = milliseconds;
            return this;
        }

        /// <summary>
        /// Adds a method tag to telemetry operations.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <returns>The configurator.</returns>
        public MethodConfigurator AddTag(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _config.Tags[key] = value;
            return this;
        }

        /// <summary>
        /// Applies the method configuration.
        /// </summary>
        public void Apply()
        {
            _provider.SetMethodConfiguration(_method, _config);
        }
    }
}
