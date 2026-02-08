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
            var httpContextType = Type.GetType("System.Web.HttpContext, System.Web");
            if (httpContextType == null)
                return null;

            var currentProperty = httpContextType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
            var httpContext = currentProperty != null ? currentProperty.GetValue(null, null) : null;
            if (httpContext == null)
                return null;

            var requestProperty = httpContextType.GetProperty("Request");
            var request = requestProperty != null ? requestProperty.GetValue(httpContext, null) : null;
            if (request == null)
                return null;

            var requestType = request.GetType();
            var method = GetString(request, requestType, "HttpMethod");
            var url = GetString(request, requestType, "Url");
            var path = GetString(request, requestType, "Path");
            var queryString = GetString(request, requestType, "QueryString");
            var userAgent = GetString(request, requestType, "UserAgent");
            var clientIp = GetString(request, requestType, "UserHostAddress");

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
