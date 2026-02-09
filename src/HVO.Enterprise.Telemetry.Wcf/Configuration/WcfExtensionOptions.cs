using System;

namespace HVO.Enterprise.Telemetry.Wcf.Configuration
{
    /// <summary>
    /// Configuration options for the WCF telemetry extension.
    /// </summary>
    public sealed class WcfExtensionOptions
    {
        /// <summary>
        /// Gets or sets whether to propagate trace context in reply messages.
        /// Default: <c>true</c>.
        /// </summary>
        /// <remarks>
        /// When enabled, the server-side dispatch inspector will inject
        /// W3C traceparent and tracestate headers into SOAP reply messages,
        /// allowing the client to correlate its Activity with the server operation.
        /// </remarks>
        public bool PropagateTraceContextInReply { get; set; } = true;

        /// <summary>
        /// Gets or sets a filter to determine which operations to trace.
        /// Default: <c>null</c> (trace all operations).
        /// </summary>
        /// <remarks>
        /// The function receives the SOAP action or operation name and returns
        /// <c>true</c> to trace the operation or <c>false</c> to skip it.
        /// Common use: skip health check or ping operations.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.OperationFilter = operationName =&gt;
        ///     !operationName.Contains("Health") &amp;&amp;
        ///     !operationName.EndsWith("Ping");
        /// </code>
        /// </example>
        public Func<string, bool>? OperationFilter { get; set; }

        /// <summary>
        /// Gets or sets whether to record message bodies in Activity tags.
        /// Default: <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>WARNING: Message bodies may contain PII or sensitive data.</para>
        /// <para>
        /// Enable this only for debugging purposes and ensure appropriate
        /// data handling policies are in place. Consider using
        /// <see cref="MaxMessageBodySize"/> to limit capture size.
        /// </para>
        /// </remarks>
        public bool RecordMessageBodies { get; set; }

        /// <summary>
        /// Gets or sets the maximum message body size (in bytes) to record when
        /// <see cref="RecordMessageBodies"/> is <c>true</c>.
        /// Default: 4096 bytes.
        /// </summary>
        /// <remarks>
        /// Must be between 0 and 1,048,576 (1 MB). Set to 0 to disable body recording
        /// even when <see cref="RecordMessageBodies"/> is <c>true</c>.
        /// </remarks>
        public int MaxMessageBodySize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the custom SOAP namespace for trace context headers.
        /// Default: <see cref="Propagation.TraceContextConstants.SoapNamespace"/>.
        /// </summary>
        /// <remarks>
        /// Override this only when interoperating with non-HVO systems that
        /// expect trace context headers in a different namespace.
        /// </remarks>
        public string? CustomSoapNamespace { get; set; }

        /// <summary>
        /// Gets or sets whether to capture fault details in Activity tags when
        /// a WCF fault is detected. Default: <c>true</c>.
        /// </summary>
        public bool CaptureFaultDetails { get; set; } = true;

        /// <summary>
        /// Validates the options and throws if any values are invalid.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="MaxMessageBodySize"/> is negative or exceeds 1 MB.
        /// </exception>
        internal void Validate()
        {
            if (MaxMessageBodySize < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxMessageBodySize), MaxMessageBodySize,
                    "MaxMessageBodySize cannot be negative.");

            if (MaxMessageBodySize > 1_048_576)
                throw new ArgumentOutOfRangeException(nameof(MaxMessageBodySize), MaxMessageBodySize,
                    "MaxMessageBodySize cannot exceed 1,048,576 bytes (1 MB).");
        }
    }
}
