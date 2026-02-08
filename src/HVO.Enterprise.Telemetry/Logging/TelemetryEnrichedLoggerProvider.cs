using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Logger provider that enriches logs with Activity and correlation context.
    /// Wraps an existing <see cref="ILoggerProvider"/> and returns enriched loggers
    /// for each category.
    /// </summary>
    /// <remarks>
    /// <para>Logger instances are cached per category name to match the standard
    /// <see cref="ILoggerProvider"/> contract (same category → same logger).</para>
    /// <para>This provider is designed to wrap another provider. For DI scenarios,
    /// use <see cref="TelemetryLoggerExtensions.AddTelemetryLoggingEnrichment"/> which
    /// registers a <see cref="TelemetryEnrichedLoggerFactory"/> wrapper. For standalone
    /// usage, use <see cref="TelemetryLogger.CreateEnrichedLogger"/>.</para>
    /// </remarks>
    public sealed class TelemetryEnrichedLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerProvider _innerProvider;
        private readonly TelemetryLoggerOptions _options;
        private readonly ConcurrentDictionary<string, ILogger> _loggers;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryEnrichedLoggerProvider"/> class.
        /// </summary>
        /// <param name="innerProvider">The inner logger provider to wrap.</param>
        /// <param name="options">
        /// Enrichment options. If <c>null</c>, default options are used
        /// (TraceId, SpanId, CorrelationId enabled).
        /// </param>
        public TelemetryEnrichedLoggerProvider(
            ILoggerProvider innerProvider,
            TelemetryLoggerOptions? options = null)
        {
            _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
            _options = options ?? new TelemetryLoggerOptions();
            _loggers = new ConcurrentDictionary<string, ILogger>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
            {
                var innerLogger = _innerProvider.CreateLogger(name);
                return new TelemetryEnrichedLogger(innerLogger, _options);
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _loggers.Clear();
            _innerProvider.Dispose();
        }
    }
}
