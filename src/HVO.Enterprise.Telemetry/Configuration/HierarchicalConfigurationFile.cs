using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Represents a hierarchical configuration payload loaded from JSON.
    /// </summary>
    public sealed class HierarchicalConfigurationFile
    {
        /// <summary>
        /// Gets or sets global configuration overrides.
        /// </summary>
        public OperationConfiguration? Global { get; set; }

        /// <summary>
        /// Gets or sets namespace-specific configuration overrides.
        /// </summary>
        public Dictionary<string, OperationConfiguration> Namespaces { get; set; } =
            new Dictionary<string, OperationConfiguration>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets type-specific configuration overrides.
        /// </summary>
        public Dictionary<string, OperationConfiguration> Types { get; set; } =
            new Dictionary<string, OperationConfiguration>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets method-specific configuration overrides.
        /// </summary>
        public Dictionary<string, OperationConfiguration> Methods { get; set; } =
            new Dictionary<string, OperationConfiguration>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Validates configuration values.
        /// </summary>
        public void Validate()
        {
            Global?.Validate();

            // Normalize null collections to empty dictionaries
            Namespaces ??= new Dictionary<string, OperationConfiguration>(StringComparer.OrdinalIgnoreCase);
            Types ??= new Dictionary<string, OperationConfiguration>(StringComparer.OrdinalIgnoreCase);
            Methods ??= new Dictionary<string, OperationConfiguration>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in Namespaces)
            {
                kvp.Value.Validate();
            }

            foreach (var kvp in Types)
            {
                kvp.Value.Validate();
            }

            foreach (var kvp in Methods)
            {
                kvp.Value.Validate();
            }
        }
    }
}
