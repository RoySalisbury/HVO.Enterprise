using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Enriches telemetry with HTTP request context.
    /// </summary>
    public sealed class HttpRequestContextProvider : IContextProvider
    {
        private static readonly Regex SensitiveQueryRegex = new Regex(
            @"(token|key|secret|password|apikey)=[^&]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IHttpRequestAccessor _requestAccessor;
        private readonly PiiRedactor _piiRedactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestContextProvider"/> class.
        /// </summary>
        /// <param name="requestAccessor">Optional request accessor.</param>
        public HttpRequestContextProvider(IHttpRequestAccessor? requestAccessor = null)
        {
            _requestAccessor = requestAccessor ?? new DefaultHttpRequestAccessor();
            _piiRedactor = new PiiRedactor();
        }

        /// <inheritdoc />
        public string Name => "HttpRequest";

        /// <inheritdoc />
        public EnrichmentLevel Level => EnrichmentLevel.Standard;

        /// <inheritdoc />
        public void EnrichActivity(Activity activity, EnrichmentOptions options)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var request = _requestAccessor.GetCurrentRequest();
            if (request == null)
                return;

            AddTag(activity, "http.method", request.Method, options);
            AddTag(activity, "http.url", request.Url, options);
            AddTag(activity, "http.target", request.Path, options);

            var queryString = request.QueryString;
            if (queryString != null && queryString.Length > 0)
            {
                var query = options.RedactPii ? RedactSensitiveQueryParams(queryString) : queryString;
                AddTag(activity, "http.query", query, options);
            }

            if (options.MaxLevel >= EnrichmentLevel.Verbose)
            {
                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        if (options.ExcludedHeaders.Contains(header.Key))
                            continue;

                        var key = "http.header." + header.Key.ToLowerInvariant();
                        AddTag(activity, key, header.Value, options);
                    }
                }

                AddTag(activity, "http.user_agent", request.UserAgent, options);
                AddTag(activity, "http.client_ip", request.ClientIp, options);
            }
        }

        /// <inheritdoc />
        public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var request = _requestAccessor.GetCurrentRequest();
            if (request == null)
                return;

            AddProperty(properties, "http.method", request.Method, options);
            AddProperty(properties, "http.url", request.Url, options);
            AddProperty(properties, "http.target", request.Path, options);

            var queryString = request.QueryString;
            if (queryString != null && queryString.Length > 0)
            {
                var query = options.RedactPii ? RedactSensitiveQueryParams(queryString) : queryString;
                AddProperty(properties, "http.query", query, options);
            }
        }

        private void AddTag(Activity activity, string key, string? value, EnrichmentOptions options)
        {
            var safeValue = value;
            if (safeValue == null || safeValue.Length == 0)
                return;

            if (_piiRedactor.TryRedact(key, safeValue, options, out var redacted) && redacted == null)
                return;

            activity.SetTag(key, redacted ?? safeValue);
        }

        private void AddProperty(IDictionary<string, object> properties, string key, string? value, EnrichmentOptions options)
        {
            var safeValue = value;
            if (safeValue == null || safeValue.Length == 0)
                return;

            if (_piiRedactor.TryRedact(key, safeValue, options, out var redacted) && redacted == null)
                return;

            properties[key] = redacted ?? safeValue;
        }

        private static string RedactSensitiveQueryParams(string queryString)
        {
            return SensitiveQueryRegex.Replace(queryString, "$1=***");
        }
    }
}
