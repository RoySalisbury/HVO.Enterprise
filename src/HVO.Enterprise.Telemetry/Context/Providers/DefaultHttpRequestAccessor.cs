using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

            var requestType = request.GetType();
            var method = GetString(request, requestType, "HttpMethod");
            var path = GetString(request, requestType, "Path");
            var userAgent = GetString(request, requestType, "UserAgent");
            var clientIp = GetString(request, requestType, "UserHostAddress");

            // Get URL and query string correctly
            var urlProperty = requestType.GetProperty("Url");
            var urlValue = urlProperty?.GetValue(request, null) as Uri;
            var url = urlValue?.ToString();
            var queryString = urlValue?.Query;
            if (queryString != null && queryString.Length > 0 && queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var headersProperty = requestType.GetProperty("Headers");
            var headersCollection = headersProperty != null ? headersProperty.GetValue(request, null) : null;
            if (headersCollection != null)
            {
                var keysProperty = headersCollection.GetType().GetProperty("AllKeys");
                var keys = keysProperty != null ? keysProperty.GetValue(headersCollection, null) as string[] : null;
                if (keys != null)
                {
                    var itemProperty = headersCollection.GetType().GetProperty("Item");
                    foreach (var key in keys)
                    {
                        var value = itemProperty != null ? itemProperty.GetValue(headersCollection, new object?[] { key }) as string : null;
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

        private static string? GetString(object target, Type targetType, string propertyName)
        {
            var property = targetType.GetProperty(propertyName);
            var value = property != null ? property.GetValue(target, null) : null;
            return value != null ? value.ToString() : null;
        }
    }
}
