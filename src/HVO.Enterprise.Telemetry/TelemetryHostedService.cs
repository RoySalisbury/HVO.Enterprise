using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Hosted service that manages the <see cref="TelemetryService"/> lifecycle in DI mode.
    /// Starts the telemetry service when the host starts and shuts it down when the host stops.
    /// Also bridges the DI-resolved instance to the static <see cref="Telemetry"/> API.
    /// </summary>
    internal sealed class TelemetryHostedService : IHostedService
    {
        private readonly TelemetryService _telemetryService;
        private readonly ILogger<TelemetryHostedService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryHostedService"/> class.
        /// </summary>
        /// <param name="telemetryService">The telemetry service instance.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public TelemetryHostedService(
            TelemetryService telemetryService,
            ILogger<TelemetryHostedService> logger)
        {
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting telemetry hosted service");

            _telemetryService.Start();

            // Bridge to static API so Telemetry.StartOperation() etc. work in DI mode
            Telemetry.SetInstance(_telemetryService);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping telemetry hosted service");

            _telemetryService.Shutdown();

            // Clear the static API bridge
            Telemetry.ClearInstance();

            return Task.CompletedTask;
        }
    }
}
