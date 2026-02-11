using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Pipeline Stage 2: Consumes <see cref="WeatherAnalysisEvent"/> messages from the
    /// <see cref="FakeMessageBus.AnalysisTopic"/> topic, performs additional simulated work
    /// (random delay + string hashing), summarises the analysis, and publishes a
    /// <see cref="WeatherNotificationEvent"/> to the <see cref="FakeMessageBus.NotificationsTopic"/>.
    /// <para>
    /// This stage demonstrates:
    /// <list type="bullet">
    ///   <item>Consuming from one topic and producing to another (fan-out/chaining)</item>
    ///   <item>Correlation ID flowing automatically through all stages</item>
    ///   <item>Simulated work with random delays representing real computation</item>
    ///   <item>Pipeline timing metrics accumulated across stages</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class WeatherAnalyticsProcessor : BackgroundService
    {
        private readonly FakeMessageBus _bus;
        private readonly ILogger<WeatherAnalyticsProcessor> _logger;
        private long _messagesProcessed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherAnalyticsProcessor"/> class.
        /// </summary>
        /// <param name="bus">The message bus to consume from and publish to.</param>
        /// <param name="logger">Logger instance.</param>
        public WeatherAnalyticsProcessor(FakeMessageBus bus, ILogger<WeatherAnalyticsProcessor> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Total messages processed by this stage.</summary>
        public long MessagesProcessed => Interlocked.Read(ref _messagesProcessed);

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "📊 WeatherAnalyticsProcessor started — consuming [{Topic}]",
                FakeMessageBus.AnalysisTopic);

            try
            {
                await _bus.ConsumeAsync(
                    FakeMessageBus.AnalysisTopic,
                    ProcessMessageAsync,
                    stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("WeatherAnalyticsProcessor stopping");
            }
        }

        private async Task ProcessMessageAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope.MessageType != nameof(WeatherAnalysisEvent))
            {
                _logger.LogDebug("Ignoring message type {MessageType}", envelope.MessageType);
                return;
            }

            var analysis = envelope.DeserializePayload<WeatherAnalysisEvent>();
            if (analysis == null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "📥 Stage 2: Processing analysis for {Location} " +
                "(Comfort={Comfort}, Alert={Alert}, Stage1Time={Stage1Time:F1}ms, " +
                "CorrelationId={CorrelationId})",
                analysis.Location, analysis.ComfortClassification,
                analysis.AlertTriggered, analysis.ProcessingTimeMs,
                envelope.CorrelationId);

            // ── Simulated work: random processing delay ──
            var delayMs = Random.Shared.Next(100, 500);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

            // ── Simulated work: compute hash iterations ──
            var hashIterations = Random.Shared.Next(1000, 10000);
            var hashResult = ComputeIteratedHash(analysis.Location, hashIterations);

            _logger.LogDebug(
                "Computed {Iterations} hash iterations (result prefix: {HashPrefix})",
                hashIterations, hashResult.Length > 8 ? hashResult[..8] : hashResult);

            // ── Build notification summary ──
            var summaryBuilder = new StringBuilder();
            summaryBuilder.Append($"{analysis.Location}: {analysis.TemperatureCelsius:F1}°C");

            if (analysis.HeatIndexCelsius.HasValue)
            {
                summaryBuilder.Append($", feels like {analysis.HeatIndexCelsius.Value:F1}°C (heat index)");
            }

            if (analysis.WindChillCelsius.HasValue)
            {
                summaryBuilder.Append($", feels like {analysis.WindChillCelsius.Value:F1}°C (wind chill)");
            }

            summaryBuilder.Append($" — {analysis.ComfortClassification}");

            if (analysis.AlertTriggered)
            {
                summaryBuilder.Append($" ⚠ {analysis.AlertDescription}");
            }

            // ── Determine severity ──
            string severity = "Info";
            if (analysis.AlertTriggered)
            {
                severity = analysis.TemperatureCelsius > 40.0 || analysis.TemperatureCelsius < -20.0
                    ? "Critical"
                    : "Warning";
            }

            sw.Stop();
            var totalPipelineMs = analysis.ProcessingTimeMs + sw.Elapsed.TotalMilliseconds;

            // ── Publish notification to final stage ──
            var notification = new WeatherNotificationEvent
            {
                Location = analysis.Location,
                Summary = summaryBuilder.ToString(),
                Severity = severity,
                HasAlert = analysis.AlertTriggered,
                TotalPipelineTimeMs = totalPipelineMs,
                DispatchedAtUtc = DateTime.UtcNow,
                ObservedAtUtc = analysis.ObservedAtUtc,
            };

            await _bus.PublishAsync(FakeMessageBus.NotificationsTopic, notification, cancellationToken)
                .ConfigureAwait(false);

            Interlocked.Increment(ref _messagesProcessed);

            _logger.LogInformation(
                "📤 Stage 2 → [{Topic}]: {Location} notification ready in {Duration:F1}ms " +
                "(Severity={Severity}, TotalPipeline={TotalPipeline:F1}ms, " +
                "CorrelationId={CorrelationId})",
                FakeMessageBus.NotificationsTopic, analysis.Location,
                sw.Elapsed.TotalMilliseconds, severity,
                totalPipelineMs, envelope.CorrelationId);
        }

        /// <summary>
        /// Performs iterated SHA-256-like hashing to simulate CPU-bound work.
        /// Uses simple string operations to avoid requiring crypto dependencies.
        /// </summary>
        private static string ComputeIteratedHash(string input, int iterations)
        {
            var current = input;
            for (int i = 0; i < iterations; i++)
            {
                // Simple hash computation — not cryptographic, just busy work
                int hash = 17;
                foreach (char c in current)
                {
                    hash = hash * 31 + c;
                }

                current = hash.ToString("X8");
            }

            return current;
        }
    }
}
