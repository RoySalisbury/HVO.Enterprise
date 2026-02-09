using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Http
{
    /// <summary>
    /// Extension methods and factory methods for adding telemetry instrumentation to
    /// <see cref="HttpClient"/> instances.
    /// </summary>
    public static class HttpClientTelemetryExtensions
    {
        /// <summary>
        /// Creates an <see cref="HttpClient"/> with telemetry instrumentation.
        /// The returned client automatically creates distributed tracing activities
        /// and propagates W3C TraceContext headers on all requests.
        /// </summary>
        /// <param name="options">
        /// Optional instrumentation options. When <see langword="null"/>,
        /// <see cref="HttpInstrumentationOptions.Default"/> is used.
        /// </param>
        /// <param name="innerHandler">
        /// Optional inner handler. When <see langword="null"/>,
        /// a new <see cref="HttpClientHandler"/> is created.
        /// </param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        /// <returns>A new <see cref="HttpClient"/> with telemetry instrumentation.</returns>
        public static HttpClient CreateWithTelemetry(
            HttpInstrumentationOptions? options = null,
            HttpMessageHandler? innerHandler = null,
            ILogger<TelemetryHttpMessageHandler>? logger = null)
        {
            var effectiveOptions = options ?? HttpInstrumentationOptions.Default;
            innerHandler = innerHandler ?? new HttpClientHandler();

            var telemetryHandler = new TelemetryHttpMessageHandler(effectiveOptions, logger)
            {
                InnerHandler = innerHandler
            };

            return new HttpClient(telemetryHandler);
        }

        /// <summary>
        /// Creates a <see cref="TelemetryHttpMessageHandler"/> configured with the specified options.
        /// Useful when you want to compose the handler into an existing pipeline.
        /// </summary>
        /// <param name="options">
        /// Optional instrumentation options. When <see langword="null"/>,
        /// <see cref="HttpInstrumentationOptions.Default"/> is used.
        /// </param>
        /// <param name="innerHandler">
        /// Optional inner handler. When <see langword="null"/>,
        /// a new <see cref="HttpClientHandler"/> is created.
        /// </param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        /// <returns>A new <see cref="TelemetryHttpMessageHandler"/>.</returns>
        public static TelemetryHttpMessageHandler CreateHandler(
            HttpInstrumentationOptions? options = null,
            HttpMessageHandler? innerHandler = null,
            ILogger<TelemetryHttpMessageHandler>? logger = null)
        {
            var effectiveOptions = options ?? HttpInstrumentationOptions.Default;

            var handler = new TelemetryHttpMessageHandler(effectiveOptions, logger)
            {
                InnerHandler = innerHandler ?? new HttpClientHandler()
            };

            return handler;
        }
    }
}
