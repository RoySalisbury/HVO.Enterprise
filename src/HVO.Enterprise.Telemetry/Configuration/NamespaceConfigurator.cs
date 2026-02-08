using System;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Fluent configurator for namespace settings.
    /// </summary>
    public sealed class NamespaceConfigurator
    {
        private readonly ConfigurationProvider _provider;
        private readonly string _namespacePattern;
        private readonly OperationConfiguration _config = new OperationConfiguration();

        internal NamespaceConfigurator(ConfigurationProvider provider, string namespacePattern)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _namespacePattern = namespacePattern ?? throw new ArgumentNullException(nameof(namespacePattern));
        }

        /// <summary>
        /// Sets the namespace sampling rate.
        /// </summary>
        /// <param name="rate">Sampling rate (0.0 to 1.0).</param>
        /// <returns>The configurator.</returns>
        public NamespaceConfigurator SamplingRate(double rate)
        {
            _config.SamplingRate = rate;
            return this;
        }

        /// <summary>
        /// Enables or disables instrumentation for the namespace.
        /// </summary>
        /// <param name="enabled">Whether instrumentation is enabled.</param>
        /// <returns>The configurator.</returns>
        public NamespaceConfigurator Enabled(bool enabled)
        {
            _config.Enabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets parameter capture mode for the namespace.
        /// </summary>
        /// <param name="mode">Parameter capture mode.</param>
        /// <returns>The configurator.</returns>
        public NamespaceConfigurator CaptureParameters(ParameterCaptureMode mode)
        {
            _config.ParameterCapture = mode;
            return this;
        }

        /// <summary>
        /// Controls exception recording for the namespace.
        /// </summary>
        /// <param name="record">Whether to record exceptions.</param>
        /// <returns>The configurator.</returns>
        public NamespaceConfigurator RecordExceptions(bool record)
        {
            _config.RecordExceptions = record;
            return this;
        }

        /// <summary>
        /// Sets the timeout threshold for the namespace.
        /// </summary>
        /// <param name="milliseconds">Timeout threshold in milliseconds.</param>
        /// <returns>The configurator.</returns>
        public NamespaceConfigurator TimeoutThreshold(int milliseconds)
        {
            _config.TimeoutThresholdMs = milliseconds;
            return this;
        }

        /// <summary>
        /// Adds a namespace tag to telemetry operations.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <returns>The configurator.</returns>
        public NamespaceConfigurator AddTag(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _config.Tags[key] = value;
            return this;
        }

        /// <summary>
        /// Applies the namespace configuration.
        /// </summary>
        public void Apply()
        {
            _provider.SetNamespaceConfiguration(_namespacePattern, _config);
        }
    }
}
