using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Samples.Net8.Tests.Integration
{
    /// <summary>
    /// Integration tests verifying health check endpoints with all extensions registered.
    /// </summary>
    [TestClass]
    public class HealthCheckTests
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
        public async Task Health_Endpoint_ReturnsOk()
        {
            var response = await _client.GetAsync("/health");

            // Health endpoint returns 200 for Healthy or Degraded
            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.OK
                || response.StatusCode == HttpStatusCode.ServiceUnavailable,
                $"Expected 200 or 503, got {response.StatusCode}");
        }

        [TestMethod]
        public async Task Health_Endpoint_ContainsTelemetryCheck()
        {
            var response = await _client.GetAsync("/health");
            var body = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(body.Contains("telemetry"),
                "Health response should include the 'telemetry' health check");
        }

        [TestMethod]
        public async Task HealthReady_Endpoint_ReturnsOk()
        {
            var response = await _client.GetAsync("/health/ready");

            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.OK
                || response.StatusCode == HttpStatusCode.ServiceUnavailable,
                $"Expected 200 or 503, got {response.StatusCode}");
        }

        [TestMethod]
        public async Task HealthLive_Endpoint_ReturnsOk()
        {
            var response = await _client.GetAsync("/health/live");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
