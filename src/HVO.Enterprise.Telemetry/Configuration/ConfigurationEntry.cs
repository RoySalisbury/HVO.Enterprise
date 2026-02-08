using System;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Represents a configured override entry.
    /// </summary>
    public sealed class ConfigurationEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationEntry"/> class.
        /// </summary>
        /// <param name="level">Configuration level.</param>
        /// <param name="source">Configuration source.</param>
        /// <param name="identifier">Identifier (namespace/type/method).</param>
        /// <param name="configuration">Configuration override.</param>
        public ConfigurationEntry(
            ConfigurationLevel level,
            ConfigurationSourceKind source,
            string? identifier,
            OperationConfiguration configuration)
        {
            Level = level;
            Source = source;
            Identifier = identifier;
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets the configuration level.
        /// </summary>
        public ConfigurationLevel Level { get; }

        /// <summary>
        /// Gets the configuration source.
        /// </summary>
        public ConfigurationSourceKind Source { get; }

        /// <summary>
        /// Gets the identifier for the configuration.
        /// </summary>
        public string? Identifier { get; }

        /// <summary>
        /// Gets the configuration override.
        /// </summary>
        public OperationConfiguration Configuration { get; }
    }
}
