namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Strategies for redacting sensitive data in captured parameters.
    /// </summary>
    public enum RedactionStrategy
    {
        /// <summary>Remove the parameter entirely from capture.</summary>
        Remove,

        /// <summary>Replace value with a masked placeholder (e.g., "***").</summary>
        Mask,

        /// <summary>Replace value with its hash for correlation without exposure.</summary>
        Hash,

        /// <summary>Show first/last characters only (e.g., "ab***yz").</summary>
        Partial,

        /// <summary>Replace value with its type name.</summary>
        TypeName
    }
}
