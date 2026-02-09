using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Http
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that adds distributed tracing to HTTP client requests.
    /// Automatically creates child activities, records OpenTelemetry semantic conventions,
    /// and propagates W3C TraceContext headers (<c>traceparent</c> / <c>tracestate</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler works with both .NET Framework 4.8 and .NET 8+ and can be used
    /// standalone or with dependency injection:
    /// </para>
    /// <code>
    /// // Standalone usage
    /// var handler = new TelemetryHttpMessageHandler { InnerHandler = new HttpClientHandler() };
    /// var client = new HttpClient(handler);
    ///
    /// // Factory usage
    /// var client = HttpClientTelemetryExtensions.CreateWithTelemetry();
    /// </code>
    /// </remarks>
    public sealed class TelemetryHttpMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// The ActivitySource name used by this handler for distributed tracing.
        /// </summary>
        internal const string ActivitySourceName = "HVO.Enterprise.Telemetry.Http";

        private static readonly ActivitySource HttpActivitySource =
            new ActivitySource(ActivitySourceName, "1.0.0");

        private readonly ILogger<TelemetryHttpMessageHandler>? _logger;
        private readonly HttpInstrumentationOptions _options;

        /// <summary>
        /// Creates a new instance with default options and no logger.
        /// </summary>
        public TelemetryHttpMessageHandler()
            : this(null, null)
        {
        }

        /// <summary>
        /// Creates a new instance with specified options and optional logger.
        /// </summary>
        /// <param name="options">
        /// Instrumentation options. When <see langword="null"/>, <see cref="HttpInstrumentationOptions.Default"/> is used.
        /// </param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        public TelemetryHttpMessageHandler(
            HttpInstrumentationOptions? options,
            ILogger<TelemetryHttpMessageHandler>? logger = null)
        {
            var effectiveOptions = options ?? HttpInstrumentationOptions.Default;
            effectiveOptions.Validate();
            _options = effectiveOptions.Clone();
            _logger = logger;
        }

        /// <summary>
        /// Sends an HTTP request with distributed tracing instrumentation.
        /// Creates a child <see cref="Activity"/>, enriches it with request/response details,
        /// injects W3C TraceContext headers, and records errors.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Start an activity for this HTTP request
            using var activity = HttpActivitySource.StartActivity(
                $"HTTP {request.Method}",
                ActivityKind.Client);

            if (activity == null)
            {
                // Activity not sampled or no listener — continue without instrumentation
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                // Enrich activity with request details
                EnrichActivityWithRequest(activity, request);

                // Inject W3C TraceContext headers for distributed tracing
                InjectW3CTraceContext(request, activity);

                // Optionally capture request headers
                if (_options.CaptureRequestHeaders)
                {
                    CaptureHeaders(activity, request.Headers, "http.request.header");
                }

                // Execute the request
                var response = await base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                // Enrich activity with response details
                EnrichActivityWithResponse(activity, response);

                // Optionally capture response headers
                if (_options.CaptureResponseHeaders)
                {
                    CaptureHeaders(activity, response.Headers, "http.response.header");
                }

                return response;
            }
            catch (Exception ex)
            {
                // Record exception on the activity
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.RecordException(ex);

                _logger?.LogError(ex,
                    "HTTP request failed: {Method} {Url}",
                    request.Method,
                    GetRedactedUrl(request.RequestUri));

                throw;
            }
        }

        private void EnrichActivityWithRequest(Activity activity, HttpRequestMessage request)
        {
            var url = request.RequestUri;
            if (url == null)
                return;

            // Standard OpenTelemetry HTTP semantic conventions
            activity.SetTag("http.method", request.Method.Method);
            activity.SetTag("http.url", GetRedactedUrl(url));
            activity.SetTag("http.scheme", url.Scheme);
            activity.SetTag("http.host", url.Host);
            activity.SetTag("http.target", GetRedactedTarget(url));

            if (url.Port > 0 && !IsDefaultPort(url))
            {
                activity.SetTag("net.peer.port", url.Port);
            }

            // Set a human-readable display name
            activity.DisplayName = $"{request.Method} {url.Host}{url.AbsolutePath}";
        }

        private void EnrichActivityWithResponse(Activity activity, HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            activity.SetTag("http.status_code", statusCode);

            // Set activity status based on HTTP status code
            if (statusCode >= 500)
            {
                activity.SetStatus(
                    ActivityStatusCode.Error,
                    response.ReasonPhrase ?? string.Empty);
            }
            else if (statusCode >= 400)
            {
                // 4xx errors are client errors — mark as unset per OTel conventions
                // but still record the status for observability
                activity.SetStatus(ActivityStatusCode.Unset);
            }
        }

        /// <summary>
        /// Injects W3C TraceContext headers into the outgoing HTTP request.
        /// <para>
        /// Format: <c>traceparent: 00-{trace-id}-{span-id}-{trace-flags}</c>
        /// </para>
        /// </summary>
        private void InjectW3CTraceContext(HttpRequestMessage request, Activity activity)
        {
            var traceId = activity.TraceId.ToHexString();
            var spanId = activity.SpanId.ToHexString();
            var traceFlags = (activity.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0
                ? "01"
                : "00";

            var traceparent = $"00-{traceId}-{spanId}-{traceFlags}";

            // Remove any existing values to avoid duplicates (e.g. if handler is applied twice)
            request.Headers.Remove("traceparent");
            request.Headers.Remove("tracestate");

            // Use TryAddWithoutValidation to avoid format validation errors
            request.Headers.TryAddWithoutValidation("traceparent", traceparent);

            // Also propagate tracestate if present
            var tracestate = activity.TraceStateString;
            if (!string.IsNullOrEmpty(tracestate))
            {
                request.Headers.TryAddWithoutValidation("tracestate", tracestate);
            }

            _logger?.LogDebug(
                "Injected W3C TraceContext: traceparent={TraceParent}",
                traceparent);
        }

        private void CaptureHeaders(
            Activity activity,
            HttpHeaders headers,
            string prefix)
        {
            foreach (var header in headers.Where(h => !_options.IsSensitiveHeader(h.Key)))
            {
                var value = string.Join(", ", header.Value);
                activity.SetTag($"{prefix}.{header.Key.ToLowerInvariant()}", value);
            }
        }

        internal string GetRedactedUrl(Uri? uri)
        {
            if (uri == null)
                return string.Empty;

            if (_options.RedactQueryStrings && !string.IsNullOrEmpty(uri.Query))
            {
                return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}?[REDACTED]";
            }

            return uri.ToString();
        }

        private string GetRedactedTarget(Uri uri)
        {
            if (_options.RedactQueryStrings && !string.IsNullOrEmpty(uri.Query))
            {
                return $"{uri.AbsolutePath}?[REDACTED]";
            }

            return uri.PathAndQuery;
        }

        private static bool IsDefaultPort(Uri uri)
        {
            return (uri.Scheme == "http" && uri.Port == 80) ||
                   (uri.Scheme == "https" && uri.Port == 443);
        }
    }
}
