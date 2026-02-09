using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.IIS
{
    /// <summary>
    /// Hosted service that initializes the <see cref="IisLifecycleManager"/> on host startup
    /// and disposes it on host stop.
    /// </summary>
    internal sealed class IisLifecycleManagerHostedService : IHostedService
    {
        private readonly IisLifecycleManager _lifecycleManager;
        private readonly IisExtensionOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisLifecycleManagerHostedService"/> class.
        /// </summary>
        /// <param name="lifecycleManager">The IIS lifecycle manager.</param>
        /// <param name="options">The IIS extension options.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="lifecycleManager"/> is null.</exception>
        public IisLifecycleManagerHostedService(
            IisLifecycleManager lifecycleManager,
            IOptions<IisExtensionOptions> options)
        {
            _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options.Value;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_options.AutoInitialize && !_lifecycleManager.IsInitialized)
            {
                _lifecycleManager.Initialize();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _lifecycleManager.Dispose();
            return Task.CompletedTask;
        }
    }
}
