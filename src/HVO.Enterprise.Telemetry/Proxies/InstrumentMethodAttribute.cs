using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Marks an interface method for automatic telemetry instrumentation via DispatchProxy.
    /// When applied to a method on an interface that is wrapped by <see cref="TelemetryDispatchProxy{T}"/>,
    /// the proxy will automatically create an operation scope around the method invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InstrumentMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the operation name. Defaults to "{InterfaceName}.{MethodName}" if not specified.
        /// </summary>
        public string? OperationName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Diagnostics.ActivityKind"/> for the operation.
        /// Defaults to <see cref="ActivityKind.Internal"/>.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;

        /// <summary>
        /// Gets or sets whether to capture method parameters as operation tags.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureParameters { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture the return value as an operation tag.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool CaptureReturnValue { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log method entry/exit events.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool LogEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level for method events.
        /// Defaults to <see cref="LogLevel.Debug"/>.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
    }
}
