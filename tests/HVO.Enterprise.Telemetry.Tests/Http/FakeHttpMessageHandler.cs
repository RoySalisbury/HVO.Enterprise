using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HVO.Enterprise.Telemetry.Tests.Http
{
    /// <summary>
    /// A test double for <see cref="HttpMessageHandler"/> that returns a canned response
    /// or throws a canned exception. Optionally captures the outgoing request for assertions.
    /// </summary>
    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;
        private readonly Action<HttpRequestMessage>? _onRequest;

        /// <summary>
        /// Gets the last <see cref="HttpRequestMessage"/> sent through this handler.
        /// </summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <summary>
        /// Gets the number of times <see cref="SendAsync"/> was invoked.
        /// </summary>
        public int CallCount { get; private set; }

        public FakeHttpMessageHandler(HttpResponseMessage response, Action<HttpRequestMessage>? onRequest = null)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _onRequest = onRequest;
        }

        public FakeHttpMessageHandler(Exception exception)
        {
            _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            CallCount++;
            _onRequest?.Invoke(request);

            if (_exception != null)
                throw _exception;

            return Task.FromResult(_response!);
        }

        /// <summary>
        /// Creates a handler that returns <see cref="HttpStatusCode.OK"/>.
        /// </summary>
        public static FakeHttpMessageHandler Ok(Action<HttpRequestMessage>? onRequest = null)
        {
            return new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK), onRequest);
        }

        /// <summary>
        /// Creates a handler that returns the specified status code.
        /// </summary>
        public static FakeHttpMessageHandler WithStatus(HttpStatusCode statusCode, string? reasonPhrase = null)
        {
            return new FakeHttpMessageHandler(new HttpResponseMessage(statusCode) { ReasonPhrase = reasonPhrase });
        }

        /// <summary>
        /// Creates a handler that throws the specified exception.
        /// </summary>
        public static FakeHttpMessageHandler Throwing(Exception exception)
        {
            return new FakeHttpMessageHandler(exception);
        }
    }
}
