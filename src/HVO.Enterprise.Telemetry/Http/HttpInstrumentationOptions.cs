using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Http
{
    /// <summary>
    /// Configuration options for HTTP instrumentation via <see cref="TelemetryHttpMessageHandler"/>.
    /// Controls URL redaction, header capture, body capture, and sensitive header filtering.
    /// </summary>
    public sealed class HttpInstrumentationOptions
    {
        /// <summary>
        /// Creates a new instance with default values.
        /// Each access returns a fresh instance to prevent shared mutable state.
        /// </summary>
        public static HttpInstrumentationOptions Default => new HttpInstrumentationOptions();

        /// <summary>
        /// Gets or sets whether to redact query strings in captured URLs.
        /// When <see langword="true"/>, query strings are replaced with <c>?[REDACTED]</c>.
        /// Default is <see langword="true"/> to prevent accidental PII exposure.
        /// </summary>
        public bool RedactQueryStrings { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture request headers as Activity tags.
        /// Sensitive headers (see <see cref="SensitiveHeaders"/>) are always excluded.
        /// Default is <see langword="false"/>.
        /// </summary>
        public bool CaptureRequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets whether to capture response headers as Activity tags.
        /// Sensitive headers (see <see cref="SensitiveHeaders"/>) are always excluded.
        /// Default is <see langword="false"/>.
        /// </summary>
        public bool CaptureResponseHeaders { get; set; }

        /// <summary>
        /// Gets or sets whether to capture request body content.
        /// <para>
        /// <strong>WARNING</strong>: Enabling this has significant performance impact as it
        /// requires buffering the request stream. Use only for debugging.
        /// </para>
        /// Default is <see langword="false"/>.
        /// <para>Reserved for future use. Body capture is not yet implemented in <see cref="TelemetryHttpMessageHandler"/>.</para>
        /// </summary>
        internal bool CaptureRequestBody { get; set; }

        /// <summary>
        /// Gets or sets whether to capture response body content.
        /// <para>
        /// <strong>WARNING</strong>: Enabling this has significant performance impact as it
        /// requires buffering the response stream. Use only for debugging.
        /// </para>
        /// Default is <see langword="false"/>.
        /// <para>Reserved for future use. Body capture is not yet implemented in <see cref="TelemetryHttpMessageHandler"/>.</para>
        /// </summary>
        internal bool CaptureResponseBody { get; set; }

        /// <summary>
        /// Gets or sets the maximum body size in bytes to capture when body capture is enabled.
        /// Bodies exceeding this size are truncated. Default is 4096 (4 KB).
        /// <para>Reserved for future use. Body capture is not yet implemented in <see cref="TelemetryHttpMessageHandler"/>.</para>
        /// </summary>
        internal int MaxBodyCaptureSize { get; set; } = 4096;

        /// <summary>
        /// Gets the set of header names considered sensitive and excluded from capture.
        /// Comparison is case-insensitive. Use <see cref="AddSensitiveHeader"/> and
        /// <see cref="RemoveSensitiveHeader"/> to modify.
        /// </summary>
        public IReadOnlyCollection<string> SensitiveHeaders => _sensitiveHeaders;

        private HashSet<string> _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Auth-Token",
            "Proxy-Authorization"
        };

        /// <summary>
        /// Adds a header name to the sensitive headers set.
        /// </summary>
        /// <param name="headerName">The header name to add.</param>
        public void AddSensitiveHeader(string headerName)
        {
            if (headerName == null)
                throw new ArgumentNullException(nameof(headerName));

            _sensitiveHeaders.Add(headerName);
        }

        /// <summary>
        /// Removes a header name from the sensitive headers set.
        /// </summary>
        /// <param name="headerName">The header name to remove.</param>
        /// <returns><see langword="true"/> if the header was removed; otherwise <see langword="false"/>.</returns>
        public bool RemoveSensitiveHeader(string headerName)
        {
            if (headerName == null)
                throw new ArgumentNullException(nameof(headerName));

            return _sensitiveHeaders.Remove(headerName);
        }

        /// <summary>
        /// Determines whether the specified header name is considered sensitive.
        /// </summary>
        /// <param name="headerName">The header name to check.</param>
        /// <returns><see langword="true"/> if the header is sensitive; otherwise <see langword="false"/>.</returns>
        public bool IsSensitiveHeader(string? headerName)
        {
            if (headerName == null)
                return false;

            return _sensitiveHeaders.Contains(headerName);
        }

        /// <summary>
        /// Validates the configuration options and throws if any values are invalid.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any option value is out of range.</exception>
        public void Validate()
        {
            if (MaxBodyCaptureSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxBodyCaptureSize), "Must be positive.");

            if (_sensitiveHeaders == null)
                throw new ArgumentNullException(nameof(SensitiveHeaders));
        }

        /// <summary>
        /// Creates a defensive copy of this options instance.
        /// </summary>
        /// <returns>A new <see cref="HttpInstrumentationOptions"/> with the same values.</returns>
        internal HttpInstrumentationOptions Clone()
        {
            var clone = new HttpInstrumentationOptions
            {
                RedactQueryStrings = RedactQueryStrings,
                CaptureRequestHeaders = CaptureRequestHeaders,
                CaptureResponseHeaders = CaptureResponseHeaders,
                CaptureRequestBody = CaptureRequestBody,
                CaptureResponseBody = CaptureResponseBody,
                MaxBodyCaptureSize = MaxBodyCaptureSize
            };
            clone._sensitiveHeaders = new HashSet<string>(_sensitiveHeaders, StringComparer.OrdinalIgnoreCase);
            return clone;
        }
    }
}
