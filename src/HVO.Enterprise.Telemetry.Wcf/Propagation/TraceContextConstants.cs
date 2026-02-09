namespace HVO.Enterprise.Telemetry.Wcf.Propagation
{
    /// <summary>
    /// Constants for W3C Trace Context propagation in SOAP headers.
    /// </summary>
    public static class TraceContextConstants
    {
        /// <summary>
        /// W3C traceparent header name.
        /// Format: <c>00-{trace-id}-{parent-id}-{trace-flags}</c>
        /// </summary>
        public const string TraceParentHeaderName = "traceparent";

        /// <summary>
        /// W3C tracestate header name.
        /// </summary>
        public const string TraceStateHeaderName = "tracestate";

        /// <summary>
        /// SOAP namespace for HVO telemetry custom headers.
        /// </summary>
        public const string SoapNamespace = "http://hvo.enterprise/telemetry";

        /// <summary>
        /// ActivitySource name for WCF operations.
        /// </summary>
        public const string ActivitySourceName = "HVO.Enterprise.Telemetry.Wcf";

        /// <summary>
        /// W3C traceparent version prefix.
        /// </summary>
        public const string TraceParentVersion = "00";

        /// <summary>
        /// Expected number of parts in a traceparent header (version-traceId-spanId-flags).
        /// </summary>
        internal const int TraceParentPartCount = 4;

        /// <summary>
        /// Expected length of a trace ID hex string (32 characters).
        /// </summary>
        internal const int TraceIdHexLength = 32;

        /// <summary>
        /// Expected length of a span ID hex string (16 characters).
        /// </summary>
        internal const int SpanIdHexLength = 16;

        /// <summary>
        /// Expected length of trace flags hex string (2 characters).
        /// </summary>
        internal const int TraceFlagsHexLength = 2;
    }
}
