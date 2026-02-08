using System;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Unified interface for recording metrics across all .NET platforms.
    /// </summary>
    public interface IMetricRecorder
    {
        /// <summary>
        /// Creates a counter that only increases over time.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="unit">Optional unit.</param>
        /// <param name="description">Optional description.</param>
        /// <returns>The counter instance.</returns>
        ICounter<long> CreateCounter(string name, string? unit = null, string? description = null);

        /// <summary>
        /// Creates a histogram for recording distribution of values.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="unit">Optional unit.</param>
        /// <param name="description">Optional description.</param>
        /// <returns>The histogram instance.</returns>
        IHistogram<long> CreateHistogram(string name, string? unit = null, string? description = null);

        /// <summary>
        /// Creates a histogram for recording distribution of double values.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="unit">Optional unit.</param>
        /// <param name="description">Optional description.</param>
        /// <returns>The histogram instance.</returns>
        IHistogram<double> CreateHistogramDouble(string name, string? unit = null, string? description = null);

        /// <summary>
        /// Creates an observable gauge (callback-based point-in-time value).
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="observeValue">Callback to read the current value.</param>
        /// <param name="unit">Optional unit.</param>
        /// <param name="description">Optional description.</param>
        /// <returns>A disposable handle for the observable gauge.</returns>
        IDisposable CreateObservableGauge(
            string name,
            Func<double> observeValue,
            string? unit = null,
            string? description = null);
    }
}
