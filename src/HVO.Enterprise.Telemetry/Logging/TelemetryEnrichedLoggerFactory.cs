using System;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Logger factory wrapper that creates enriched loggers for every category.
    /// </summary>
    /// <remarks>
    /// This wraps an existing <see cref="ILoggerFactory"/> so that all loggers created
    /// through it automatically receive telemetry enrichment. Provider registrations
    /// (<see cref="AddProvider"/>) are forwarded to the inner factory.
    /// </remarks>
    internal sealed class TelemetryEnrichedLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _innerFactory;
        private readonly TelemetryLoggerOptions _options;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryEnrichedLoggerFactory"/> class.
        /// </summary>
        /// <param name="innerFactory">The inner logger factory to wrap.</param>
        /// <param name="options">Enrichment options.</param>
        internal TelemetryEnrichedLoggerFactory(ILoggerFactory innerFactory, TelemetryLoggerOptions options)
        {
            _innerFactory = innerFactory ?? throw new ArgumentNullException(nameof(innerFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _innerFactory.CreateLogger(categoryName);
            return new TelemetryEnrichedLogger(innerLogger, _options);
        }

        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider)
        {
            _innerFactory.AddProvider(provider);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _innerFactory.Dispose();
        }
    }
}
