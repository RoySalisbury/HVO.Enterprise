namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Defines enrichment levels for controlling overhead.
    /// </summary>
    public enum EnrichmentLevel
    {
        /// <summary>
        /// Essential context only.
        /// </summary>
        Minimal = 0,

        /// <summary>
        /// Standard context (user, request basics).
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Verbose context (headers, environment, custom).
        /// </summary>
        Verbose = 2
    }
}
