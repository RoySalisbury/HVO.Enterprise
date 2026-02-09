using System;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Exceptions;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Core telemetry service implementation providing unified observability.
    /// Orchestrates existing subsystems (operation scopes, statistics, exception tracking)
    /// and supports both DI-based and static initialization patterns.
    /// </summary>
    public sealed class TelemetryService : ITelemetryService, IDisposable
    {
        private readonly TelemetryOptions _options;
        private readonly ILogger<TelemetryService> _logger;
        private readonly TelemetryStatistics _statistics;
        private readonly IOperationScopeFactory _operationScopeFactory;
        private volatile bool _isStarted;
        private volatile bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryService"/> class for DI-based initialization.
        /// </summary>
        /// <param name="options">Telemetry configuration options.</param>
        /// <param name="statistics">Statistics tracker.</param>
        /// <param name="operationScopeFactory">Factory for creating operation scopes.</param>
        /// <param name="logger">Logger for internal diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public TelemetryService(
            IOptions<TelemetryOptions> options,
            ITelemetryStatistics statistics,
            IOperationScopeFactory operationScopeFactory,
            ILogger<TelemetryService> logger)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options.Value ?? throw new ArgumentNullException(nameof(options), "Options.Value is null.");
            _statistics = statistics as TelemetryStatistics ?? throw new ArgumentException(
                "Statistics must be a TelemetryStatistics instance.", nameof(statistics));
            _operationScopeFactory = operationScopeFactory ?? throw new ArgumentNullException(nameof(operationScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryService"/> class for static initialization.
        /// Creates internal subsystem instances directly (no DI container required).
        /// </summary>
        /// <param name="options">Telemetry configuration options.</param>
        /// <param name="loggerFactory">Logger factory for internal diagnostics. Uses <see cref="NullLoggerFactory"/> if null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        internal TelemetryService(
            TelemetryOptions options,
            ILoggerFactory? loggerFactory = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
            var factory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = factory.CreateLogger<TelemetryService>();
            _statistics = new TelemetryStatistics();

            // Determine activity source name from options
            var sourceName = _options.ActivitySources != null && _options.ActivitySources.Count > 0
                ? _options.ActivitySources[0]
                : "HVO.Enterprise.Telemetry";

            _operationScopeFactory = new OperationScopeFactory(
                sourceName,
                _options.ServiceVersion,
                factory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryService"/> class with all dependencies explicitly provided.
        /// </summary>
        /// <param name="options">Telemetry configuration options.</param>
        /// <param name="statistics">Statistics tracker.</param>
        /// <param name="operationScopeFactory">Factory for creating operation scopes.</param>
        /// <param name="logger">Logger for internal diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        internal TelemetryService(
            TelemetryOptions options,
            TelemetryStatistics statistics,
            IOperationScopeFactory operationScopeFactory,
            ILogger<TelemetryService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            _operationScopeFactory = operationScopeFactory ?? throw new ArgumentNullException(nameof(operationScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool IsEnabled => _options.Enabled && !_isDisposed;

        /// <inheritdoc />
        public ITelemetryStatistics Statistics => _statistics;

        /// <summary>
        /// Gets the internal statistics instance for direct access by the telemetry system.
        /// </summary>
        internal TelemetryStatistics InternalStatistics => _statistics;

        /// <inheritdoc />
        public void Start()
        {
            if (_isStarted)
                return;

            _logger.LogInformation(
                "Starting telemetry service: {ServiceName} v{Version} ({Environment})",
                _options.ServiceName,
                _options.ServiceVersion ?? "unknown",
                _options.Environment ?? "unknown");

            _isStarted = true;
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!_isStarted || _isDisposed)
                return;

            _logger.LogInformation("Shutting down telemetry service: {ServiceName}", _options.ServiceName);

            _isStarted = false;
        }

        /// <inheritdoc />
        public IOperationScope StartOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));

            if (!IsEnabled)
                return new NoOpOperationScope(operationName);

            _statistics.IncrementActivitiesCreated(operationName);
            return _operationScopeFactory.Begin(operationName);
        }

        /// <inheritdoc />
        public void TrackException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (!IsEnabled)
                return;

            exception.RecordException();
            _statistics.IncrementExceptionsTracked();
        }

        /// <inheritdoc />
        public void TrackEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));

            if (!IsEnabled)
                return;

            _statistics.IncrementEventsRecorded();

            _logger.LogDebug("Event tracked: {EventName}", eventName);
        }

        /// <inheritdoc />
        public void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(metricName));

            if (!IsEnabled)
                return;

            _statistics.IncrementMetricsRecorded();

            _logger.LogDebug("Metric recorded: {MetricName} = {Value}", metricName, value);
        }

        /// <summary>
        /// Disposes the telemetry service, shutting down if still running.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Shutdown();
            _isDisposed = true;
        }
    }
}
