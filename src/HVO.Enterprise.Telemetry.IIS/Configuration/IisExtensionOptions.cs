using System;
using System.Threading;
using System.Threading.Tasks;

namespace HVO.Enterprise.Telemetry.IIS.Configuration
{
    /// <summary>
    /// Configuration options for the IIS telemetry extension.
    /// </summary>
    public sealed class IisExtensionOptions
    {
        /// <summary>
        /// Gets or sets the maximum time to wait for telemetry flush during shutdown.
        /// Default: 25 seconds (leaving 5s buffer before IIS 30s timeout).
        /// </summary>
        /// <remarks>
        /// IIS has a default shutdown timeout of 30 seconds. This value should be set
        /// lower than the IIS timeout to allow time for the unregister call and
        /// final cleanup operations.
        /// </remarks>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(25);

        /// <summary>
        /// Gets or sets whether to automatically initialize the IIS lifecycle manager
        /// when registered via DI. Default: <c>true</c>.
        /// </summary>
        public bool AutoInitialize { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to register with IIS <c>HostingEnvironment.RegisterObject</c>
        /// for graceful shutdown notifications. Default: <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Only effective on .NET Framework 4.8+ when System.Web is available at runtime.
        /// On .NET Core/.NET 5+, AppDomain and <c>IHostApplicationLifetime</c> events
        /// are used regardless of this setting.
        /// </remarks>
        public bool RegisterWithHostingEnvironment { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional handler called before shutdown begins.
        /// Use this to perform custom cleanup before telemetry is flushed.
        /// </summary>
        public Func<CancellationToken, Task>? OnPreShutdown { get; set; }

        /// <summary>
        /// Gets or sets an optional handler called after shutdown completes.
        /// Use this to perform post-shutdown actions like logging to an external system.
        /// </summary>
        public Func<CancellationToken, Task>? OnPostShutdown { get; set; }

        /// <summary>
        /// Validates the options and throws if any values are invalid.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="ShutdownTimeout"/> is negative or exceeds 120 seconds.
        /// </exception>
        internal void Validate()
        {
            if (ShutdownTimeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ShutdownTimeout), ShutdownTimeout,
                    "ShutdownTimeout cannot be negative.");

            if (ShutdownTimeout > TimeSpan.FromSeconds(120))
                throw new ArgumentOutOfRangeException(nameof(ShutdownTimeout), ShutdownTimeout,
                    "ShutdownTimeout cannot exceed 120 seconds.");
        }
    }
}
