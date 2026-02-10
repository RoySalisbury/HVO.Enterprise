using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Models;

namespace HVO.Enterprise.Samples.Net8.Services
{
    /// <summary>
    /// Interface for the weather data service, designed for DispatchProxy instrumentation.
    /// All methods are virtual-dispatch friendly (interface-based) so the telemetry proxy
    /// can wrap them with automatic operation scopes, parameter capture, and metrics.
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Fetches current weather for a single location from the Open-Meteo API.
        /// </summary>
        Task<WeatherObservation> GetCurrentWeatherAsync(
            string locationName, double latitude, double longitude,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches weather for all monitored locations and produces a summary.
        /// </summary>
        Task<WeatherSummary> GetWeatherSummaryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the list of locations currently being monitored.
        /// </summary>
        IReadOnlyList<MonitoredLocation> GetMonitoredLocations();

        /// <summary>
        /// Adds a new location to the monitoring list.
        /// </summary>
        void AddMonitoredLocation(string name, double latitude, double longitude);

        /// <summary>
        /// Removes a monitored location by name.
        /// </summary>
        bool RemoveMonitoredLocation(string name);

        /// <summary>
        /// Evaluates the latest observations and returns any weather alerts.
        /// </summary>
        IReadOnlyList<WeatherAlert> EvaluateAlerts(IEnumerable<WeatherObservation> observations);
    }
}
