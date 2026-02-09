using System.ComponentModel.DataAnnotations;

namespace HVO.Enterprise.Telemetry.Data.Redis.Configuration
{
    /// <summary>
    /// Configuration options for Redis telemetry instrumentation.
    /// </summary>
    public sealed class RedisTelemetryOptions
    {
        /// <summary>
        /// Whether to record Redis keys in telemetry.
        /// WARNING: Keys may contain PII. Default: <c>true</c>.
        /// </summary>
        public bool RecordKeys { get; set; } = true;

        /// <summary>
        /// Maximum key length to record. Keys exceeding this length are truncated.
        /// Default: 100 characters.
        /// </summary>
        [Range(10, 1000)]
        public int MaxKeyLength { get; set; } = 100;

        /// <summary>
        /// Whether to record the Redis command name (e.g., GET, SET, HGETALL).
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordCommands { get; set; } = true;

        /// <summary>
        /// Whether to record the database index.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordDatabaseIndex { get; set; } = true;

        /// <summary>
        /// Whether to record server endpoint information.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordEndpoint { get; set; } = true;
    }
}
