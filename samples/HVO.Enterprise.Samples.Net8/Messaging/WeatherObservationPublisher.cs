using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Messaging
{
    /// <summary>
    /// Publishes weather observation events to the <see cref="FakeMessageBus"/>.
    /// Called by the weather collector whenever a new reading is obtained.
    /// </summary>
    public sealed class WeatherObservationPublisher
    {
        private readonly FakeMessageBus _bus;
        private readonly ILogger<WeatherObservationPublisher> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherObservationPublisher"/> class.
        /// </summary>
        /// <param name="bus">The message bus to publish to.</param>
        /// <param name="logger">Logger instance.</param>
        public WeatherObservationPublisher(FakeMessageBus bus, ILogger<WeatherObservationPublisher> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes a weather observation event.
        /// </summary>
        /// <param name="observation">The weather observation data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task PublishObservationAsync(
            WeatherObservationEvent observation,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Publishing weather observation for {Location}: {Temperature}°C",
                observation.Location, observation.TemperatureCelsius);

            await _bus.PublishAsync(observation, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Event published when a weather observation is recorded.
    /// </summary>
    public sealed class WeatherObservationEvent
    {
        /// <summary>Location of the observation.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Temperature in Celsius.</summary>
        public double TemperatureCelsius { get; set; }

        /// <summary>Humidity percentage.</summary>
        public double? Humidity { get; set; }

        /// <summary>Wind speed in km/h.</summary>
        public double? WindSpeedKmh { get; set; }

        /// <summary>Weather condition.</summary>
        public string? Condition { get; set; }

        /// <summary>UTC timestamp of the observation.</summary>
        public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
