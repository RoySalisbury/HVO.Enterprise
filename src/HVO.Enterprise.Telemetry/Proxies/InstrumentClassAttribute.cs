using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Marks an interface for automatic telemetry instrumentation of all public methods.
    /// When applied to an interface wrapped by <see cref="TelemetryDispatchProxy{T}"/>,
    /// every method that does not have a <see cref="NoTelemetryAttribute"/> will be automatically instrumented.
    /// Method-level <see cref="InstrumentMethodAttribute"/> settings override these defaults.
    /// </summary>
    /// <remarks>
    /// This attribute is only inspected on interface types (including inherited interfaces).
    /// The <see cref="TelemetryDispatchProxy{T}"/> uses <c>typeof(T)</c> and its base interfaces
    /// to discover this attribute. Applying it to a concrete class has no effect.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public sealed class InstrumentClassAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the default operation name prefix. Defaults to the interface/class name.
        /// </summary>
        public string? OperationPrefix { get; set; }

        /// <summary>
        /// Gets or sets the default <see cref="System.Diagnostics.ActivityKind"/> for all methods.
        /// Defaults to <see cref="ActivityKind.Internal"/>.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;

        /// <summary>
        /// Gets or sets whether to capture parameters by default for all methods.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureParameters { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log events by default for all methods.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool LogEvents { get; set; } = true;
    }
}
