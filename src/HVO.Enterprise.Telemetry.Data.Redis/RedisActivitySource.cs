using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Data.Redis
{
    /// <summary>
    /// <see cref="ActivitySource"/> for Redis telemetry.
    /// </summary>
    public static class RedisActivitySource
    {
        /// <summary>
        /// The activity source name for Redis operations.
        /// </summary>
        public const string Name = "HVO.Enterprise.Telemetry.Data.Redis";

        /// <summary>
        /// Gets the <see cref="ActivitySource"/> for Redis telemetry.
        /// </summary>
        public static ActivitySource Source { get; } = new ActivitySource(Name, "1.0.0");
    }
}
