using System;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Event published after weather analysis processing completes.
    /// Carries the original observation data enriched with computed analytics
    /// (heat index, wind chill, classification) through the pipeline.
    /// </summary>
    public sealed class WeatherAnalysisEvent
    {
        /// <summary>Location of the original observation.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Original temperature in Celsius.</summary>
        public double TemperatureCelsius { get; set; }

        /// <summary>Original humidity percentage.</summary>
        public double? Humidity { get; set; }

        /// <summary>Original wind speed in km/h.</summary>
        public double? WindSpeedKmh { get; set; }

        /// <summary>Computed heat index (°C). Null when temp &lt; 27°C or humidity unavailable.</summary>
        public double? HeatIndexCelsius { get; set; }

        /// <summary>Computed wind chill (°C). Null when temp &gt; 10°C or wind unavailable.</summary>
        public double? WindChillCelsius { get; set; }

        /// <summary>Comfort classification derived from the observation.</summary>
        public string ComfortClassification { get; set; } = "Unknown";

        /// <summary>Whether any alert threshold was breached.</summary>
        public bool AlertTriggered { get; set; }

        /// <summary>Alert description if <see cref="AlertTriggered"/> is true.</summary>
        public string? AlertDescription { get; set; }

        /// <summary>When the analysis was completed.</summary>
        public DateTime AnalysedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>How long analysis took in milliseconds.</summary>
        public double ProcessingTimeMs { get; set; }

        /// <summary>Number of Leibniz series iterations performed during simulated work.</summary>
        public int PiIterationsComputed { get; set; }

        /// <summary>UTC timestamp of the original observation.</summary>
        public DateTime ObservedAtUtc { get; set; }
    }

    /// <summary>
    /// Event published when the notification dispatcher completes processing.
    /// Represents the final stage of the weather processing pipeline.
    /// </summary>
    public sealed class WeatherNotificationEvent
    {
        /// <summary>Location this notification pertains to.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Notification summary message.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Notification severity: Info, Warning, Critical.</summary>
        public string Severity { get; set; } = "Info";

        /// <summary>Whether an alert was included in the analysis.</summary>
        public bool HasAlert { get; set; }

        /// <summary>Total pipeline processing time from observation to notification (ms).</summary>
        public double TotalPipelineTimeMs { get; set; }

        /// <summary>UTC timestamp when the notification was dispatched.</summary>
        public DateTime DispatchedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the original observation.</summary>
        public DateTime ObservedAtUtc { get; set; }
    }
}
