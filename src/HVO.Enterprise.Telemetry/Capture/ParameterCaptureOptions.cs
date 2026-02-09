using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Proxies;

namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Options for configuring parameter capture behavior including verbosity levels,
    /// depth limits, sensitive data detection, and redaction strategies.
    /// </summary>
    public sealed class ParameterCaptureOptions
    {
        /// <summary>
        /// Gets or sets the capture level controlling how much detail is captured.
        /// Defaults to <see cref="CaptureLevel.Standard"/>.
        /// </summary>
        public CaptureLevel Level { get; set; } = CaptureLevel.Standard;

        /// <summary>
        /// Gets or sets whether to automatically detect and redact sensitive data
        /// based on parameter/property naming patterns (e.g., "password", "ssn", "creditCard").
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool AutoDetectSensitiveData { get; set; } = true;

        /// <summary>
        /// Gets or sets the default redaction strategy for sensitive data.
        /// Defaults to <see cref="RedactionStrategy.Mask"/>.
        /// </summary>
        public RedactionStrategy RedactionStrategy { get; set; } = RedactionStrategy.Mask;

        /// <summary>
        /// Gets or sets the maximum depth for traversing nested objects.
        /// Objects deeper than this limit are replaced with a marker string indicating
        /// that the maximum depth was reached (for example, "[Max depth 2 reached]").
        /// Defaults to 2.
        /// </summary>
        public int MaxDepth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum number of items to capture from collections.
        /// Collections exceeding this limit are truncated with a marker.
        /// Defaults to 10.
        /// </summary>
        public int MaxCollectionItems { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum string length before truncation.
        /// Strings longer than this are truncated with a count suffix.
        /// Defaults to 1000.
        /// </summary>
        public int MaxStringLength { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to use custom <see cref="object.ToString()"/> implementations
        /// for types that override it.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool UseCustomToString { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture property names for complex objects in verbose mode.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool CapturePropertyNames { get; set; } = true;

        /// <summary>
        /// Gets or sets custom type serializers. When a type has a registered serializer,
        /// it is used instead of the default property-traversal logic.
        /// </summary>
        public Dictionary<Type, Func<object, object?>>? CustomSerializers { get; set; }

        /// <summary>
        /// Creates a default set of options.
        /// </summary>
        public static ParameterCaptureOptions Default => new ParameterCaptureOptions();
    }
}
