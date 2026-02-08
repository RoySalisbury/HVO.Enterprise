using System;
using System.Collections.Generic;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Default implementation that uses async-local storage or System.Web if available.
    /// </summary>
    internal sealed class DefaultHttpRequestAccessor : IHttpRequestAccessor
    {
        private static volatile bool _systemWebChecked;
        private static Type? _httpContextType;
        private static PropertyInfo? _currentProperty;
        private static PropertyInfo? _requestProperty;

        // Cached request object property lookups
        private static Type? _requestType;
        private static PropertyInfo? _httpMethodProperty;
        private static PropertyInfo? _pathProperty;
        private static PropertyInfo? _userAgentProperty;
        private static PropertyInfo? _userHostAddressProperty;
        private static PropertyInfo? _urlProperty;
        private static PropertyInfo? _headersProperty;
        private static PropertyInfo? _headersAllKeysProperty;
        private static PropertyInfo? _headersItemProperty;

        /// <inheritdoc />
        public HttpRequestInfo? GetCurrentRequest()
        {
            var asyncLocalRequest = HttpRequestContextStore.Current;
            if (asyncLocalRequest != null)
                return asyncLocalRequest;

            return TryGetSystemWebRequest();
        }

        private static HttpRequestInfo? TryGetSystemWebRequest()
        {
            // Cache type lookups on first call
            if (!_systemWebChecked)
            {
                _httpContextType = Type.GetType("System.Web.HttpContext, System.Web");
                if (_httpContextType != null)
                {
                    _currentProperty = _httpContextType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
                    _requestProperty = _httpContextType.GetProperty("Request");
                }
                _systemWebChecked = true;
            }

            if (_httpContextType == null)
                return null;

            var httpContext = _currentProperty?.GetValue(null, null);
            if (httpContext == null)
                return null;

            var request = _requestProperty?.GetValue(httpContext, null);
            if (request == null)
                return null;

            // Cache request type property lookups on first successful access
            var requestType = request.GetType();
            if (_requestType != requestType)
            {
                _requestType = requestType;
                _httpMethodProperty = requestType.GetProperty("HttpMethod");
                _pathProperty = requestType.GetProperty("Path");
                _userAgentProperty = requestType.GetProperty("UserAgent");
                _userHostAddressProperty = requestType.GetProperty("UserHostAddress");
                _urlProperty = requestType.GetProperty("Url");
                _headersProperty = requestType.GetProperty("Headers");
            }

            var method = _httpMethodProperty?.GetValue(request, null)?.ToString();
            var path = _pathProperty?.GetValue(request, null)?.ToString();
            var userAgent = _userAgentProperty?.GetValue(request, null)?.ToString();
            var clientIp = _userHostAddressProperty?.GetValue(request, null)?.ToString();

            // Get URL and query string correctly
            var urlValue = _urlProperty?.GetValue(request, null) as Uri;
            var url = urlValue?.ToString();
            var queryString = urlValue?.Query;
            if (queryString != null && queryString.Length > 0 && queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var headersCollection = _headersProperty?.GetValue(request, null);
            if (headersCollection != null)
            {
                // Cache headers collection property lookups
                if (_headersAllKeysProperty == null || _headersItemProperty == null)
                {
                    var headersType = headersCollection.GetType();
                    _headersAllKeysProperty = headersType.GetProperty("AllKeys");
                    _headersItemProperty = headersType.GetProperty("Item");
                }

                var keys = _headersAllKeysProperty?.GetValue(headersCollection, null) as string[];
                if (keys != null)
                {
                    foreach (var key in keys)
                    {
                        var value = _headersItemProperty?.GetValue(headersCollection, new object?[] { key }) as string;
                        if (!string.IsNullOrEmpty(key) && value != null)
                            headers[key] = value;
                    }
                }
            }

            return new HttpRequestInfo
            {
                Method = method ?? string.Empty,
                Url = url ?? string.Empty,
                Path = path ?? string.Empty,
                QueryString = queryString,
                UserAgent = userAgent,
                ClientIp = clientIp,
                Headers = headers
            };
        }
    }
}
