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
        /// Gets or sets whether to capture fault details in Activity tags when
        /// a WCF fault is detected. Default: <c>true</c>.
        /// </summary>
        public bool CaptureFaultDetails { get; set; } = true;
    }
}
