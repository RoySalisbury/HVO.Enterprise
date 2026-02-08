namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Identifies a configuration precedence level.
    /// </summary>
    public enum ConfigurationLevel
    {
        /// <summary>
        /// Built-in defaults.
        /// </summary>
        GlobalDefault = 0,

        /// <summary>
        /// Global overrides.
        /// </summary>
        Global = 1,

        /// <summary>
        /// Namespace overrides.
        /// </summary>
        Namespace = 2,

        /// <summary>
        /// Type overrides.
        /// </summary>
        Type = 3,

        /// <summary>
        /// Method overrides.
        /// </summary>
        Method = 4,

        /// <summary>
        /// Call-specific overrides.
        /// </summary>
        Call = 5
    }
}
