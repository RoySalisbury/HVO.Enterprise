using System;
using System.Collections.Generic;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Diagnostic API for inspecting configuration.
    /// </summary>
    public static class ConfigurationDiagnostics
    {
        /// <summary>
        /// Explains why specific configuration values are applied.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <param name="method">Target method.</param>
        /// <param name="callConfig">Call-specific configuration.</param>
        /// <returns>Diagnostic report.</returns>
        public static ConfigurationDiagnosticReport ExplainConfiguration(
            Type? targetType = null,
            MethodInfo? method = null,
            OperationConfiguration? callConfig = null)
        {
            var provider = ConfigurationProvider.Instance;
            var layers = provider.GetConfigurationLayers(targetType, method, callConfig);
            var effective = provider.GetEffectiveConfiguration(targetType, method, callConfig);

            return new ConfigurationDiagnosticReport(layers, effective);
        }

        /// <summary>
        /// Lists all configuration sources and values.
        /// </summary>
        /// <returns>Configured overrides.</returns>
        public static IReadOnlyList<ConfigurationEntry> ListAllConfigurations()
        {
            return ConfigurationProvider.Instance.GetAllConfigurations();
        }
    }
}
