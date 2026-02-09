using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ
{
    /// <summary>
    /// <see cref="ActivitySource"/> for RabbitMQ telemetry.
    /// </summary>
    public static class RabbitMqActivitySource
    {
        /// <summary>
        /// The activity source name for RabbitMQ operations.
        /// </summary>
        public const string Name = "HVO.Enterprise.Telemetry.Data.RabbitMQ";

        /// <summary>
        /// Gets the <see cref="ActivitySource"/> for RabbitMQ telemetry.
        /// </summary>
        public static ActivitySource Source { get; } = new ActivitySource(Name, "1.0.0");
    }
}
