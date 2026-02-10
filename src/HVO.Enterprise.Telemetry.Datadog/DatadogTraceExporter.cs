using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Enriches <see cref="Activity"/> instances with Datadog-specific tags and provides
    /// trace-context propagation header generation and extraction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides Activity enrichment with Datadog unified service tags and manual trace-context
    /// propagation via both W3C and Datadog-native headers
    /// (<c>x-datadog-trace-id</c>, <c>x-datadog-parent-id</c>).
    /// </para>
    /// <para>
    /// This class is thread-safe. Configuration values are captured as immutable snapshots
    /// during construction; subsequent mutations to the source <see cref="DatadogOptions"/>
    /// instance have no effect.
    /// </para>
    /// </remarks>
    public sealed class DatadogTraceExporter
    {
        private readonly string? _serviceName;
        private readonly string? _environment;
        private readonly string? _version;
        private readonly KeyValuePair<string, string>[] _globalTags;
        private readonly ILogger<DatadogTraceExporter>? _logger;
        private readonly bool _enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatadogTraceExporter"/> class.
        /// </summary>
        /// <param name="options">Datadog configuration options.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public DatadogTraceExporter(
            DatadogOptions options,
            ILogger<DatadogTraceExporter>? logger = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger;
            _enabled = options.EnableTraceExporter;

            // Snapshot immutable copies to guarantee thread safety
            _serviceName = options.ServiceName;
            _environment = options.Environment;
            _version = options.Version;
            _globalTags = options.GlobalTags.ToArray();

            if (!_enabled)
            {
                _logger?.LogInformation("DatadogTraceExporter registered but disabled via EnableTraceExporter=false");
            }
        }

        /// <summary>
        /// Enriches an <see cref="Activity"/> with Datadog unified service tags (<c>service.name</c>,
        /// <c>env</c>, <c>version</c>) and any configured global tags.
        /// </summary>
        /// <param name="activity">The activity to enrich.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> is null.</exception>
        public void EnrichActivity(Activity activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (!_enabled) { return; }

            // Add unified service tags
            if (!string.IsNullOrEmpty(_serviceName))
            {
                activity.SetTag("service.name", _serviceName);
            }
            if (!string.IsNullOrEmpty(_environment))
            {
                activity.SetTag("env", _environment);
            }
            if (!string.IsNullOrEmpty(_version))
            {
                activity.SetTag("version", _version);
            }

            // Add global tags (don't overwrite existing)
            foreach (var tag in _globalTags.Where(tag => !activity.Tags.Any(t => t.Key == tag.Key)))
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        /// <summary>
        /// Creates HTTP headers for Datadog trace context propagation.
        /// Emits both W3C <c>traceparent</c>/<c>tracestate</c> and Datadog-native
        /// <c>x-datadog-trace-id</c>/<c>x-datadog-parent-id</c> headers.
        /// </summary>
        /// <param name="activity">
        /// The activity whose context to propagate. Falls back to <see cref="Activity.Current"/> when <see langword="null"/>.
        /// </param>
        /// <returns>A dictionary of header name-value pairs. Empty when no activity is available.</returns>
        public IDictionary<string, string> CreatePropagationHeaders(Activity? activity = null)
        {
            activity ??= Activity.Current;
            if (activity == null)
            {
                return new Dictionary<string, string>();
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                // W3C TraceContext headers
                if (!string.IsNullOrEmpty(activity.Id))
                {
                    headers["traceparent"] = activity.Id!;
                }

                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    headers["tracestate"] = activity.TraceStateString!;
                }

                // Datadog-native headers (hex-to-decimal conversion for compatibility)
                var traceIdHex = activity.TraceId.ToString();
                var spanIdHex = activity.SpanId.ToString();

                if (traceIdHex.Length >= 16
                    && ulong.TryParse(
                        traceIdHex.Substring(traceIdHex.Length - 16),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out var traceIdDecimal))
                {
                    headers["x-datadog-trace-id"] = traceIdDecimal.ToString(CultureInfo.InvariantCulture);
                }

                if (ulong.TryParse(
                        spanIdHex,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out var spanIdDecimal))
                {
                    headers["x-datadog-parent-id"] = spanIdDecimal.ToString(CultureInfo.InvariantCulture);
                }
            }

            return headers;
        }

        /// <summary>
        /// Extracts Datadog or W3C trace context from incoming HTTP headers.
        /// </summary>
        /// <param name="headers">HTTP headers to inspect.</param>
        /// <returns>
        /// A tuple of (<c>TraceId</c>, <c>ParentId</c>, <c>SamplingPriority</c>) if trace context
        /// is found; otherwise <see langword="null"/>.
        /// </returns>
        public (string TraceId, string ParentId, string? SamplingPriority)?
            ExtractTraceContext(IDictionary<string, string>? headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return null;
            }

            // Prefer W3C TraceContext
            if (headers.TryGetValue("traceparent", out var traceparent)
                && !string.IsNullOrEmpty(traceparent))
            {
                var parsed = ParseW3CTraceParent(traceparent);
                if (parsed != null)
                {
                    return parsed;
                }
            }

            // Fall back to Datadog-native headers
            if (headers.TryGetValue("x-datadog-trace-id", out var traceId)
                && headers.TryGetValue("x-datadog-parent-id", out var parentId)
                && !string.IsNullOrEmpty(traceId)
                && !string.IsNullOrEmpty(parentId))
            {
                headers.TryGetValue("x-datadog-sampling-priority", out var samplingPriority);
                return (traceId, parentId, samplingPriority);
            }

            return null;
        }

        private static (string TraceId, string ParentId, string? SamplingPriority)?
            ParseW3CTraceParent(string traceparent)
        {
            // W3C format: {version}-{trace-id}-{parent-id}-{trace-flags}
            var parts = traceparent.Split('-');
            if (parts.Length != 4)
            {
                return null;
            }

            return (parts[1], parts[2], parts[3]);
        }
    }
}
