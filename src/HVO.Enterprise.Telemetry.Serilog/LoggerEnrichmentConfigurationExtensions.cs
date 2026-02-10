using System;
using Serilog;
using Serilog.Configuration;

namespace HVO.Enterprise.Telemetry.Serilog
{
    /// <summary>
    /// Extension methods for configuring HVO telemetry enrichers on Serilog's
    /// <see cref="LoggerEnrichmentConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides fluent API methods for adding Activity tracing and correlation enrichment
    /// to a Serilog logger configuration:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="WithActivity"/> — Adds TraceId, SpanId, ParentId from <c>Activity.Current</c></description></item>
    /// <item><description><see cref="WithCorrelation"/> — Adds CorrelationId from <c>CorrelationContext.Current</c></description></item>
    /// <item><description><see cref="WithTelemetry"/> — Convenience method that adds both enrichers</description></item>
    /// </list>
    /// <para>
    /// These enrichers complement the core <c>HVO.Enterprise.Telemetry</c> ILogger enrichment by providing
    /// native Serilog integration. While the core library enriches via <c>ILogger.BeginScope</c> (provider-agnostic),
    /// these enrichers use Serilog's native <c>ILogEventEnricher</c> pipeline, which is more efficient when
    /// Serilog is the primary logging framework.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Recommended: Add both enrichers
    /// Log.Logger = new LoggerConfiguration()
    ///     .Enrich.WithTelemetry()
    ///     .WriteTo.Console()
    ///     .CreateLogger();
    ///
    /// // Or add individually for more control
    /// Log.Logger = new LoggerConfiguration()
    ///     .Enrich.WithActivity(traceIdPropertyName: "trace_id")
    ///     .Enrich.WithCorrelation(propertyName: "correlation_id")
    ///     .WriteTo.Console()
    ///     .CreateLogger();
    /// </code>
    /// </example>
    public static class LoggerEnrichmentConfigurationExtensions
    {
        /// <summary>
        /// Enriches log events with Activity tracing information (TraceId, SpanId, ParentId).
        /// </summary>
        /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
        /// <param name="traceIdPropertyName">Property name for TraceId. Default: <c>"TraceId"</c>.</param>
        /// <param name="spanIdPropertyName">Property name for SpanId. Default: <c>"SpanId"</c>.</param>
        /// <param name="parentIdPropertyName">Property name for ParentId. Default: <c>"ParentId"</c>.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="enrichmentConfiguration"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// Log.Logger = new LoggerConfiguration()
        ///     .Enrich.WithActivity()
        ///     .WriteTo.Console()
        ///     .CreateLogger();
        /// </code>
        /// </example>
        public static LoggerConfiguration WithActivity(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            string traceIdPropertyName = "TraceId",
            string spanIdPropertyName = "SpanId",
            string parentIdPropertyName = "ParentId")
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            return enrichmentConfiguration.With(new ActivityEnricher(
                traceIdPropertyName,
                spanIdPropertyName,
                parentIdPropertyName));
        }

        /// <summary>
        /// Enriches log events with correlation ID from <see cref="HVO.Enterprise.Telemetry.Correlation.CorrelationContext"/>.
        /// </summary>
        /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
        /// <param name="propertyName">Property name for correlation ID. Default: <c>"CorrelationId"</c>.</param>
        /// <param name="fallbackToActivity">
        /// Whether to use Activity TraceId when no explicit correlation is set. Default: <see langword="true"/>.
        /// </param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="enrichmentConfiguration"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// Log.Logger = new LoggerConfiguration()
        ///     .Enrich.WithCorrelation(
        ///         propertyName: "request_id",
        ///         fallbackToActivity: true)
        ///     .WriteTo.Console()
        ///     .CreateLogger();
        /// </code>
        /// </example>
        public static LoggerConfiguration WithCorrelation(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            string propertyName = "CorrelationId",
            bool fallbackToActivity = true)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            return enrichmentConfiguration.With(new CorrelationEnricher(
                propertyName,
                fallbackToActivity));
        }

        /// <summary>
        /// Enriches log events with both Activity tracing and correlation information.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method equivalent to calling:
        /// <c>.Enrich.WithActivity().Enrich.WithCorrelation()</c>
        /// </para>
        /// <para>
        /// Adds the following properties to log events when available:
        /// </para>
        /// <list type="bullet">
        /// <item><description><c>TraceId</c> — W3C trace identifier or hierarchical root ID</description></item>
        /// <item><description><c>SpanId</c> — W3C span identifier or hierarchical activity ID</description></item>
        /// <item><description><c>ParentId</c> — Parent span/activity ID (if present)</description></item>
        /// <item><description><c>CorrelationId</c> — Correlation ID from <c>CorrelationContext</c></description></item>
        /// </list>
        /// </remarks>
        /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="enrichmentConfiguration"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// Log.Logger = new LoggerConfiguration()
        ///     .Enrich.WithTelemetry()
        ///     .WriteTo.Console(outputTemplate:
        ///         "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        ///         "{TraceId} {SpanId} {CorrelationId}{NewLine}{Exception}")
        ///     .CreateLogger();
        /// </code>
        /// </example>
        public static LoggerConfiguration WithTelemetry(
            this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            return enrichmentConfiguration
                .With(new ActivityEnricher())
                .Enrich.With(new CorrelationEnricher());
        }
    }
}
