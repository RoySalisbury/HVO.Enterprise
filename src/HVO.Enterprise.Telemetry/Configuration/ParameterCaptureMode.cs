namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Parameter capture mode for telemetry configuration.
    /// </summary>
    public enum ParameterCaptureMode
    {
        /// <summary>
        /// Use the inherited configuration.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Do not capture parameters.
        /// </summary>
        None = 1,

        /// <summary>
        /// Capture parameter names only.
        /// </summary>
        NamesOnly = 2,

        /// <summary>
        /// Capture names and values (excluding sensitive data).
        /// </summary>
        NamesAndValues = 3,

        /// <summary>
        /// Capture everything including sensitive data.
        /// </summary>
        Full = 4
    }
}
