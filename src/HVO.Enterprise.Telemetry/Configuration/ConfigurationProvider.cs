using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Provides hierarchical configuration for telemetry operations.
    /// </summary>
    public sealed class ConfigurationProvider
    {
        private static readonly ConfigurationSourceKind[] SourceOrder =
        {
            ConfigurationSourceKind.Code,
            ConfigurationSourceKind.File,
            ConfigurationSourceKind.Runtime
        };

        private readonly ConcurrentDictionary<string, OperationConfiguration>[] _namespaceConfigurations;
        private readonly ConcurrentDictionary<Type, OperationConfiguration>[] _typeConfigurations;
        private readonly ConcurrentDictionary<MethodInfo, OperationConfiguration>[] _methodConfigurations;
        private readonly OperationConfiguration?[] _globalConfigurations;
        private OperationConfiguration _defaultConfiguration;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static ConfigurationProvider Instance { get; } = new ConfigurationProvider();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationProvider"/> class.
        /// </summary>
        public ConfigurationProvider()
        {
            _namespaceConfigurations = CreateDictionaryArray<string>(StringComparer.OrdinalIgnoreCase);
            _typeConfigurations = CreateDictionaryArray<Type>();
            _methodConfigurations = CreateDictionaryArray<MethodInfo>();
            _globalConfigurations = new OperationConfiguration?[4];

            _defaultConfiguration = new OperationConfiguration
            {
                Enabled = true,
                SamplingRate = 1.0,
                ParameterCapture = ParameterCaptureMode.NamesOnly,
                RecordExceptions = true
            };
        }

        /// <summary>
        /// Sets the global configuration for a specific source.
        /// </summary>
        /// <param name="config">Configuration to apply.</param>
        /// <param name="source">Configuration source.</param>
        public void SetGlobalConfiguration(
            OperationConfiguration config,
            ConfigurationSourceKind source = ConfigurationSourceKind.Code)
        {
            ValidateSource(source);
            var normalizedConfig = NormalizeConfiguration(config);
            Volatile.Write(ref _globalConfigurations[(int)source], normalizedConfig);
        }

        /// <summary>
        /// Sets configuration for a namespace pattern.
        /// </summary>
        /// <param name="namespacePattern">Namespace pattern (exact or prefix with *).</param>
        /// <param name="config">Configuration to apply.</param>
        /// <param name="source">Configuration source.</param>
        public void SetNamespaceConfiguration(
            string namespacePattern,
            OperationConfiguration config,
            ConfigurationSourceKind source = ConfigurationSourceKind.Code)
        {
            if (string.IsNullOrWhiteSpace(namespacePattern))
                throw new ArgumentNullException(nameof(namespacePattern));

            ValidateSource(source);
            _namespaceConfigurations[(int)source][namespacePattern] = NormalizeConfiguration(config);
        }

        /// <summary>
        /// Sets configuration for a specific type.
        /// </summary>
        /// <param name="type">Target type.</param>
        /// <param name="config">Configuration to apply.</param>
        /// <param name="source">Configuration source.</param>
        public void SetTypeConfiguration(
            Type type,
            OperationConfiguration config,
            ConfigurationSourceKind source = ConfigurationSourceKind.Code)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ValidateSource(source);
            _typeConfigurations[(int)source][type] = NormalizeConfiguration(config);
        }

        /// <summary>
        /// Sets configuration for a specific method.
        /// </summary>
        /// <param name="method">Target method.</param>
        /// <param name="config">Configuration to apply.</param>
        /// <param name="source">Configuration source.</param>
        public void SetMethodConfiguration(
            MethodInfo method,
            OperationConfiguration config,
            ConfigurationSourceKind source = ConfigurationSourceKind.Code)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            ValidateSource(source);
            _methodConfigurations[(int)source][method] = NormalizeConfiguration(config);
        }

        /// <summary>
        /// Applies attribute-based configuration for a type and its methods.
        /// </summary>
        /// <param name="type">Type to scan for attributes.</param>
        public void ApplyAttributeConfiguration(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var typeAttribute = (TelemetryConfigurationAttribute?)Attribute.GetCustomAttribute(
                type,
                typeof(TelemetryConfigurationAttribute));

            if (typeAttribute != null)
            {
                SetTypeConfiguration(type, typeAttribute.ToConfiguration(), ConfigurationSourceKind.Code);
            }

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var methodAttribute = (TelemetryConfigurationAttribute?)Attribute.GetCustomAttribute(
                    method,
                    typeof(TelemetryConfigurationAttribute));

                if (methodAttribute != null)
                {
                    SetMethodConfiguration(method, methodAttribute.ToConfiguration(), ConfigurationSourceKind.Code);
                }
            }
        }

        /// <summary>
        /// Gets the effective configuration for an operation.
        /// Applies precedence: Call > Method > Type > Namespace > Global > Default.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <param name="method">Target method.</param>
        /// <param name="callConfig">Call-specific configuration.</param>
        /// <returns>Effective configuration.</returns>
        public OperationConfiguration GetEffectiveConfiguration(
            Type? targetType = null,
            MethodInfo? method = null,
            OperationConfiguration? callConfig = null)
        {
            var effective = _defaultConfiguration.Clone();

            foreach (var source in SourceOrder)
            {
                var globalConfig = _globalConfigurations[(int)source];
                if (globalConfig != null)
                    effective = globalConfig.MergeWith(effective);

                if (targetType != null)
                {
                    if (TryGetNamespaceConfiguration(targetType.Namespace, source, out var namespaceConfig, out _))
                        effective = namespaceConfig.MergeWith(effective);

                    if (_typeConfigurations[(int)source].TryGetValue(targetType, out var typeConfig))
                        effective = typeConfig.MergeWith(effective);
                }

                if (method != null && _methodConfigurations[(int)source].TryGetValue(method, out var methodConfig))
                    effective = methodConfig.MergeWith(effective);
            }

            if (callConfig != null)
            {
                callConfig.Validate();
                effective = callConfig.MergeWith(effective);
            }

            return effective;
        }

        /// <summary>
        /// Lists all configured overrides.
        /// </summary>
        /// <returns>Configured overrides.</returns>
        public IReadOnlyList<ConfigurationEntry> GetAllConfigurations()
        {
            var entries = new List<ConfigurationEntry>();

            foreach (var source in SourceOrder)
            {
                var globalConfig = _globalConfigurations[(int)source];
                if (globalConfig != null)
                {
                    entries.Add(new ConfigurationEntry(ConfigurationLevel.Global, source, "global", globalConfig.Clone()));
                }

                foreach (var kvp in _namespaceConfigurations[(int)source])
                {
                    entries.Add(new ConfigurationEntry(ConfigurationLevel.Namespace, source, kvp.Key, kvp.Value.Clone()));
                }

                foreach (var kvp in _typeConfigurations[(int)source])
                {
                    entries.Add(new ConfigurationEntry(ConfigurationLevel.Type, source, kvp.Key.FullName, kvp.Value.Clone()));
                }

                foreach (var kvp in _methodConfigurations[(int)source])
                {
                    var methodIdentifier = kvp.Key.DeclaringType != null
                        ? kvp.Key.DeclaringType.FullName + "::" + kvp.Key.Name
                        : kvp.Key.Name;

                    entries.Add(new ConfigurationEntry(ConfigurationLevel.Method, source, methodIdentifier, kvp.Value.Clone()));
                }
            }

            return entries;
        }

        /// <summary>
        /// Clears all configurations and resets defaults.
        /// </summary>
        public void Clear()
        {
            foreach (var source in SourceOrder)
            {
                _namespaceConfigurations[(int)source].Clear();
                _typeConfigurations[(int)source].Clear();
                _methodConfigurations[(int)source].Clear();
                Volatile.Write(ref _globalConfigurations[(int)source], null);
            }

            var defaultConfig = new OperationConfiguration
            {
                Enabled = true,
                SamplingRate = 1.0,
                ParameterCapture = ParameterCaptureMode.NamesOnly,
                RecordExceptions = true
            };
            Volatile.Write(ref _defaultConfiguration, defaultConfig);
        }

        internal IReadOnlyList<ConfigurationLayer> GetConfigurationLayers(
            Type? targetType,
            MethodInfo? method,
            OperationConfiguration? callConfig)
        {
            var layers = new List<ConfigurationLayer>
            {
                new ConfigurationLayer(
                    ConfigurationLevel.GlobalDefault,
                    ConfigurationSourceKind.Default,
                    "default",
                    _defaultConfiguration.Clone())
            };

            foreach (var source in SourceOrder)
            {
                var globalConfig = _globalConfigurations[(int)source];
                if (globalConfig != null)
                {
                    layers.Add(new ConfigurationLayer(ConfigurationLevel.Global, source, "global", globalConfig.Clone()));
                }

                if (targetType != null)
                {
                    if (TryGetNamespaceConfiguration(targetType.Namespace, source, out var namespaceConfig, out var pattern))
                    {
                        layers.Add(new ConfigurationLayer(ConfigurationLevel.Namespace, source, pattern, namespaceConfig.Clone()));
                    }

                    if (_typeConfigurations[(int)source].TryGetValue(targetType, out var typeConfig))
                    {
                        layers.Add(new ConfigurationLayer(ConfigurationLevel.Type, source, targetType.FullName, typeConfig.Clone()));
                    }
                }

                if (method != null && _methodConfigurations[(int)source].TryGetValue(method, out var methodConfig))
                {
                    var identifier = method.DeclaringType != null
                        ? method.DeclaringType.FullName + "::" + method.Name
                        : method.Name;
                    layers.Add(new ConfigurationLayer(ConfigurationLevel.Method, source, identifier, methodConfig.Clone()));
                }
            }

            if (callConfig != null)
            {
                callConfig.Validate();
                layers.Add(new ConfigurationLayer(ConfigurationLevel.Call, ConfigurationSourceKind.Runtime, "call", callConfig.Clone()));
            }

            return layers;
        }

        private static OperationConfiguration NormalizeConfiguration(OperationConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.Validate();
            config.EnsureDefaults();
            return config.Clone();
        }

        private static void ValidateSource(ConfigurationSourceKind source)
        {
            if (source == ConfigurationSourceKind.Default)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Default source cannot be used for overrides.");
            }

            switch (source)
            {
                case ConfigurationSourceKind.Code:
                case ConfigurationSourceKind.File:
                case ConfigurationSourceKind.Runtime:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), "Invalid configuration source.");
            }
        }

        private bool TryGetNamespaceConfiguration(
            string? targetNamespace,
            ConfigurationSourceKind source,
            out OperationConfiguration configuration,
            out string? matchedPattern)
        {
            configuration = null!;
            matchedPattern = null;

            var namespaceValue = targetNamespace ?? string.Empty;
            if (namespaceValue.Length == 0)
                return false;

            var dictionary = _namespaceConfigurations[(int)source];
            if (dictionary.TryGetValue(namespaceValue, out var exactMatch))
            {
                configuration = exactMatch;
                matchedPattern = namespaceValue;
                return true;
            }

            OperationConfiguration? bestMatch = null;
            string? bestPattern = null;
            var bestLength = -1;

            foreach (var kvp in dictionary)
            {
                var pattern = kvp.Key;
                if (!pattern.EndsWith("*", StringComparison.Ordinal))
                    continue;

                var prefix = pattern.Substring(0, pattern.Length - 1);
                if (!namespaceValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (prefix.Length > bestLength)
                {
                    bestMatch = kvp.Value;
                    bestPattern = pattern;
                    bestLength = prefix.Length;
                }
            }

            if (bestMatch != null)
            {
                configuration = bestMatch;
                matchedPattern = bestPattern ?? namespaceValue;
                return true;
            }

            return false;
        }

        private static ConcurrentDictionary<TKey, OperationConfiguration>[] CreateDictionaryArray<TKey>(
            IEqualityComparer<TKey>? comparer = null)
            where TKey : notnull
        {
            return new[]
            {
                new ConcurrentDictionary<TKey, OperationConfiguration>(comparer ?? EqualityComparer<TKey>.Default),
                new ConcurrentDictionary<TKey, OperationConfiguration>(comparer ?? EqualityComparer<TKey>.Default),
                new ConcurrentDictionary<TKey, OperationConfiguration>(comparer ?? EqualityComparer<TKey>.Default),
                new ConcurrentDictionary<TKey, OperationConfiguration>(comparer ?? EqualityComparer<TKey>.Default)
            };
        }
    }
}
