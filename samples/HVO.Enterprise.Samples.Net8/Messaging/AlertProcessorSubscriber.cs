using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Pipeline Stage 1: Consumes raw weather observations from the
    /// <see cref="FakeMessageBus.ObservationsTopic"/> topic, evaluates alert thresholds,
    /// computes weather analytics (heat index, wind chill, comfort classification),
    /// performs simulated CPU-bound work (approximating Pi via Leibniz series), and publishes
    /// a <see cref="WeatherAnalysisEvent"/> to the <see cref="FakeMessageBus.AnalysisTopic"/>.
    /// <para>
    /// The correlation ID from the original publisher is restored automatically
    /// by the message bus, so all log entries carry the same correlation context.
    /// </para>
    /// </summary>
    public sealed class AlertProcessorSubscriber : BackgroundService
    {
        private readonly FakeMessageBus _bus;
        private readonly ILogger<AlertProcessorSubscriber> _logger;
        private long _alertsTriggered;
        private long _messagesProcessed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlertProcessorSubscriber"/> class.
        /// </summary>
        /// <param name="bus">The message bus to consume from and publish to.</param>
        /// <param name="logger">Logger instance.</param>
        public AlertProcessorSubscriber(FakeMessageBus bus, ILogger<AlertProcessorSubscriber> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Total alerts triggered by the processor.</summary>
        public long AlertsTriggered => Interlocked.Read(ref _alertsTriggered);

        /// <summary>Total messages processed by this stage.</summary>
        public long MessagesProcessed => Interlocked.Read(ref _messagesProcessed);

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "🔬 AlertProcessorSubscriber started — consuming [{Topic}]",
                FakeMessageBus.ObservationsTopic);

            try
            {
                await _bus.ConsumeAsync(
                    FakeMessageBus.ObservationsTopic,
                    ProcessMessageAsync,
                    stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("AlertProcessorSubscriber stopping");
            }
        }

        private async Task ProcessMessageAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope.MessageType != nameof(WeatherObservationEvent))
            {
                _logger.LogDebug("Ignoring message type {MessageType}", envelope.MessageType);
                return;
            }

            var observation = envelope.DeserializePayload<WeatherObservationEvent>();
            if (observation == null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "📥 Stage 1: Processing observation for {Location} " +
                "(Temp={Temperature}°C, Wind={Wind}km/h, CorrelationId={CorrelationId})",
                observation.Location, observation.TemperatureCelsius,
                observation.WindSpeedKmh, envelope.CorrelationId);

            // ── Simulated CPU-bound work: approximate Pi via Leibniz series ──
            var piIterations = Random.Shared.Next(500, 5000);
            var piResult = ComputePiApproximation(piIterations);

            _logger.LogDebug(
                "Computed {PiIterations} Leibniz iterations (result: {PiPrefix}...)",
                piIterations, piResult.Length > 20 ? piResult[..20] : piResult);

            // ── Random processing delay to simulate real work ──
            var delayMs = Random.Shared.Next(50, 300);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

            // ── Evaluate alert thresholds ──
            bool alertTriggered = false;
            string? alertDescription = null;

            if (observation.TemperatureCelsius > 40.0)
            {
                alertTriggered = true;
                alertDescription = $"🔥 HEAT ALERT: {observation.Location} at {observation.TemperatureCelsius}°C";
                Interlocked.Increment(ref _alertsTriggered);
                _logger.LogWarning(
                    "🔥 HEAT ALERT: {Location} temperature is {Temperature}°C (CorrelationId={CorrelationId})",
                    observation.Location, observation.TemperatureCelsius, envelope.CorrelationId);
            }
            else if (observation.TemperatureCelsius < -20.0)
            {
                alertTriggered = true;
                alertDescription = $"🥶 COLD ALERT: {observation.Location} at {observation.TemperatureCelsius}°C";
                Interlocked.Increment(ref _alertsTriggered);
                _logger.LogWarning(
                    "🥶 COLD ALERT: {Location} temperature is {Temperature}°C (CorrelationId={CorrelationId})",
                    observation.Location, observation.TemperatureCelsius, envelope.CorrelationId);
            }

            if (observation.WindSpeedKmh.HasValue && observation.WindSpeedKmh.Value > 100.0)
            {
                alertTriggered = true;
                var windAlert = $"💨 WIND ALERT: {observation.Location} at {observation.WindSpeedKmh.Value}km/h";
                alertDescription = alertDescription != null ? $"{alertDescription} | {windAlert}" : windAlert;
                Interlocked.Increment(ref _alertsTriggered);
                _logger.LogWarning(
                    "💨 WIND ALERT: {Location} wind speed is {WindSpeed} km/h (CorrelationId={CorrelationId})",
                    observation.Location, observation.WindSpeedKmh.Value, envelope.CorrelationId);
            }

            // ── Compute weather analytics ──
            var heatIndex = ComputeHeatIndex(observation.TemperatureCelsius, observation.Humidity);
            var windChill = ComputeWindChill(observation.TemperatureCelsius, observation.WindSpeedKmh);
            var comfort = ClassifyComfort(observation.TemperatureCelsius, observation.Humidity, observation.WindSpeedKmh);

            sw.Stop();

            // ── Publish analysis results to next stage ──
            var analysis = new WeatherAnalysisEvent
            {
                Location = observation.Location,
                TemperatureCelsius = observation.TemperatureCelsius,
                Humidity = observation.Humidity,
                WindSpeedKmh = observation.WindSpeedKmh,
                HeatIndexCelsius = heatIndex,
                WindChillCelsius = windChill,
                ComfortClassification = comfort,
                AlertTriggered = alertTriggered,
                AlertDescription = alertDescription,
                AnalysedAtUtc = DateTime.UtcNow,
                ProcessingTimeMs = sw.Elapsed.TotalMilliseconds,
                PiIterationsComputed = piIterations,
                ObservedAtUtc = observation.ObservedAtUtc,
            };

            await _bus.PublishAsync(FakeMessageBus.AnalysisTopic, analysis, cancellationToken)
                .ConfigureAwait(false);

            Interlocked.Increment(ref _messagesProcessed);

            _logger.LogInformation(
                "📤 Stage 1 → [{Topic}]: {Location} analysed in {Duration:F1}ms " +
                "(Comfort={Comfort}, Alert={Alert}, PiIterations={PiIterations}, CorrelationId={CorrelationId})",
                FakeMessageBus.AnalysisTopic, observation.Location,
                sw.Elapsed.TotalMilliseconds, comfort, alertTriggered,
                piIterations, envelope.CorrelationId);
        }

        /// <summary>
        /// Approximates Pi using the Leibniz series. This is intentionally
        /// CPU-bound to simulate real processing work in the pipeline.
        /// </summary>
        private static string ComputePiApproximation(int iterations)
        {
            // Leibniz series: π/4 = 1 - 1/3 + 1/5 - 1/7 + ...
            double sum = 0.0;
            for (int i = 0; i < iterations; i++)
            {
                double term = 1.0 / (2.0 * i + 1.0);
                sum += (i % 2 == 0) ? term : -term;
            }

            double pi = 4.0 * sum;
            return pi.ToString("F15");
        }

        /// <summary>
        /// Computes the heat index (apparent temperature considering humidity).
        /// Returns null when conditions don't warrant heat index calculation.
        /// </summary>
        private static double? ComputeHeatIndex(double tempC, double? humidity)
        {
            if (tempC < 27.0 || !humidity.HasValue)
            {
                return null;
            }

            // Simplified Rothfusz regression equation (converted to Celsius)
            double t = tempC * 9.0 / 5.0 + 32.0; // to Fahrenheit
            double r = humidity.Value;

            double hi = -42.379 + 2.04901523 * t + 10.14333127 * r
                - 0.22475541 * t * r - 0.00683783 * t * t
                - 0.05481717 * r * r + 0.00122874 * t * t * r
                + 0.00085282 * t * r * r - 0.00000199 * t * t * r * r;

            return (hi - 32.0) * 5.0 / 9.0; // back to Celsius
        }

        /// <summary>
        /// Computes the wind chill (apparent temperature considering wind speed).
        /// Returns null when conditions don't warrant wind chill calculation.
        /// </summary>
        private static double? ComputeWindChill(double tempC, double? windKmh)
        {
            if (tempC > 10.0 || !windKmh.HasValue || windKmh.Value < 4.8)
            {
                return null;
            }

            // Environment Canada wind chill formula
            double v = Math.Pow(windKmh.Value, 0.16);
            return 13.12 + 0.6215 * tempC - 11.37 * v + 0.3965 * tempC * v;
        }

        /// <summary>
        /// Classifies comfort level based on temperature, humidity, and wind.
        /// </summary>
        private static string ClassifyComfort(double tempC, double? humidity, double? windKmh)
        {
            if (tempC > 40.0) return "Extreme Heat";
            if (tempC > 35.0 && humidity.GetValueOrDefault() > 70) return "Dangerous Heat + Humidity";
            if (tempC > 30.0) return "Hot";
            if (tempC > 20.0) return "Comfortable";
            if (tempC > 10.0) return "Cool";
            if (tempC > 0.0) return "Cold";
            if (tempC > -10.0) return "Very Cold";
            if (windKmh.GetValueOrDefault() > 40) return "Extreme Cold + Wind";
            return "Extreme Cold";
        }
    }
}
