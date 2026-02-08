namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// PII redaction strategies.
    /// </summary>
    public enum PiiRedactionStrategy
    {
        /// <summary>
        /// Remove the property entirely.
        /// </summary>
        Remove = 0,

        /// <summary>
        /// Replace with masked value (e.g., "***").
        /// </summary>
        Mask = 1,

        /// <summary>
        /// Replace with SHA256 hash.
        /// </summary>
        Hash = 2,

        /// <summary>
        /// Keep first/last characters, mask middle.
        /// </summary>
        Partial = 3
    }
}
