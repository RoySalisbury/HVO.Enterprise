using System;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Fluent configurator for type-specific settings.
    /// </summary>
    /// <typeparam name="T">Target type.</typeparam>
    public sealed class TypeConfigurator<T>
    {
        private readonly ConfigurationProvider _provider;
        private readonly OperationConfiguration _config = new OperationConfiguration();

        internal TypeConfigurator(ConfigurationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Sets the type sampling rate.
        /// </summary>
        /// <param name="rate">Sampling rate (0.0 to 1.0).</param>
        /// <returns>The configurator.</returns>
        public TypeConfigurator<T> SamplingRate(double rate)
        {
            _config.SamplingRate = rate;
            return this;
        }

        /// <summary>
        /// Enables or disables instrumentation for the type.
        /// </summary>
        /// <param name="enabled">Whether instrumentation is enabled.</param>
        /// <returns>The configurator.</returns>
        public TypeConfigurator<T> Enabled(bool enabled)
        {
            _config.Enabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets parameter capture mode for the type.
        /// </summary>
        /// <param name="mode">Parameter capture mode.</param>
        /// <returns>The configurator.</returns>
        public TypeConfigurator<T> CaptureParameters(ParameterCaptureMode mode)
        {
            _config.ParameterCapture = mode;
            return this;
        }

        /// <summary>
        /// Controls exception recording for the type.
        /// </summary>
        /// <param name="record">Whether to record exceptions.</param>
        /// <returns>The configurator.</returns>
        public TypeConfigurator<T> RecordExceptions(bool record)
        {
            _config.RecordExceptions = record;
            return this;
        }

        /// <summary>
        /// Sets the timeout threshold for the type.
        /// </summary>
        /// <param name="milliseconds">Timeout threshold in milliseconds.</param>
        /// <returns>The configurator.</returns>
        public TypeConfigurator<T> TimeoutThreshold(int milliseconds)
        {
            _config.TimeoutThresholdMs = milliseconds;
            return this;
        }

        /// <summary>
        /// Adds a type tag to telemetry operations.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <returns>The configurator.</returns>
        public TypeConfigurator<T> AddTag(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _config.Tags[key] = value;
            return this;
        }

        /// <summary>
        /// Applies the type configuration.
        /// </summary>
        public void Apply()
        {
            _provider.SetTypeConfiguration(typeof(T), _config);
        }
    }
}
