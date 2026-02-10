using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StatsdClient;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Exports metrics to Datadog using the DogStatsD protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Wraps the DogStatsD C# client to provide counter, gauge, histogram, distribution,
    /// and timing metric types with automatic global-tag injection and optional metric prefixing.
    /// </para>
    /// <para>
    /// DogStatsD uses fire-and-forget UDP (or Unix domain socket) transport. Metric calls
    /// are non-blocking and will not throw if the Datadog agent is unreachable.
    /// </para>
    /// <para>
    /// This class is thread-safe. Create a single instance and register it as a singleton.
    /// </para>
    /// </remarks>
    public sealed class DatadogMetricsExporter : IDisposable
    {
        private readonly DogStatsdService? _statsd;
        private readonly ILogger<DatadogMetricsExporter>? _logger;
        private readonly string[] _globalTags;
        private readonly bool _enabled;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatadogMetricsExporter"/> class.
        /// </summary>
        /// <param name="options">Datadog configuration options.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public DatadogMetricsExporter(
            DatadogOptions options,
            ILogger<DatadogMetricsExporter>? logger = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger;
            _enabled = options.EnableMetricsExporter;

            if (!_enabled)
            {
                _globalTags = Array.Empty<string>();
                _logger?.LogInformation("DatadogMetricsExporter registered but disabled via EnableMetricsExporter=false");
                return;
            }

            _globalTags = options.GlobalTags
                .Select(kvp => kvp.Key + ":" + kvp.Value)
                .ToArray();

            var statsdConfig = new StatsdConfig
            {
                StatsdServerName = options.GetEffectiveServerName(),
                StatsdPort = options.AgentPort,
                Prefix = options.MetricPrefix ?? string.Empty,
                ConstantTags = _globalTags
            };

            _statsd = new DogStatsdService();
            _statsd.Configure(statsdConfig);

            if (options.UseUnixDomainSocket)
            {
                _logger?.LogInformation(
                    "DatadogMetricsExporter initialized: {Endpoint}, Transport: UDS",
                    options.GetEffectiveServerName());
            }
            else
            {
                _logger?.LogInformation(
                    "DatadogMetricsExporter initialized: {Host}:{Port}, Transport: UDP",
                    options.AgentHost,
                    options.AgentPort);
            }
        }

        /// <summary>
        /// Records a counter metric (incremental count).
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Counter value.</param>
        /// <param name="tags">Optional tags as key-value pairs.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the exporter has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void Counter(string name, long value, IDictionary<string, string>? tags = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(name));
            }

            if (!_enabled) { return; }
            _statsd!.Counter(name, value, tags: FormatTags(tags));
        }

        /// <summary>
        /// Records a gauge metric (point-in-time value).
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Gauge value.</param>
        /// <param name="tags">Optional tags as key-value pairs.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the exporter has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void Gauge(string name, double value, IDictionary<string, string>? tags = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(name));
            }

            if (!_enabled) { return; }
            _statsd!.Gauge(name, value, tags: FormatTags(tags));
        }

        /// <summary>
        /// Records a histogram metric (statistical distribution computed agent-side).
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Histogram value.</param>
        /// <param name="tags">Optional tags as key-value pairs.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the exporter has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void Histogram(string name, double value, IDictionary<string, string>? tags = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(name));
            }

            if (!_enabled) { return; }
            _statsd!.Histogram(name, value, tags: FormatTags(tags));
        }

        /// <summary>
        /// Records a distribution metric (computed server-side for global percentiles).
        /// Preferred over <see cref="Histogram"/> for percentile computations across multiple hosts.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Distribution value.</param>
        /// <param name="tags">Optional tags as key-value pairs.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the exporter has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void Distribution(string name, double value, IDictionary<string, string>? tags = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(name));
            }

            if (!_enabled) { return; }
            _statsd!.Distribution(name, value, tags: FormatTags(tags));
        }

        /// <summary>
        /// Records a timing metric (duration in milliseconds).
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="milliseconds">Duration in milliseconds.</param>
        /// <param name="tags">Optional tags as key-value pairs.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the exporter has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void Timing(string name, double milliseconds, IDictionary<string, string>? tags = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(name));
            }

            if (!_enabled) { return; }
            _statsd!.Timer(name, milliseconds, tags: FormatTags(tags));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _statsd?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing DogStatsD service");
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatadogMetricsExporter));
            }
        }

        private static string[]? FormatTags(IDictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return null;
            }

            return tags.Select(kvp => kvp.Key + ":" + kvp.Value).ToArray();
        }
    }
}
