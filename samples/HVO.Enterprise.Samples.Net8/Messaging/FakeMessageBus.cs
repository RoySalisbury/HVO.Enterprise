using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// In-process message bus backed by <see cref="System.Threading.Channels.Channel{T}"/>
    /// with named topic/queue support. Simulates RabbitMQ publish/consume semantics
    /// with correlation propagation across multi-hop processing pipelines.
    /// When a real RabbitMQ server is available (via Docker), this can be replaced
    /// by the actual RabbitMQ connection + HVO telemetry wrapper.
    /// </summary>
    public sealed class FakeMessageBus : IDisposable
    {
        private readonly ConcurrentDictionary<string, Channel<MessageEnvelope>> _topics = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<FakeMessageBus> _logger;
        private readonly int _capacity;
        private long _published;
        private long _consumed;

        /// <summary>Default topic name when none is specified.</summary>
        public const string DefaultTopic = "default";

        /// <summary>Topic for raw weather observations from the collector.</summary>
        public const string ObservationsTopic = "weather.observations";

        /// <summary>Topic for analysed weather events (post-processing).</summary>
        public const string AnalysisTopic = "weather.analysis";

        /// <summary>Topic for final notification dispatch.</summary>
        public const string NotificationsTopic = "weather.notifications";

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeMessageBus"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="capacity">Bounded channel capacity per topic. Default: 1000.</param>
        public FakeMessageBus(ILogger<FakeMessageBus> logger, int capacity = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _capacity = capacity;
        }

        /// <summary>Total messages published across all topics.</summary>
        public long Published => Interlocked.Read(ref _published);

        /// <summary>Total messages consumed across all topics.</summary>
        public long Consumed => Interlocked.Read(ref _consumed);

        /// <summary>
        /// Publishes a message to the default topic with correlation context propagation.
        /// </summary>
        /// <typeparam name="T">Message payload type.</typeparam>
        /// <param name="message">The message payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            return PublishAsync(DefaultTopic, message, cancellationToken);
        }

        /// <summary>
        /// Publishes a message to a named topic with correlation context propagation.
        /// Simulates a small network delay (10-30ms) to exercise timeout/cancellation paths.
        /// </summary>
        /// <typeparam name="T">Message payload type.</typeparam>
        /// <param name="topic">The target topic/queue name.</param>
        /// <param name="message">The message payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        {
            var envelope = new MessageEnvelope
            {
                MessageType = typeof(T).Name,
                Topic = topic,
                Payload = JsonSerializer.Serialize(message),
                CorrelationId = CorrelationContext.Current,
                TraceParent = Activity.Current?.Id,
                PublishedAtUtc = DateTime.UtcNow,
            };

            // Simulate realistic network latency
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(10, 30)), cancellationToken)
                .ConfigureAwait(false);

            var channel = GetOrCreateChannel(topic);
            await channel.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _published);

            _logger.LogDebug(
                "Published {MessageType} → [{Topic}] (CorrelationId={CorrelationId})",
                envelope.MessageType, topic, envelope.CorrelationId);
        }

        /// <summary>
        /// Reads messages from the default topic asynchronously. Loops until cancellation.
        /// </summary>
        /// <param name="handler">Handler invoked for each received message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task ConsumeAsync(
            Func<MessageEnvelope, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            return ConsumeAsync(DefaultTopic, handler, cancellationToken);
        }

        /// <summary>
        /// Reads messages from a named topic asynchronously. Loops until cancellation.
        /// Restores the correlation context from the message envelope so downstream
        /// logs and telemetry carry the original correlation ID.
        /// </summary>
        /// <param name="topic">The topic/queue to consume from.</param>
        /// <param name="handler">Handler invoked for each received message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task ConsumeAsync(
            string topic,
            Func<MessageEnvelope, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            var channel = GetOrCreateChannel(topic);

            await foreach (var envelope in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    // Restore the correlation context from the message so all
                    // downstream activity carries the original correlation ID
                    using var correlationScope = !string.IsNullOrEmpty(envelope.CorrelationId)
                        ? CorrelationContext.BeginScope(envelope.CorrelationId)
                        : null;

                    await handler(envelope, cancellationToken).ConfigureAwait(false);
                    Interlocked.Increment(ref _consumed);

                    _logger.LogDebug(
                        "Consumed {MessageType} ← [{Topic}] (CorrelationId={CorrelationId})",
                        envelope.MessageType, topic, envelope.CorrelationId);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error consuming {MessageType} ← [{Topic}] (CorrelationId={CorrelationId})",
                        envelope.MessageType, topic, envelope.CorrelationId);
                }
            }
        }

        /// <summary>
        /// Signals that no more messages will be written across all topics.
        /// </summary>
        public void Complete()
        {
            foreach (var channel in _topics.Values)
            {
                channel.Writer.TryComplete();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Complete();
        }

        private Channel<MessageEnvelope> GetOrCreateChannel(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Topic name must not be null or whitespace.", nameof(topic));
            }

            return _topics.GetOrAdd(topic, _ => Channel.CreateBounded<MessageEnvelope>(
                new BoundedChannelOptions(_capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = false,
                }));
        }
    }
}
