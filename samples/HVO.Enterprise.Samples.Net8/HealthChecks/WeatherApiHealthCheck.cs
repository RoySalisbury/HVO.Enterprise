using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HVO.Enterprise.Samples.Net8.HealthChecks
{
    /// <summary>
    /// Health check that reports on weather API connectivity.
    /// Demonstrates combining external-dependency health checking with telemetry statistics.
    /// </summary>
    public sealed class WeatherApiHealthCheck : IHealthCheck
    {
        private readonly ITelemetryService _telemetry;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherApiHealthCheck(
            ITelemetryService telemetry,
            IHttpClientFactory httpClientFactory)
        {
            _telemetry = telemetry;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ping the Open-Meteo API with minimal data
                var client = _httpClientFactory.CreateClient("OpenMeteo");
                var response = await client.GetAsync(
                    "https://api.open-meteo.com/v1/forecast?latitude=0&longitude=0&current_weather=true",
                    cancellationToken);

                var data = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["status_code"] = (int)response.StatusCode,
                    ["telemetry_enabled"] = _telemetry.IsEnabled,
                };

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Open-Meteo API is reachable.", data);
                }

                return HealthCheckResult.Degraded(
                    $"Open-Meteo returned HTTP {(int)response.StatusCode}.", data: data);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                return HealthCheckResult.Unhealthy(
                    "Cannot reach Open-Meteo API.", ex);
            }
        }
    }
}
