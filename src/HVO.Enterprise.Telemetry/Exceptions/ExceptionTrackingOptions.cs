namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Configures how exception details are captured for telemetry.
    /// </summary>
    public sealed class ExceptionTrackingOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTrackingOptions"/> class.
        /// </summary>
        /// <param name="captureMessage">Whether to capture exception messages.</param>
        /// <param name="captureStackTrace">Whether to capture exception stack traces.</param>
        /// <param name="includeMessageInActivityStatus">
        /// Whether to use the exception message in the activity status description.
        /// </param>
        public ExceptionTrackingOptions(
            bool captureMessage = false,
            bool captureStackTrace = false,
            bool includeMessageInActivityStatus = false)
        {
            CaptureMessage = captureMessage;
            CaptureStackTrace = captureStackTrace;
            IncludeMessageInActivityStatus = includeMessageInActivityStatus;
        }

        /// <summary>
        /// Gets a value indicating whether exception messages are captured.
        /// </summary>
        public bool CaptureMessage { get; }

        /// <summary>
        /// Gets a value indicating whether exception stack traces are captured.
        /// </summary>
        public bool CaptureStackTrace { get; }

        /// <summary>
        /// Gets a value indicating whether the activity status description uses the exception message.
        /// </summary>
        public bool IncludeMessageInActivityStatus { get; }
    }
}
