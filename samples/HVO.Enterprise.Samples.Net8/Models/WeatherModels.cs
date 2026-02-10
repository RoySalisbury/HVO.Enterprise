using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HVO.Enterprise.Samples.Net8.Models
{
    // ────────────────────────────────────────────────────────────────
    // Domain Models
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A weather observation at a specific location and time.
    /// </summary>
    public sealed record WeatherObservation
    {
        public required string LocationName { get; init; }
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
        public required DateTimeOffset ObservedAt { get; init; }
        public required double TemperatureCelsius { get; init; }
        public required double WindSpeedKmh { get; init; }
        public required int RelativeHumidity { get; init; }
        public required int WeatherCode { get; init; }
        public string WeatherDescription => DecodeWeatherCode(WeatherCode);

        private static string DecodeWeatherCode(int code) => code switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snow",
            80 or 81 or 82 => "Rain showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => $"Unknown ({code})"
        };
    }

    /// <summary>
    /// Summary of weather data collected across multiple locations.
    /// </summary>
    public sealed record WeatherSummary
    {
        public required int LocationCount { get; init; }
        public required double AverageTemperature { get; init; }
        public required double MinTemperature { get; init; }
        public required double MaxTemperature { get; init; }
        public required double AverageWindSpeed { get; init; }
        public required DateTimeOffset CollectedAt { get; init; }
        public required IReadOnlyList<WeatherObservation> Observations { get; init; }
    }

    /// <summary>
    /// Represents a named location for weather monitoring.
    /// </summary>
    public sealed record MonitoredLocation(string Name, double Latitude, double Longitude);

    /// <summary>
    /// Alert raised when weather conditions exceed thresholds.
    /// </summary>
    public sealed record WeatherAlert
    {
        public required string AlertId { get; init; }
        public required string LocationName { get; init; }
        public required string AlertType { get; init; }
        public required string Message { get; init; }
        public required string Severity { get; init; }
        public required DateTimeOffset IssuedAt { get; init; }
    }

    // ────────────────────────────────────────────────────────────────
    // Open-Meteo API Response DTOs
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Root response from the Open-Meteo current weather API.
    /// </summary>
    public sealed class OpenMeteoResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("current_weather")]
        public OpenMeteoCurrentWeather? CurrentWeather { get; set; }
    }

    /// <summary>
    /// The "current_weather" block from Open-Meteo.
    /// </summary>
    public sealed class OpenMeteoCurrentWeather
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("windspeed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("weathercode")]
        public int WeatherCode { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }
    }

    // ────────────────────────────────────────────────────────────────
    // API Request / Response Models
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Request to add a new monitored location.
    /// </summary>
    public sealed record AddLocationRequest(string Name, double Latitude, double Longitude);

    /// <summary>
    /// Telemetry diagnostics response.
    /// </summary>
    public sealed record TelemetryDiagnosticsResponse
    {
        public required long ActivitiesCreated { get; init; }
        public required long ActivitiesCompleted { get; init; }
        public required long ActiveActivities { get; init; }
        public required long ExceptionsTracked { get; init; }
        public required long EventsRecorded { get; init; }
        public required long MetricsRecorded { get; init; }
        public required int QueueDepth { get; init; }
        public required long ItemsProcessed { get; init; }
        public required long ItemsDropped { get; init; }
        public required double AverageProcessingTimeMs { get; init; }
        public required double CurrentErrorRate { get; init; }
        public required double CurrentThroughput { get; init; }
        public required TimeSpan Uptime { get; init; }
    }
}
