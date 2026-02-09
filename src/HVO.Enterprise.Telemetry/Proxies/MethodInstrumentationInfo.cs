using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Cached instrumentation metadata for a single method, built once
    /// from attribute inspection and reused for every subsequent invocation.
    /// </summary>
    internal sealed class MethodInstrumentationInfo
    {
        /// <summary>
        /// Gets or sets whether this method should be instrumented.
        /// When <c>false</c>, the proxy invokes the target directly with zero overhead.
        /// </summary>
        public bool IsInstrumented { get; set; }

        /// <summary>
        /// Gets or sets the operation name used for the scope / activity.
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="System.Diagnostics.ActivityKind"/> for the operation.
        /// </summary>
        public ActivityKind ActivityKind { get; set; }

        /// <summary>
        /// Gets or sets whether to capture method parameters.
        /// </summary>
        public bool CaptureParameters { get; set; }

        /// <summary>
        /// Gets or sets whether to capture the return value.
        /// </summary>
        public bool CaptureReturnValue { get; set; }

        /// <summary>
        /// Gets or sets whether to log method entry/exit.
        /// </summary>
        public bool LogEvents { get; set; }

        /// <summary>
        /// Gets or sets the log level for method events.
        /// </summary>
        public LogLevel LogLevel { get; set; }
    }
}
