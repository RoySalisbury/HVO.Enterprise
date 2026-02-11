using System;
using System.Text.Json;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Envelope for messages flowing through the <see cref="FakeMessageBus"/>.
    /// Carries correlation context alongside the payload.
    /// </summary>
    public sealed class MessageEnvelope
    {
        /// <summary>
        /// Gets or sets the message type name.
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the topic/queue this message was published to.
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON-serialised payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the correlation ID that was active when the message was published.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the W3C trace-parent header for distributed tracing.
        /// </summary>
        public string? TraceParent { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the message was published.
        /// </summary>
        public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Deserialises the payload to the specified type.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <returns>The deserialised object.</returns>
        public T? DeserializePayload<T>()
        {
            return JsonSerializer.Deserialize<T>(Payload);
        }
    }
}
