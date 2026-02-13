using System;

namespace HVO.Enterprise.Telemetry.Grpc
{
    /// <summary>
    /// Configuration options for gRPC telemetry instrumentation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls the behavior of <see cref="Server.TelemetryServerInterceptor"/> and
    /// <see cref="Client.TelemetryClientInterceptor"/>, including which calls are
    /// instrumented and what metadata headers are used for correlation propagation.
    /// </para>
    /// <para>
    /// Configure via dependency injection:
    /// </para>
    /// <code>
    /// services.AddGrpcTelemetry(options =&gt;
    /// {
    ///     options.SuppressHealthChecks = true;
    /// });
    /// </code>
    /// </remarks>
    public sealed class GrpcTelemetryOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the server interceptor is enabled.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableServerInterceptor { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the client interceptor is enabled.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableClientInterceptor { get; set; } = true;

        /// <summary>
        /// Gets or sets the correlation ID header name in gRPC metadata.
        /// Default: <c>"x-correlation-id"</c>.
        /// </summary>
        public string CorrelationHeaderName { get; set; } = "x-correlation-id";

        /// <summary>
        /// Gets or sets a value indicating whether to suppress instrumentation
        /// for gRPC health check calls (<c>grpc.health.v1.Health</c>).
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool SuppressHealthChecks { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to suppress instrumentation
        /// for gRPC server reflection calls.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool SuppressReflection { get; set; } = true;
    }
}
