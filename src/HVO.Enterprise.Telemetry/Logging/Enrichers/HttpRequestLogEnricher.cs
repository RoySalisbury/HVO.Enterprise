using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Context.Providers;

namespace HVO.Enterprise.Telemetry.Logging.Enrichers
{
    /// <summary>
    /// Enriches log entries with HTTP request context (method, path, URL).
    /// </summary>
    /// <remarks>
    /// Delegates to the existing <see cref="IHttpRequestAccessor"/> infrastructure.
    /// Only non-sensitive request metadata is included. Query strings, headers,
    /// and request bodies are excluded to avoid PII leakage.
    /// </remarks>
    public sealed class HttpRequestLogEnricher : ILogEnricher
    {
        private readonly IHttpRequestAccessor _requestAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestLogEnricher"/> class.
        /// </summary>
        /// <param name="requestAccessor">
        /// Optional HTTP request accessor. If <c>null</c>, uses
        /// <see cref="DefaultHttpRequestAccessor"/> which attempts runtime detection
        /// of the current HTTP context.
        /// </param>
        public HttpRequestLogEnricher(IHttpRequestAccessor? requestAccessor = null)
        {
            _requestAccessor = requestAccessor ?? new DefaultHttpRequestAccessor();
        }

        /// <inheritdoc />
        public void Enrich(IDictionary<string, object?> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var request = _requestAccessor.GetCurrentRequest();
            if (request == null)
                return;

            if (!string.IsNullOrEmpty(request.Method))
                properties["HttpMethod"] = request.Method;

            if (!string.IsNullOrEmpty(request.Path))
                properties["HttpPath"] = request.Path;

            if (!string.IsNullOrEmpty(request.Url))
                properties["HttpUrl"] = request.Url;
        }
    }
}
