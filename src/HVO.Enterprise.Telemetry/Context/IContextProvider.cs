using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Provides contextual information for telemetry enrichment.
    /// </summary>
    public interface IContextProvider
    {
        /// <summary>
        /// Gets the provider name for configuration and filtering.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the enrichment level this provider operates at.
        /// </summary>
        EnrichmentLevel Level { get; }

        /// <summary>
        /// Attempts to enrich the activity with context.
        /// </summary>
        /// <param name="activity">Activity to enrich.</param>
        /// <param name="options">Enrichment options.</param>
        void EnrichActivity(Activity activity, EnrichmentOptions options);

        /// <summary>
        /// Attempts to enrich the properties dictionary with context.
        /// </summary>
        /// <param name="properties">Properties to enrich.</param>
        /// <param name="options">Enrichment options.</param>
        void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options);
    }
}
