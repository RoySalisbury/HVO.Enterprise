using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Loads hierarchical configuration from JSON and applies it to a provider.
    /// </summary>
    public static class ConfigurationFileProvider
    {
        /// <summary>
        /// Loads configuration from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to configuration file.</param>
        /// <returns>Parsed configuration file.</returns>
        public static HierarchicalConfigurationFile LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var json = File.ReadAllText(filePath);
            return LoadFromJson(json);
        }

        /// <summary>
        /// Loads configuration from JSON payload.
        /// </summary>
        /// <param name="json">JSON configuration.</param>
        /// <returns>Parsed configuration file.</returns>
        public static HierarchicalConfigurationFile LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            var file = JsonSerializer.Deserialize<HierarchicalConfigurationFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (file == null)
                throw new InvalidOperationException("Configuration JSON deserialized to null.");

            file.Validate();
            return file;
        }

        /// <summary>
        /// Applies configuration to the provider using the specified source.
        /// </summary>
        /// <param name="provider">Configuration provider.</param>
        /// <param name="file">Configuration file payload.</param>
        /// <param name="source">Configuration source.</param>
        /// <param name="typeResolver">Optional type resolver.</param>
        /// <param name="methodResolver">Optional method resolver.</param>
        public static void ApplyTo(
            ConfigurationProvider provider,
            HierarchicalConfigurationFile file,
            ConfigurationSourceKind source = ConfigurationSourceKind.File,
            Func<string, Type?>? typeResolver = null,
            Func<Type, string, MethodInfo?>? methodResolver = null)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            file.Validate();

            if (file.Global != null)
                provider.SetGlobalConfiguration(file.Global, source);

            foreach (var kvp in file.Namespaces)
            {
                provider.SetNamespaceConfiguration(kvp.Key, kvp.Value, source);
            }

            foreach (var kvp in file.Types)
            {
                var type = ResolveType(kvp.Key, typeResolver);
                if (type != null)
                {
                    provider.SetTypeConfiguration(type, kvp.Value, source);
                }
            }

            foreach (var kvp in file.Methods)
            {
                if (TryParseMethodKey(kvp.Key, out var typeName, out var methodName))
                {
                    var type = ResolveType(typeName, typeResolver);
                    if (type != null)
                    {
                        var method = ResolveMethod(type, methodName, methodResolver);
                        if (method != null)
                        {
                            provider.SetMethodConfiguration(method, kvp.Value, source);
                        }
                    }
                }
            }
        }

        private static Type? ResolveType(string typeName, Func<string, Type?>? resolver)
        {
            if (resolver != null)
                return resolver(typeName);

            return Type.GetType(typeName, throwOnError: false);
        }

        private static MethodInfo? ResolveMethod(Type type, string methodName, Func<Type, string, MethodInfo?>? resolver)
        {
            if (resolver != null)
                return resolver(type, methodName);

            return type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static bool TryParseMethodKey(string key, out string typeName, out string methodName)
        {
            typeName = string.Empty;
            methodName = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            var parts = key.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length != 2)
                return false;

            typeName = parts[0].Trim();
            methodName = parts[1].Trim();

            return !string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(methodName);
        }
    }
}
