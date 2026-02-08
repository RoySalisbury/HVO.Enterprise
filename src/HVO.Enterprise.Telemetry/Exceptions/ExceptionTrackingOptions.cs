namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Configures how exception details are captured for telemetry.
    /// </summary>
    public sealed class ExceptionTrackingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether exception messages are captured.
        /// </summary>
        public bool CaptureMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exception stack traces are captured.
        /// </summary>
        public bool CaptureStackTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the activity status description uses the exception message.
        /// </summary>
        public bool IncludeMessageInActivityStatus { get; set; }
    }
}
