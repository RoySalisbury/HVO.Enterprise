using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Monitors telemetry configuration file for changes and reloads automatically.
    /// </summary>
    public sealed class FileConfigurationReloader : IDisposable
    {
        private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(50);
        private const int DefaultMaxReadRetries = 3;

        private readonly string _configFilePath;
        private readonly ILogger<FileConfigurationReloader> _logger;
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _debounceTimer;
        private readonly TimeSpan _debounceDelay;
        private readonly int _maxReadRetries;
        private readonly TimeSpan _retryDelay;
        private TelemetryOptions _currentOptions;
        private volatile bool _disposed;

        /// <summary>
        /// Raised when configuration changes and is successfully reloaded.
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileConfigurationReloader"/> class.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public FileConfigurationReloader(string configFilePath, ILogger<FileConfigurationReloader>? logger = null)
            : this(configFilePath, logger, DefaultDebounceDelay, DefaultMaxReadRetries, DefaultRetryDelay)
        {
        }

        internal FileConfigurationReloader(
            string configFilePath,
            ILogger<FileConfigurationReloader>? logger,
            TimeSpan debounceDelay,
            int maxReadRetries,
            TimeSpan retryDelay)
        {
            if (string.IsNullOrWhiteSpace(configFilePath))
                throw new ArgumentNullException(nameof(configFilePath));

            if (debounceDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(debounceDelay), "Debounce delay must be non-negative.");

            if (maxReadRetries <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxReadRetries), "Max read retries must be greater than zero.");

            if (retryDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be non-negative.");

            _configFilePath = configFilePath;
            _logger = logger ?? NullLogger<FileConfigurationReloader>.Instance;
            _debounceDelay = debounceDelay;
            _maxReadRetries = maxReadRetries;
            _retryDelay = retryDelay;

            _currentOptions = LoadConfiguration();

            var directory = Path.GetDirectoryName(_configFilePath);
            var fileName = Path.GetFileName(_configFilePath);

            _watcher = new FileSystemWatcher(directory ?? ".")
            {
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;

            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("Configuration file watcher started for {FilePath}", _configFilePath);
        }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public TelemetryOptions CurrentOptions => Volatile.Read(ref _currentOptions);

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_disposed)
                return;

            _logger.LogDebug("Configuration file changed: {ChangeType}", e.ChangeType);
            _debounceTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
        }

        private void OnDebounceTimerElapsed(object? state)
        {
            if (_disposed)
                return;

            try
            {
                var newOptions = LoadConfiguration();
                newOptions.Validate();

                var oldOptions = Volatile.Read(ref _currentOptions);
                Volatile.Write(ref _currentOptions, newOptions);

                _logger.LogInformation("Configuration reloaded successfully from {FilePath}", _configFilePath);
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(oldOptions, newOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to reload configuration from {FilePath}. Keeping previous configuration.",
                    _configFilePath);
            }
        }

        private TelemetryOptions LoadConfiguration()
        {
            for (int i = 0; i < _maxReadRetries; i++)
            {
                try
                {
                    var json = File.ReadAllText(_configFilePath);
                    var options = JsonSerializer.Deserialize<TelemetryOptions>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });

                    if (options == null)
                        throw new InvalidOperationException("Configuration JSON deserialized to null.");

                    return options;
                }
                catch (IOException) when (i < _maxReadRetries - 1)
                {
                    Thread.Sleep(_retryDelay);
                }
            }

            _logger.LogWarning("Failed to load configuration from {FilePath}. Using defaults.", _configFilePath);
            return new TelemetryOptions();
        }

        /// <summary>
        /// Stops file watching and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _watcher.Changed -= OnFileChanged;
            _watcher.Renamed -= OnFileChanged;
            _watcher.Dispose();
            _debounceTimer.Dispose();

            _logger.LogInformation("Configuration file watcher stopped");
        }
    }
}
