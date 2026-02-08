using System;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Fluent API for configuring telemetry.
    /// </summary>
    public sealed class TelemetryConfigurator
    {
        private readonly ConfigurationProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryConfigurator"/> class.
        /// </summary>
        public TelemetryConfigurator()
            : this(ConfigurationProvider.Instance)
        {
        }

        internal TelemetryConfigurator(ConfigurationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Configures global defaults.
        /// </summary>
        /// <returns>Global configurator.</returns>
        public GlobalConfigurator Global()
        {
            return new GlobalConfigurator(_provider);
        }

        /// <summary>
        /// Configures a specific namespace.
        /// </summary>
        /// <param name="namespacePattern">Namespace pattern.</param>
        /// <returns>Namespace configurator.</returns>
        public NamespaceConfigurator Namespace(string namespacePattern)
        {
            return new NamespaceConfigurator(_provider, namespacePattern);
        }

        /// <summary>
        /// Configures a specific type.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <returns>Type configurator.</returns>
        public TypeConfigurator<T> ForType<T>()
        {
            return new TypeConfigurator<T>(_provider);
        }

        /// <summary>
        /// Configures a specific method.
        /// </summary>
        /// <param name="method">Target method.</param>
        /// <returns>Method configurator.</returns>
        public MethodConfigurator ForMethod(MethodInfo method)
        {
            return new MethodConfigurator(_provider, method);
        }
    }

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
