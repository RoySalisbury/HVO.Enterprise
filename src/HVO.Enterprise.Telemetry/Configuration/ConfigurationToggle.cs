namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Represents an enabled/disabled toggle with inheritance support.
    /// </summary>
    public enum ConfigurationToggle
    {
        /// <summary>
        /// Inherit from the parent configuration.
        /// </summary>
        Inherit = 0,

        /// <summary>
        /// Explicitly enabled.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Explicitly disabled.
        /// </summary>
        Disabled = 2
    }
}
