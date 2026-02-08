using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Manages enrichment of telemetry with contextual information.
    /// </summary>
    public interface IContextEnricher
    {
        /// <summary>
        /// Enriches the current Activity with context from all configured providers.
        /// </summary>
        /// <param name="activity">Activity to enrich.</param>
        void EnrichActivity(Activity activity);

        /// <summary>
        /// Enriches a dictionary with context from all configured providers.
        /// </summary>
        /// <param name="properties">Properties to enrich.</param>
        void EnrichProperties(IDictionary<string, object> properties);

        /// <summary>
        /// Registers a context provider.
        /// </summary>
        /// <param name="provider">Provider to register.</param>
        void RegisterProvider(IContextProvider provider);
    }
}
