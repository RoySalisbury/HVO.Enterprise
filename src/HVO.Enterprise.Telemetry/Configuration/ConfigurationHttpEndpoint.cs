using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// HTTP endpoint for runtime configuration updates.
    /// </summary>
    public sealed class ConfigurationHttpEndpoint : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly ILogger<ConfigurationHttpEndpoint> _logger;
        private readonly Func<string, bool>? _authenticator;
        private readonly CancellationTokenSource _shutdownCts;
        private TelemetryOptions _currentOptions;
        private volatile bool _disposed;

        /// <summary>
        /// Raised when configuration is updated via HTTP.
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHttpEndpoint"/> class.
        /// </summary>
        /// <param name="prefix">HttpListener prefix (for example, http://localhost:5000/).</param>
        /// <param name="initialOptions">Initial configuration.</param>
        /// <param name="authenticator">Optional authentication callback. Receives Authorization header.</param>
        /// <param name="logger">Optional logger.</param>
        public ConfigurationHttpEndpoint(
            string prefix,
            TelemetryOptions initialOptions,
            Func<string, bool>? authenticator = null,
            ILogger<ConfigurationHttpEndpoint>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));

            _currentOptions = initialOptions ?? throw new ArgumentNullException(nameof(initialOptions));
            _authenticator = authenticator;
            _logger = logger ?? NullLogger<ConfigurationHttpEndpoint>.Instance;
            _shutdownCts = new CancellationTokenSource();

            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
        }

        /// <summary>
        /// Starts the HTTP endpoint.
        /// </summary>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConfigurationHttpEndpoint));

            _listener.Start();
            _ = ProcessRequestsAsync(_shutdownCts.Token);

            _logger.LogInformation("Configuration HTTP endpoint started on {Prefix}",
                string.Join(", ", _listener.Prefixes));
        }

        private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            while (_listener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = HandleRequestAsync(context, cancellationToken);
                }
                catch (HttpListenerException) when (_disposed || cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_disposed)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing HTTP request");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (!Authorize(context))
                {
                    context.Response.StatusCode = 401;
                    await WriteResponseAsync(context.Response, "Unauthorized", cancellationToken).ConfigureAwait(false);
                    return;
                }

                var path = context.Request.Url?.AbsolutePath ?? string.Empty;

                if (string.Equals(path, "/telemetry/config", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleGetConfigurationAsync(context, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(path, "/telemetry/config", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleUpdateConfigurationAsync(context, cancellationToken).ConfigureAwait(false);
                    return;
                }

                context.Response.StatusCode = 404;
                await WriteResponseAsync(context.Response, "Not found", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling HTTP request");
                context.Response.StatusCode = 500;
                await WriteResponseAsync(context.Response, "Internal server error", cancellationToken).ConfigureAwait(false);
            }
        }

        private bool Authorize(HttpListenerContext context)
        {
            if (_authenticator == null)
                return true;

            var authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
                return false;

            return _authenticator(authHeader);
        }

        private async Task HandleGetConfigurationAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var options = Volatile.Read(ref _currentOptions);
            var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await WriteResponseAsync(context.Response, json, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleUpdateConfigurationAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var json = await reader.ReadToEndAsync().ConfigureAwait(false);

            try
            {
                var newOptions = JsonSerializer.Deserialize<TelemetryOptions>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (newOptions == null)
                    throw new InvalidOperationException("Invalid configuration JSON");

                newOptions.Validate();

                var oldOptions = Volatile.Read(ref _currentOptions);
                Volatile.Write(ref _currentOptions, newOptions);

                _logger.LogInformation("Configuration updated via HTTP endpoint");
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(oldOptions, newOptions));

                context.Response.StatusCode = 200;
                await WriteResponseAsync(context.Response, "Configuration updated successfully", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update configuration");
                context.Response.StatusCode = 400;
                await WriteResponseAsync(context.Response, "Invalid configuration: " + ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task WriteResponseAsync(
            HttpListenerResponse response,
            string message,
            CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            response.OutputStream.Close();
        }

        /// <summary>
        /// Stops the HTTP endpoint and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _shutdownCts.Cancel();
            _listener.Stop();
            _listener.Close();
            _shutdownCts.Dispose();

            _logger.LogInformation("Configuration HTTP endpoint stopped");
        }
    }
}
