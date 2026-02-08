using System;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Event arguments for configuration change notifications.
    /// </summary>
    public sealed class ConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldConfiguration">Previous configuration.</param>
        /// <param name="newConfiguration">New configuration.</param>
        public ConfigurationChangedEventArgs(TelemetryOptions oldConfiguration, TelemetryOptions newConfiguration)
        {
            OldConfiguration = oldConfiguration ?? throw new ArgumentNullException(nameof(oldConfiguration));
            NewConfiguration = newConfiguration ?? throw new ArgumentNullException(nameof(newConfiguration));
        }

        /// <summary>
        /// Gets the previous configuration.
        /// </summary>
        public TelemetryOptions OldConfiguration { get; }

        /// <summary>
        /// Gets the new configuration.
        /// </summary>
        public TelemetryOptions NewConfiguration { get; }
    }
}
