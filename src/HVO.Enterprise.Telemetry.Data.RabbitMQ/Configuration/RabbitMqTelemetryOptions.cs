using System.ComponentModel.DataAnnotations;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for RabbitMQ telemetry instrumentation.
    /// </summary>
    public sealed class RabbitMqTelemetryOptions
    {
        /// <summary>
        /// Whether to propagate W3C TraceContext in message headers.
        /// Default: <c>true</c>.
        /// </summary>
        public bool PropagateTraceContext { get; set; } = true;

        /// <summary>
        /// Whether to record the exchange name in activity tags.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordExchange { get; set; } = true;

        /// <summary>
        /// Whether to record the routing key in activity tags.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordRoutingKey { get; set; } = true;

        /// <summary>
        /// Whether to record message body size in activity tags.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordBodySize { get; set; } = true;

        /// <summary>
        /// Whether to record the message ID and correlation ID.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordMessageIds { get; set; } = true;

        /// <summary>
        /// Whether to record the queue name on consume operations.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordQueueName { get; set; } = true;
    }
}
