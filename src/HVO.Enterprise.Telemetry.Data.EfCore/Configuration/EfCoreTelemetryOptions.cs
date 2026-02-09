using System;
using HVO.Enterprise.Telemetry.Data.Configuration;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Configuration
{
    /// <summary>
    /// Configuration options for Entity Framework Core telemetry.
    /// </summary>
    public sealed class EfCoreTelemetryOptions : DataExtensionOptions
    {
        /// <summary>
        /// Whether to capture the database connection string (sanitized) in tags.
        /// Default: <c>false</c>.
        /// </summary>
        public bool RecordConnectionInfo { get; set; }
    }
}
