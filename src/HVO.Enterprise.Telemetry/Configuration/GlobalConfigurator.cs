using System;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Fluent configurator for global settings.
    /// </summary>
    public sealed class GlobalConfigurator
    {
        private readonly ConfigurationProvider _provider;
        private readonly OperationConfiguration _config = new OperationConfiguration();

        internal GlobalConfigurator(ConfigurationProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Sets the global sampling rate.
        /// </summary>
        /// <param name="rate">Sampling rate (0.0 to 1.0).</param>
        /// <returns>The configurator.</returns>
        public GlobalConfigurator SamplingRate(double rate)
        {
            _config.SamplingRate = rate;
            return this;
        }

        /// <summary>
        /// Enables or disables instrumentation globally.
        /// </summary>
        /// <param name="enabled">Whether instrumentation is enabled.</param>
        /// <returns>The configurator.</returns>
        public GlobalConfigurator Enabled(bool enabled)
        {
            _config.Enabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets global parameter capture mode.
        /// </summary>
        /// <param name="mode">Parameter capture mode.</param>
        /// <returns>The configurator.</returns>
        public GlobalConfigurator CaptureParameters(ParameterCaptureMode mode)
        {
            _config.ParameterCapture = mode;
            return this;
        }

        /// <summary>
        /// Controls global exception recording.
        /// </summary>
        /// <param name="record">Whether to record exceptions.</param>
        /// <returns>The configurator.</returns>
        public GlobalConfigurator RecordExceptions(bool record)
        {
            _config.RecordExceptions = record;
            return this;
        }

        /// <summary>
        /// Sets the global timeout threshold in milliseconds.
        /// </summary>
        /// <param name="milliseconds">Timeout threshold in milliseconds.</param>
        /// <returns>The configurator.</returns>
        public GlobalConfigurator TimeoutThreshold(int milliseconds)
        {
            _config.TimeoutThresholdMs = milliseconds;
            return this;
        }

        /// <summary>
        /// Adds a global tag to telemetry operations.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <returns>The configurator.</returns>
        public GlobalConfigurator AddTag(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _config.Tags[key] = value;
            return this;
        }

        /// <summary>
        /// Applies the global configuration.
        /// </summary>
        public void Apply()
        {
            _provider.SetGlobalConfiguration(_config);
        }
    }
}
