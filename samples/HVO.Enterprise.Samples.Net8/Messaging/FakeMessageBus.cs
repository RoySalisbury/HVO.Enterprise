using System;
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
    /// In-process message bus backed by <see cref="System.Threading.Channels.Channel{T}"/>.
    /// Simulates RabbitMQ publish/consume semantics with correlation propagation.
    /// When a real RabbitMQ server is available (via Docker), this can be replaced
    /// by the actual RabbitMQ connection + HVO telemetry wrapper.
    /// </summary>
    public sealed class FakeMessageBus : IDisposable
    {
        private readonly Channel<MessageEnvelope> _channel;
        private readonly ILogger<FakeMessageBus> _logger;
        private long _published;
        private long _consumed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeMessageBus"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="capacity">Bounded channel capacity. Default: 1000.</param>
        public FakeMessageBus(ILogger<FakeMessageBus> logger, int capacity = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channel = Channel.CreateBounded<MessageEnvelope>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            });
        }

        /// <summary>Total messages published.</summary>
        public long Published => Interlocked.Read(ref _published);

        /// <summary>Total messages consumed.</summary>
        public long Consumed => Interlocked.Read(ref _consumed);

        /// <summary>
        /// Publishes a message to the bus with correlation context propagation.
        /// Simulates a small network delay (10-30ms) to exercise timeout/cancellation paths.
        /// </summary>
        /// <typeparam name="T">Message payload type.</typeparam>
        /// <param name="message">The message payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            var envelope = new MessageEnvelope
            {
                MessageType = typeof(T).Name,
                Payload = JsonSerializer.Serialize(message),
                CorrelationId = CorrelationContext.Current,
                TraceParent = Activity.Current?.Id,
                PublishedAtUtc = DateTime.UtcNow,
            };

            // Simulate realistic network latency
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(10, 30)), cancellationToken)
                .ConfigureAwait(false);

            await _channel.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _published);

            _logger.LogDebug(
                "Published {MessageType} (CorrelationId={CorrelationId})",
                envelope.MessageType, envelope.CorrelationId);
        }

        /// <summary>
        /// Reads messages from the bus asynchronously. Loops until cancellation.
        /// </summary>
        /// <param name="handler">Handler invoked for each received message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task ConsumeAsync(
            Func<MessageEnvelope, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            await foreach (var envelope in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    // Simulate processing latency
                    await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(5, 15)), cancellationToken)
                        .ConfigureAwait(false);

                    await handler(envelope, cancellationToken).ConfigureAwait(false);
                    Interlocked.Increment(ref _consumed);

                    _logger.LogDebug(
                        "Consumed {MessageType} (CorrelationId={CorrelationId})",
                        envelope.MessageType, envelope.CorrelationId);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error consuming {MessageType} (CorrelationId={CorrelationId})",
                        envelope.MessageType, envelope.CorrelationId);
                }
            }
        }

        /// <summary>
        /// Signals that no more messages will be written.
        /// </summary>
        public void Complete()
        {
            _channel.Writer.TryComplete();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Complete();
        }
    }
}
