using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Static entry point for telemetry operations. Supports both DI and static initialization.
    /// For DI mode, use the <c>AddTelemetry()</c> extension method on <c>IServiceCollection</c> instead.
    /// For non-DI mode, call <see cref="Initialize()"/> at application startup.
    /// </summary>
    public static class Telemetry
    {
        private static readonly object _lock = new object();
        private static TelemetryService? _instance;
        private static volatile bool _isInitialized;

        /// <summary>
        /// Gets whether telemetry has been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the current telemetry statistics.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when telemetry has not been initialized.
        /// </exception>
        public static ITelemetryStatistics Statistics
        {
            get
            {
                EnsureInitialized();
                return _instance!.Statistics;
            }
        }

        /// <summary>
        /// Gets the current correlation ID for the execution context.
        /// Returns null if no correlation ID has been set.
        /// </summary>
        public static string? CurrentCorrelationId
        {
            get
            {
                var raw = CorrelationContext.GetRawValue();
                return raw;
            }
        }

        /// <summary>
        /// Gets the current <see cref="Activity"/> (distributed trace span).
        /// </summary>
        public static Activity? CurrentActivity => Activity.Current;

        /// <summary>
        /// Initializes telemetry with default options.
        /// </summary>
        /// <returns>True if initialized successfully, false if already initialized.</returns>
        public static bool Initialize()
        {
            return Initialize(new TelemetryOptions());
        }

        /// <summary>
        /// Initializes telemetry with specified options.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        /// <returns>True if initialized successfully, false if already initialized.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when options validation fails.</exception>
        public static bool Initialize(TelemetryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return Initialize(options, NullLoggerFactory.Instance);
        }

        /// <summary>
        /// Initializes telemetry with options and a logger factory.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        /// <param name="loggerFactory">Logger factory for internal logging.</param>
        /// <returns>True if initialized successfully, false if already initialized.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when options validation fails.</exception>
        public static bool Initialize(TelemetryOptions options, ILoggerFactory loggerFactory)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            lock (_lock)
            {
                if (_isInitialized)
                    return false;

                options.Validate();

                _instance = new TelemetryService(options, loggerFactory);
                _instance.Start();

                // Register shutdown hooks for graceful cleanup
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

                _isInitialized = true;
                return true;
            }
        }

        /// <summary>
        /// Shuts down telemetry and flushes pending data.
        /// Safe to call multiple times or when not initialized.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (!_isInitialized || _instance == null)
                    return;

                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;

                _instance.Shutdown();
                _instance.Dispose();
                _instance = null;
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Starts a new operation scope with automatic timing and telemetry capture.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>A disposable operation scope.</returns>
        /// <exception cref="InvalidOperationException">Thrown when telemetry has not been initialized.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is null or empty.</exception>
        public static IOperationScope StartOperation(string operationName)
        {
            EnsureInitialized();
            return _instance!.StartOperation(operationName);
        }

        /// <summary>
        /// Records an exception with the telemetry system.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static void RecordException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            // RecordException works with or without initialization via the global aggregator
            exception.RecordException();
        }

        /// <summary>
        /// Tracks an exception. Requires initialization.
        /// </summary>
        /// <param name="exception">Exception to track.</param>
        /// <exception cref="InvalidOperationException">Thrown when telemetry has not been initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static void TrackException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            EnsureInitialized();
            _instance!.TrackException(exception);
        }

        /// <summary>
        /// Tracks a custom event.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <exception cref="InvalidOperationException">Thrown when telemetry has not been initialized.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventName"/> is null or empty.</exception>
        public static void TrackEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));

            EnsureInitialized();
            _instance!.TrackEvent(eventName);
        }

        /// <summary>
        /// Records a metric measurement.
        /// </summary>
        /// <param name="metricName">The metric name.</param>
        /// <param name="value">The metric value.</param>
        /// <exception cref="InvalidOperationException">Thrown when telemetry has not been initialized.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="metricName"/> is null or empty.</exception>
        public static void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(metricName));

            EnsureInitialized();
            _instance!.RecordMetric(metricName, value);
        }

        /// <summary>
        /// Sets the correlation ID for the current execution context within a scope.
        /// The previous correlation ID is restored when the returned scope is disposed.
        /// </summary>
        /// <param name="correlationId">The correlation ID to set.</param>
        /// <returns>A disposable scope that restores the previous correlation ID on disposal.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="correlationId"/> is null or empty.</exception>
        public static IDisposable SetCorrelationId(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty.", nameof(correlationId));

            return CorrelationContext.BeginScope(correlationId);
        }

        /// <summary>
        /// Generates a new correlation ID and sets it for the current execution context.
        /// The previous correlation ID is restored when the returned scope is disposed.
        /// </summary>
        /// <returns>A disposable scope that restores the previous correlation ID on disposal.</returns>
        public static IDisposable BeginCorrelation()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            return CorrelationContext.BeginScope(correlationId);
        }

        /// <summary>
        /// Gets the exception aggregator for querying exception statistics.
        /// Works without initialization.
        /// </summary>
        /// <returns>The global exception aggregator instance.</returns>
        public static ExceptionAggregator GetExceptionAggregator()
        {
            return TelemetryExceptionExtensions.GetAggregator();
        }

        /// <summary>
        /// Sets the telemetry service instance from a DI container.
        /// This is called internally by the hosted service to enable the static API
        /// when using DI mode.
        /// </summary>
        /// <param name="instance">The DI-resolved telemetry service.</param>
        internal static void SetInstance(TelemetryService instance)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                _instance = instance;
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Clears the static instance. Used for testing and shutdown in DI mode.
        /// </summary>
        internal static void ClearInstance()
        {
            lock (_lock)
            {
                _instance = null;
                _isInitialized = false;
            }
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized || _instance == null)
            {
                throw new InvalidOperationException(
                    "Telemetry has not been initialized. " +
                    "Call Telemetry.Initialize() for static mode, or use AddTelemetry() for DI mode.");
            }
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            Shutdown();
        }

        private static void OnDomainUnload(object? sender, EventArgs e)
        {
            Shutdown();
        }
    }
}
