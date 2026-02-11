using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Pipeline Stage 3 (final): Consumes <see cref="WeatherNotificationEvent"/> messages
    /// from the <see cref="FakeMessageBus.NotificationsTopic"/> topic and logs the final
    /// notification with full pipeline timing.
    /// <para>
    /// This is the terminal stage in the weather processing pipeline:
    /// <list type="number">
    ///   <item>Collector publishes observation → <c>weather.observations</c></item>
    ///   <item>AlertProcessor analyses + computes Pi → <c>weather.analysis</c></item>
    ///   <item>AnalyticsProcessor summarises + hashes → <c>weather.notifications</c></item>
    ///   <item><b>NotificationDispatch logs final result</b> (this stage)</item>
    /// </list>
    /// The same correlation ID flows through all four stages, visible in every log entry.
    /// </para>
    /// </summary>
    public sealed class NotificationDispatchSubscriber : BackgroundService
    {
        private readonly FakeMessageBus _bus;
        private readonly ILogger<NotificationDispatchSubscriber> _logger;
        private long _notificationsDispatched;
        private long _criticalNotifications;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDispatchSubscriber"/> class.
        /// </summary>
        /// <param name="bus">The message bus to consume from.</param>
        /// <param name="logger">Logger instance.</param>
        public NotificationDispatchSubscriber(FakeMessageBus bus, ILogger<NotificationDispatchSubscriber> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Total notifications dispatched.</summary>
        public long NotificationsDispatched => Interlocked.Read(ref _notificationsDispatched);

        /// <summary>Total critical severity notifications.</summary>
        public long CriticalNotifications => Interlocked.Read(ref _criticalNotifications);

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "📬 NotificationDispatchSubscriber started — consuming [{Topic}]",
                FakeMessageBus.NotificationsTopic);

            try
            {
                await _bus.ConsumeAsync(
                    FakeMessageBus.NotificationsTopic,
                    ProcessMessageAsync,
                    stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("NotificationDispatchSubscriber stopping");
            }
        }

        private async Task ProcessMessageAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope.MessageType != nameof(WeatherNotificationEvent))
            {
                _logger.LogDebug("Ignoring message type {MessageType}", envelope.MessageType);
                return;
            }

            var notification = envelope.DeserializePayload<WeatherNotificationEvent>();
            if (notification == null)
            {
                return;
            }

            // Simulate dispatch latency (e.g. email/SMS/webhook delivery)
            var dispatchDelayMs = Random.Shared.Next(20, 100);
            await Task.Delay(dispatchDelayMs, cancellationToken).ConfigureAwait(false);

            Interlocked.Increment(ref _notificationsDispatched);

            var pipelineAge = (DateTime.UtcNow - notification.ObservedAtUtc).TotalMilliseconds;

            if (notification.Severity == "Critical")
            {
                Interlocked.Increment(ref _criticalNotifications);
                _logger.LogWarning(
                    "🚨 Stage 3 DISPATCH [{Severity}]: {Summary} " +
                    "(PipelineTime={PipelineTime:F1}ms, WallClock={WallClock:F0}ms, " +
                    "CorrelationId={CorrelationId})",
                    notification.Severity, notification.Summary,
                    notification.TotalPipelineTimeMs, pipelineAge,
                    envelope.CorrelationId);
            }
            else if (notification.HasAlert)
            {
                _logger.LogWarning(
                    "⚠️ Stage 3 DISPATCH [{Severity}]: {Summary} " +
                    "(PipelineTime={PipelineTime:F1}ms, WallClock={WallClock:F0}ms, " +
                    "CorrelationId={CorrelationId})",
                    notification.Severity, notification.Summary,
                    notification.TotalPipelineTimeMs, pipelineAge,
                    envelope.CorrelationId);
            }
            else
            {
                _logger.LogInformation(
                    "✅ Stage 3 DISPATCH [{Severity}]: {Summary} " +
                    "(PipelineTime={PipelineTime:F1}ms, WallClock={WallClock:F0}ms, " +
                    "CorrelationId={CorrelationId})",
                    notification.Severity, notification.Summary,
                    notification.TotalPipelineTimeMs, pipelineAge,
                    envelope.CorrelationId);
            }
        }
    }
}
