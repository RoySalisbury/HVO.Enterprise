using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Monitors <see cref="TelemetryOptions"/> changes from <see cref="IOptionsMonitor{TOptions}"/>.
    /// </summary>
    public sealed class OptionsMonitorConfigurationProvider : IDisposable
    {
        private readonly IOptionsMonitor<TelemetryOptions> _optionsMonitor;
        private readonly ILogger<OptionsMonitorConfigurationProvider> _logger;
        private readonly IDisposable _changeListener = NullDisposable.Instance;
        private TelemetryOptions _currentOptions;

        /// <summary>
        /// Raised when configuration changes.
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsMonitorConfigurationProvider"/> class.
        /// </summary>
        /// <param name="optionsMonitor">Options monitor.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public OptionsMonitorConfigurationProvider(
            IOptionsMonitor<TelemetryOptions> optionsMonitor,
            ILogger<OptionsMonitorConfigurationProvider>? logger = null)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? NullLogger<OptionsMonitorConfigurationProvider>.Instance;

            TelemetryOptions? currentOptions = _optionsMonitor.CurrentValue;
            if (currentOptions == null)
                currentOptions = new TelemetryOptions();

            currentOptions.Validate();
            _currentOptions = currentOptions!;

            _changeListener = _optionsMonitor.OnChange(OnOptionsChanged) ?? NullDisposable.Instance;
        }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public TelemetryOptions CurrentOptions => Volatile.Read(ref _currentOptions);

        private void OnOptionsChanged(TelemetryOptions newOptions)
        {
            try
            {
                newOptions.Validate();

                var oldOptions = Volatile.Read(ref _currentOptions);
                Volatile.Write(ref _currentOptions, newOptions);

                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(oldOptions, newOptions));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ignoring invalid telemetry configuration update from IOptionsMonitor.");
            }
        }

        /// <summary>
        /// Stops monitoring and releases resources.
        /// </summary>
        public void Dispose()
        {
            _changeListener.Dispose();
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();

            private NullDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
