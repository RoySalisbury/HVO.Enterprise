namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Defines parameter capture verbosity levels controlling how much detail
    /// is captured for method parameters and return values.
    /// </summary>
    public enum CaptureLevel
    {
        /// <summary>No parameter capture. Zero overhead.</summary>
        None = 0,

        /// <summary>Capture only primitive types and strings.</summary>
        Minimal = 1,

        /// <summary>Capture primitives, strings, and simple collections.</summary>
        Standard = 2,

        /// <summary>Capture complex objects with property traversal.</summary>
        Verbose = 3
    }
}
