using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Background service that consumes weather observation messages and evaluates
    /// alert thresholds. Demonstrates message consumption with correlation propagation.
    /// </summary>
    public sealed class AlertProcessorSubscriber : BackgroundService
    {
        private readonly FakeMessageBus _bus;
        private readonly ILogger<AlertProcessorSubscriber> _logger;
        private long _alertsTriggered;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlertProcessorSubscriber"/> class.
        /// </summary>
        /// <param name="bus">The message bus to consume from.</param>
        /// <param name="logger">Logger instance.</param>
        public AlertProcessorSubscriber(FakeMessageBus bus, ILogger<AlertProcessorSubscriber> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Total alerts triggered by the processor.</summary>
        public long AlertsTriggered => Interlocked.Read(ref _alertsTriggered);

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertProcessorSubscriber started — consuming weather observations");

            try
            {
                await _bus.ConsumeAsync(ProcessMessageAsync, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("AlertProcessorSubscriber stopping");
            }
        }

        private Task ProcessMessageAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope.MessageType != nameof(WeatherObservationEvent))
            {
                _logger.LogDebug("Ignoring message type {MessageType}", envelope.MessageType);
                return Task.CompletedTask;
            }

            var observation = envelope.DeserializePayload<WeatherObservationEvent>();
            if (observation == null)
            {
                return Task.CompletedTask;
            }

            // Evaluate alert thresholds
            if (observation.TemperatureCelsius > 40.0)
            {
                Interlocked.Increment(ref _alertsTriggered);
                _logger.LogWarning(
                    "🔥 HEAT ALERT: {Location} temperature is {Temperature}°C (CorrelationId={CorrelationId})",
                    observation.Location, observation.TemperatureCelsius, envelope.CorrelationId);
            }
            else if (observation.TemperatureCelsius < -20.0)
            {
                Interlocked.Increment(ref _alertsTriggered);
                _logger.LogWarning(
                    "🥶 COLD ALERT: {Location} temperature is {Temperature}°C (CorrelationId={CorrelationId})",
                    observation.Location, observation.TemperatureCelsius, envelope.CorrelationId);
            }

            if (observation.WindSpeedKmh.HasValue && observation.WindSpeedKmh.Value > 100.0)
            {
                Interlocked.Increment(ref _alertsTriggered);
                _logger.LogWarning(
                    "💨 WIND ALERT: {Location} wind speed is {WindSpeed} km/h (CorrelationId={CorrelationId})",
                    observation.Location, observation.WindSpeedKmh.Value, envelope.CorrelationId);
            }

            return Task.CompletedTask;
        }
    }
}
