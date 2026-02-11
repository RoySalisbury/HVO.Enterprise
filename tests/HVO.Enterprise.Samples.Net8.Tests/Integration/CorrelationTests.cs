using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Samples.Net8.Tests.Integration
{
    /// <summary>
    /// Integration tests verifying correlation ID propagation through HTTP headers.
    /// </summary>
    [TestClass]
    public class CorrelationTests
    {
        private static WebApplicationFactory<Program> _factory = null!;
        private static HttpClient _client = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [TestMethod]
        public async Task Request_WithCorrelationHeader_PropagatesCorrelation()
        {
            var correlationId = Guid.NewGuid().ToString();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
            request.Headers.Add("X-Correlation-ID", correlationId);

            var response = await _client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            // Response should echo the correlation ID
            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.Contains(correlationId),
                $"Expected correlation ID '{correlationId}' in response body. Got: {body}");
        }

        [TestMethod]
        public async Task Request_WithoutCorrelationHeader_GeneratesOne()
        {
            var response = await _client.GetAsync("/ping");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.Contains("correlationId"),
                "Response should include a correlationId field");
        }

        [TestMethod]
        public async Task Info_Endpoint_ReturnsExtensionInfo()
        {
            var response = await _client.GetAsync("/info");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.Contains("HVO.Enterprise.Samples.Net8"));
            Assert.IsTrue(body.Contains("telemetryEnabled"));
        }
    }
}
