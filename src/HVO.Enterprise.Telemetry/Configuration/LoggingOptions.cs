using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Logging configuration.
    /// </summary>
    public sealed class LoggingOptions
    {
        /// <summary>
        /// Gets or sets whether correlation enrichment is enabled.
        /// </summary>
        public bool EnableCorrelationEnrichment { get; set; } = true;

        /// <summary>
        /// Gets or sets minimum log level by category.
        /// </summary>
        public Dictionary<string, string> MinimumLevel { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
