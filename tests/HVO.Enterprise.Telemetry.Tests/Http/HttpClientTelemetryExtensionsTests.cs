using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using HVO.Enterprise.Telemetry.Http;

namespace HVO.Enterprise.Telemetry.Tests.Http
{
    [TestClass]
    public class HttpClientTelemetryExtensionsTests
    {
        [TestMethod]
        public void CreateWithTelemetry_ReturnsHttpClient()
        {
            using var client = HttpClientTelemetryExtensions.CreateWithTelemetry();

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void CreateWithTelemetry_WithCustomOptions_UsesOptions()
        {
            var options = new HttpInstrumentationOptions
            {
                RedactQueryStrings = false,
                CaptureRequestHeaders = true
            };

            using var client = HttpClientTelemetryExtensions.CreateWithTelemetry(options);

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void CreateWithTelemetry_WithCustomInnerHandler_UsesHandler()
        {
            var inner = FakeHttpMessageHandler.Ok();

            using var client = HttpClientTelemetryExtensions.CreateWithTelemetry(
                innerHandler: inner);

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void CreateHandler_ReturnsHandler_WithInnerSet()
        {
            using var handler = HttpClientTelemetryExtensions.CreateHandler();

            Assert.IsNotNull(handler);
            Assert.IsNotNull(handler.InnerHandler);
        }

        [TestMethod]
        public void CreateHandler_WithCustomInner_SetsInnerHandler()
        {
            var inner = FakeHttpMessageHandler.Ok();

            using var handler = HttpClientTelemetryExtensions.CreateHandler(innerHandler: inner);

            Assert.AreSame(inner, handler.InnerHandler);
        }

        [TestMethod]
        public void CreateHandler_NullOptions_UsesDefaults()
        {
            using var handler = HttpClientTelemetryExtensions.CreateHandler(options: null);

            Assert.IsNotNull(handler);
        }
    }
}
