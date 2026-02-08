namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Identifies the source of configuration values.
    /// </summary>
    public enum ConfigurationSourceKind
    {
        /// <summary>
        /// Built-in defaults.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Code-based configuration (attributes, fluent API).
        /// </summary>
        Code = 1,

        /// <summary>
        /// File-based configuration.
        /// </summary>
        File = 2,

        /// <summary>
        /// Runtime configuration (API calls).
        /// </summary>
        Runtime = 3
    }
}
