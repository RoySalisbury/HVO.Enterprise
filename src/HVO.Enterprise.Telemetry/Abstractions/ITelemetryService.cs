using System;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Defines the contract for telemetry services providing unified observability.
    /// Supports both DI-based and static initialization patterns.
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// Gets whether the telemetry service is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the real-time telemetry statistics.
        /// </summary>
        ITelemetryStatistics Statistics { get; }

        /// <summary>
        /// Starts a new operation scope with automatic timing and telemetry capture.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>A disposable operation scope.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is null or empty.</exception>
        IOperationScope StartOperation(string operationName);

        /// <summary>
        /// Tracks an exception with the telemetry system.
        /// </summary>
        /// <param name="exception">The exception to track.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        void TrackException(Exception exception);

        /// <summary>
        /// Tracks a custom event by name.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventName"/> is null or empty.</exception>
        void TrackEvent(string eventName);

        /// <summary>
        /// Records a metric measurement.
        /// </summary>
        /// <param name="metricName">The metric name.</param>
        /// <param name="value">The metric value.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="metricName"/> is null or empty.</exception>
        void RecordMetric(string metricName, double value);

        /// <summary>
        /// Starts the telemetry service and begins processing.
        /// </summary>
        void Start();

        /// <summary>
        /// Shuts down the telemetry service, flushing pending data.
        /// </summary>
        void Shutdown();
    }
}
