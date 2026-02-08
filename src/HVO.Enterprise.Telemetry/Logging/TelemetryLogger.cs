using System;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// Static helper for creating enriched loggers without dependency injection.
    /// </summary>
    /// <remarks>
    /// <para>Use this class in .NET Framework 4.8 applications, console apps, or any
    /// scenario where the DI container is not available. For DI-based applications,
    /// prefer <see cref="TelemetryLoggerExtensions.AddTelemetryLoggingEnrichment"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Wrap an existing logger
    /// var enrichedLogger = TelemetryLogger.CreateEnrichedLogger(existingLogger);
    /// enrichedLogger.LogInformation("Order {OrderId} processed", orderId);
    /// // Output includes TraceId, SpanId, CorrelationId automatically
    ///
    /// // Wrap an entire factory
    /// var enrichedFactory = TelemetryLogger.CreateEnrichedLoggerFactory(
    ///     existingFactory, new TelemetryLoggerOptions { IncludeTraceFlags = true });
    /// var logger = enrichedFactory.CreateLogger("MyApp.OrderService");
    /// </code>
    /// </example>
    public static class TelemetryLogger
    {
        /// <summary>
        /// Creates an enriched logger wrapping the specified logger.
        /// </summary>
        /// <param name="innerLogger">The logger to wrap with telemetry enrichment.</param>
        /// <param name="options">
        /// Enrichment options. If <c>null</c>, default options are used
        /// (TraceId, SpanId, CorrelationId enabled).
        /// </param>
        /// <returns>An <see cref="ILogger"/> that automatically enriches log entries.</returns>
        public static ILogger CreateEnrichedLogger(
            ILogger innerLogger,
            TelemetryLoggerOptions? options = null)
        {
            if (innerLogger == null)
                throw new ArgumentNullException(nameof(innerLogger));

            return new TelemetryEnrichedLogger(innerLogger, options ?? new TelemetryLoggerOptions());
        }

        /// <summary>
        /// Creates an enriched logger factory wrapping the specified factory.
        /// All loggers created from the returned factory will automatically include
        /// telemetry enrichment.
        /// </summary>
        /// <param name="innerFactory">The logger factory to wrap.</param>
        /// <param name="options">
        /// Enrichment options. If <c>null</c>, default options are used.
        /// </param>
        /// <returns>An <see cref="ILoggerFactory"/> that creates enriched loggers.</returns>
        public static ILoggerFactory CreateEnrichedLoggerFactory(
            ILoggerFactory innerFactory,
            TelemetryLoggerOptions? options = null)
        {
            if (innerFactory == null)
                throw new ArgumentNullException(nameof(innerFactory));

            return new TelemetryEnrichedLoggerFactory(innerFactory, options ?? new TelemetryLoggerOptions());
        }
    }
}
