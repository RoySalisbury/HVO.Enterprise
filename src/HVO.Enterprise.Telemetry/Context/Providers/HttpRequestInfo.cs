using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Represents HTTP request information.
    /// </summary>
    public sealed class HttpRequestInfo
    {
        /// <summary>
        /// Gets or sets the HTTP method.
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the request URL.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the request path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the query string.
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the client IP.
        /// </summary>
        public string? ClientIp { get; set; }
    }
}
