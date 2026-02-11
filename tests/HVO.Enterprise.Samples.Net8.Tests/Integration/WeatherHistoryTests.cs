using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8;
using HVO.Enterprise.Samples.Net8.Controllers;
using HVO.Enterprise.Samples.Net8.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Samples.Net8.Tests.Integration
{
    /// <summary>
    /// Integration tests for weather history endpoints using WebApplicationFactory.
    /// Verifies EF Core, ADO.NET, caching, and messaging integrations work end-to-end.
    /// </summary>
    [TestClass]
    public class WeatherHistoryTests
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
        public async Task GetRecentReadings_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/weather/history");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task GetRecentReadings_WithLocationFilter_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/weather/history?location=London");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task AddReading_ThenRetrieve_RoundTrips()
        {
            // Add a reading
            var request = new AddWeatherReadingRequest
            {
                Location = "TestCity-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                TemperatureCelsius = 22.5,
                Humidity = 65.0,
                WindSpeedKmh = 10.0,
                Condition = "Clear",
            };

            var postResponse = await _client.PostAsJsonAsync("/api/weather/history", request);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            var created = await postResponse.Content.ReadFromJsonAsync<WeatherReadingEntity>();
            Assert.IsNotNull(created);
            Assert.AreEqual(request.Location, created.Location);
            Assert.AreEqual(request.TemperatureCelsius, created.TemperatureCelsius);

            // Retrieve readings for that location
            var getResponse = await _client.GetAsync($"/api/weather/history?location={request.Location}");
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

            var readings = await getResponse.Content.ReadFromJsonAsync<List<WeatherReadingEntity>>();
            Assert.IsNotNull(readings);
            Assert.IsTrue(readings.Count >= 1);
        }

        [TestMethod]
        public async Task GetAggregate_NoData_Returns404()
        {
            var response = await _client.GetAsync("/api/weather/history/NonExistentCity99/aggregate");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task GetAggregate_WithData_ReturnsOk()
        {
            var location = "AggTestCity-" + Guid.NewGuid().ToString("N").Substring(0, 6);

            // Add some readings first
            for (int i = 0; i < 3; i++)
            {
                var req = new AddWeatherReadingRequest
                {
                    Location = location,
                    TemperatureCelsius = 20.0 + i,
                    Humidity = 50.0,
                    Condition = "Clear",
                };
                var post = await _client.PostAsJsonAsync("/api/weather/history", req);
                Assert.AreEqual(HttpStatusCode.Created, post.StatusCode);
            }

            var response = await _client.GetAsync($"/api/weather/history/{location}/aggregate");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.Contains("averageTemperature") || body.Contains("AverageTemperature"));
        }

        [TestMethod]
        public async Task GetLocationAverages_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/weather/history/averages");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task GetCount_ReturnsBothCounts()
        {
            var response = await _client.GetAsync("/api/weather/history/count");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.Contains("efCoreCount") || body.Contains("EfCoreCount"));
            Assert.IsTrue(body.Contains("adoNetCount") || body.Contains("AdoNetCount"));
        }

        [TestMethod]
        public async Task AddReading_InvalidLocation_ReturnsBadRequest()
        {
            var request = new AddWeatherReadingRequest
            {
                Location = "",
                TemperatureCelsius = 15.0,
            };

            var response = await _client.PostAsJsonAsync("/api/weather/history", request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task GetExtensionDiagnostics_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/weather/extensions");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.Contains("database"));
            Assert.IsTrue(body.Contains("cache"));
            Assert.IsTrue(body.Contains("messaging"));
        }
    }
}
