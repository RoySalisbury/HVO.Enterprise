using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Options for context enrichment.
    /// </summary>
    public sealed class EnrichmentOptions
    {
        /// <summary>
        /// Gets or sets the maximum enrichment level to apply.
        /// </summary>
        public EnrichmentLevel MaxLevel { get; set; } = EnrichmentLevel.Standard;

        /// <summary>
        /// Gets or sets whether to redact PII.
        /// </summary>
        public bool RedactPii { get; set; } = true;

        /// <summary>
        /// Gets or sets the PII redaction strategy.
        /// </summary>
        public PiiRedactionStrategy RedactionStrategy { get; set; } = PiiRedactionStrategy.Mask;

        /// <summary>
        /// Gets or sets headers to exclude from enrichment.
        /// </summary>
        public HashSet<string> ExcludedHeaders { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        /// <summary>
        /// Gets or sets property names that should be treated as PII.
        /// </summary>
        public HashSet<string> PiiProperties { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "email",
            "ssn",
            "creditcard",
            "password",
            "phone",
            "token",
            "apikey"
        };

        /// <summary>
        /// Gets or sets custom environment tags.
        /// </summary>
        public Dictionary<string, string> CustomEnvironmentTags { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Ensures collection defaults are initialized.
        /// </summary>
        internal void EnsureDefaults()
        {
            ExcludedHeaders ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PiiProperties ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CustomEnvironmentTags ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
