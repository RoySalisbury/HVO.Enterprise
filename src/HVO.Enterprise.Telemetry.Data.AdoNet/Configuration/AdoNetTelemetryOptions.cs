using HVO.Enterprise.Telemetry.Data.Configuration;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Configuration
{
    /// <summary>
    /// Configuration options for ADO.NET telemetry instrumentation.
    /// </summary>
    public sealed class AdoNetTelemetryOptions : DataExtensionOptions
    {
        /// <summary>
        /// Whether to capture the database connection string (sanitized) in tags.
        /// Default: <c>false</c>.
        /// </summary>
        public bool RecordConnectionInfo { get; set; }
    }
}
