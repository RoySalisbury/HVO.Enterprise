namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Options for configuring proxy instrumentation behavior including
    /// parameter capture depth, collection limits, and PII detection.
    /// </summary>
    public sealed class InstrumentationOptions
    {
        /// <summary>
        /// Gets or sets the maximum depth for capturing nested object properties.
        /// Deeper nesting is represented by the type name only.
        /// Defaults to 2.
        /// </summary>
        public int MaxCaptureDepth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum number of items to capture from collections.
        /// Defaults to 10.
        /// </summary>
        public int MaxCollectionItems { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to capture complex types by serializing their public properties.
        /// When <c>false</c>, complex types are captured as their <see cref="object.ToString"/> result.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureComplexTypes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically detect and redact common PII patterns
        /// (e.g., fields named "password", "token", "ssn").
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool AutoDetectPii { get; set; } = true;
    }
}
