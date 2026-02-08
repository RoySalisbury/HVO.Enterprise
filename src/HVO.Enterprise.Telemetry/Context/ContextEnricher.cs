using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Default implementation of <see cref="IContextEnricher"/>.
    /// </summary>
    public sealed class ContextEnricher : IContextEnricher
    {
        private readonly List<IContextProvider> _providers;
        private readonly EnrichmentOptions _options;
        private readonly ILogger<ContextEnricher> _logger;
        private readonly object _sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextEnricher"/> class.
        /// </summary>
        /// <param name="options">Optional enrichment options.</param>
        /// <param name="logger">Optional logger.</param>
        public ContextEnricher(EnrichmentOptions? options = null, ILogger<ContextEnricher>? logger = null)
        {
            _options = options ?? new EnrichmentOptions();
            _options.EnsureDefaults();
            _logger = logger ?? NullLogger<ContextEnricher>.Instance;
            _providers = new List<IContextProvider>();

            RegisterProvider(new EnvironmentContextProvider());
            RegisterProvider(new UserContextProvider());
            RegisterProvider(new HttpRequestContextProvider());
            RegisterProvider(new WcfRequestContextProvider());
            RegisterProvider(new GrpcRequestContextProvider());
        }

        /// <inheritdoc />
        public void EnrichActivity(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            foreach (var provider in SnapshotProviders().Where(p => p.Level <= _options.MaxLevel))
            {
                try
                {
                    provider.EnrichActivity(activity, _options);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Context enrichment failed for provider {Provider}", provider.Name);
                }
            }
        }

        /// <inheritdoc />
        public void EnrichProperties(IDictionary<string, object> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            foreach (var provider in SnapshotProviders().Where(p => p.Level <= _options.MaxLevel))
            {
                try
                {
                    provider.EnrichProperties(properties, _options);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Context enrichment failed for provider {Provider}", provider.Name);
                }
            }
        }

        /// <inheritdoc />
        public void RegisterProvider(IContextProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_sync)
            {
                _providers.Add(provider);
            }
        }

        private IContextProvider[] SnapshotProviders()
        {
            lock (_sync)
            {
                return _providers.ToArray();
            }
        }
    }
}
