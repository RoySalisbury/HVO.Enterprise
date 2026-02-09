using System;
using System.ServiceModel.Channels;

namespace HVO.Enterprise.Telemetry.Wcf.Propagation
{
    /// <summary>
    /// Reads and writes custom SOAP headers for W3C Trace Context propagation.
    /// </summary>
    /// <remarks>
    /// Headers are stored in the HVO telemetry SOAP namespace defined by
    /// <see cref="TraceContextConstants.SoapNamespace"/>. This avoids conflicts
    /// with other SOAP headers and provides a consistent location for trace context
    /// across all HVO-instrumented WCF services.
    /// </remarks>
    public static class SoapHeaderAccessor
    {
        /// <summary>
        /// Gets a header value from the message headers by name.
        /// </summary>
        /// <param name="headers">The message headers to search.</param>
        /// <param name="name">The header name (e.g., "traceparent").</param>
        /// <returns>The header value as a string, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public static string? GetHeader(MessageHeaders headers, string name)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Header name cannot be null or empty.", nameof(name));

            var headerIndex = headers.FindHeader(name, TraceContextConstants.SoapNamespace);
            if (headerIndex < 0)
                return null;

            try
            {
                return headers.GetHeader<string>(headerIndex);
            }
            catch
            {
                // Header exists but could not be deserialized as string
                return null;
            }
        }

        /// <summary>
        /// Adds a header to the message headers.
        /// </summary>
        /// <param name="headers">The message headers to add to.</param>
        /// <param name="name">The header name (e.g., "traceparent").</param>
        /// <param name="value">The header value.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="headers"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> or <paramref name="value"/> is null or empty.
        /// </exception>
        public static void AddHeader(MessageHeaders headers, string name, string value)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Header name cannot be null or empty.", nameof(name));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Header value cannot be null or empty.", nameof(value));

            var header = MessageHeader.CreateHeader(
                name,
                TraceContextConstants.SoapNamespace,
                value);

            headers.Add(header);
        }

        /// <summary>
        /// Removes a header from the message headers if it exists.
        /// </summary>
        /// <param name="headers">The message headers to remove from.</param>
        /// <param name="name">The header name to remove.</param>
        /// <returns><c>true</c> if the header was found and removed; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is null.</exception>
        public static bool RemoveHeader(MessageHeaders headers, string name)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            var headerIndex = headers.FindHeader(name, TraceContextConstants.SoapNamespace);
            if (headerIndex >= 0)
            {
                headers.RemoveAt(headerIndex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds or replaces a header in the message headers.
        /// </summary>
        /// <param name="headers">The message headers to modify.</param>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> or <paramref name="value"/> is null or empty.
        /// </exception>
        public static void SetHeader(MessageHeaders headers, string name, string value)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Header name cannot be null or empty.", nameof(name));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Header value cannot be null or empty.", nameof(value));

            RemoveHeader(headers, name);
            AddHeader(headers, name, value);
        }
    }
}
