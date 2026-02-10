using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Bridge between HVO telemetry and Application Insights with dual-mode support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports two modes:
    /// </para>
    /// <list type="number">
    /// <item><description><b>OTLP Mode</b>: OpenTelemetry SDK exports to Application Insights via OTLP exporter.
    /// In this mode, track methods are no-ops since OpenTelemetry handles export.</description></item>
    /// <item><description><b>Direct Mode</b>: Direct ApplicationInsights SDK integration for .NET Framework 4.8.
    /// In this mode, telemetry is explicitly sent via <see cref="TelemetryClient"/>.</description></item>
    /// </list>
    /// <para>
    /// Mode is auto-detected based on environment variables (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>)
    /// unless explicitly overridden via <see cref="AppInsightsOptions.ForceOtlpMode"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var bridge = new ApplicationInsightsBridge(telemetryClient);
    /// bridge.TrackRequest("ProcessOrder", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), "200", true);
    /// bridge.Flush();
    /// </code>
    /// </example>
    public sealed class ApplicationInsightsBridge : IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<ApplicationInsightsBridge>? _logger;
        private readonly bool _isOtlpMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsBridge"/> class.
        /// </summary>
        /// <param name="telemetryClient">Application Insights telemetry client.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <param name="forceOtlpMode">
        /// Optional flag to force a specific mode. When <see langword="null"/>, the mode is auto-detected.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetryClient"/> is null.</exception>
        public ApplicationInsightsBridge(
            TelemetryClient telemetryClient,
            ILogger<ApplicationInsightsBridge>? logger = null,
            bool? forceOtlpMode = null)
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            _telemetryClient = telemetryClient;
            _logger = logger;
            _isOtlpMode = forceOtlpMode ?? DetectOtlpMode();

            _logger?.LogInformation(
                "ApplicationInsightsBridge initialized in {Mode} mode",
                _isOtlpMode ? "OTLP" : "Direct");
        }

        /// <summary>
        /// Gets a value indicating whether OTLP mode is active.
        /// </summary>
        /// <remarks>
        /// In OTLP mode, track methods are no-ops and OpenTelemetry SDK handles export.
        /// In Direct mode, telemetry is explicitly sent via <see cref="TelemetryClient"/>.
        /// </remarks>
        public bool IsOtlpMode => _isOtlpMode;

        /// <summary>
        /// Tracks a request (incoming operation).
        /// </summary>
        /// <remarks>
        /// In OTLP mode, this is a no-op (OpenTelemetry handles export).
        /// In Direct mode, explicitly sends request telemetry to Application Insights.
        /// </remarks>
        /// <param name="name">The operation name.</param>
        /// <param name="timestamp">The operation start time.</param>
        /// <param name="duration">The operation duration.</param>
        /// <param name="responseCode">The response code (e.g., "200", "500").</param>
        /// <param name="success">Whether the operation succeeded.</param>
        public void TrackRequest(
            string name,
            DateTimeOffset timestamp,
            TimeSpan duration,
            string responseCode,
            bool success)
        {
            if (_isOtlpMode)
            {
                return;
            }

            var request = new RequestTelemetry
            {
                Name = name,
                Timestamp = timestamp,
                Duration = duration,
                ResponseCode = responseCode,
                Success = success
            };

            EnrichFromContext(request);
            _telemetryClient.TrackRequest(request);
        }

        /// <summary>
        /// Tracks a dependency (outgoing operation).
        /// </summary>
        /// <remarks>
        /// In OTLP mode, this is a no-op (OpenTelemetry handles export).
        /// In Direct mode, explicitly sends dependency telemetry to Application Insights.
        /// </remarks>
        /// <param name="dependencyType">The dependency type (e.g., "SQL", "HTTP").</param>
        /// <param name="target">The target of the dependency.</param>
        /// <param name="name">The dependency name.</param>
        /// <param name="data">The dependency data/command.</param>
        /// <param name="timestamp">The operation start time.</param>
        /// <param name="duration">The operation duration.</param>
        /// <param name="resultCode">The result code.</param>
        /// <param name="success">Whether the operation succeeded.</param>
        public void TrackDependency(
            string dependencyType,
            string target,
            string name,
            string data,
            DateTimeOffset timestamp,
            TimeSpan duration,
            string resultCode,
            bool success)
        {
            if (_isOtlpMode)
            {
                return;
            }

            var dependency = new DependencyTelemetry
            {
                Type = dependencyType,
                Target = target,
                Name = name,
                Data = data,
                Timestamp = timestamp,
                Duration = duration,
                ResultCode = resultCode,
                Success = success
            };

            EnrichFromContext(dependency);
            _telemetryClient.TrackDependency(dependency);
        }

        /// <summary>
        /// Tracks a metric value.
        /// </summary>
        /// <remarks>
        /// In OTLP mode, this is a no-op (OpenTelemetry handles export).
        /// In Direct mode, explicitly sends metric telemetry to Application Insights.
        /// </remarks>
        /// <param name="name">The metric name.</param>
        /// <param name="value">The metric value.</param>
        public void TrackMetric(string name, double value)
        {
            if (_isOtlpMode)
            {
                return;
            }

            _telemetryClient.TrackMetric(name, value);
        }

        /// <summary>
        /// Tracks an exception.
        /// </summary>
        /// <remarks>
        /// In OTLP mode, this is a no-op (OpenTelemetry handles export).
        /// In Direct mode, explicitly sends exception telemetry to Application Insights.
        /// </remarks>
        /// <param name="exception">The exception to track.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public void TrackException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (_isOtlpMode)
            {
                return;
            }

            var telemetry = new ExceptionTelemetry(exception);
            EnrichFromContext(telemetry);
            _telemetryClient.TrackException(telemetry);
        }

        /// <summary>
        /// Flushes buffered telemetry to Application Insights.
        /// </summary>
        /// <remarks>
        /// Call this during shutdown to ensure all buffered telemetry is sent.
        /// Use sparingly in normal operation as it can be expensive.
        /// </remarks>
        public void Flush()
        {
            _telemetryClient.Flush();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Flush();
        }

        /// <summary>
        /// Detects whether OTLP mode should be used based on environment configuration.
        /// </summary>
        /// <returns><see langword="true"/> if OTLP endpoint is configured; otherwise <see langword="false"/>.</returns>
        internal static bool DetectOtlpMode()
        {
            var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            return !string.IsNullOrEmpty(otlpEndpoint);
        }

        /// <summary>
        /// Enriches telemetry with correlation context and Activity tags.
        /// </summary>
        private static void EnrichFromContext(ISupportProperties telemetry)
        {
            // Add correlation ID
            if (CorrelationContext.TryGetExplicitCorrelationId(out var correlationId) &&
                !string.IsNullOrEmpty(correlationId))
            {
                if (!telemetry.Properties.ContainsKey("CorrelationId"))
                {
                    telemetry.Properties["CorrelationId"] = correlationId;
                }
            }

            // Add Activity context
            var activity = Activity.Current;
            if (activity != null)
            {
                foreach (var tag in activity.Tags)
                {
                    if (!string.IsNullOrEmpty(tag.Key) &&
                        !telemetry.Properties.ContainsKey(tag.Key))
                    {
                        telemetry.Properties[tag.Key] = tag.Value ?? string.Empty;
                    }
                }
            }
        }
    }
}
