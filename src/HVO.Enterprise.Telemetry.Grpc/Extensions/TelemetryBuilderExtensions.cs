using System;

namespace HVO.Enterprise.Telemetry.Grpc.Extensions
{
    /// <summary>
    /// Extension methods for integrating gRPC telemetry with the
    /// <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds gRPC interceptor instrumentation for automatic tracing of gRPC calls.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="GrpcTelemetryOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> is null.
        /// </exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =&gt;
        /// {
        ///     builder.WithGrpcInstrumentation(options =&gt;
        ///     {
        ///         options.SuppressHealthChecks = true;
        ///         options.RecordMessageSize = false;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithGrpcInstrumentation(
            this TelemetryBuilder builder,
            Action<GrpcTelemetryOptions>? configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddGrpcTelemetry(configure);
            return builder;
        }
    }
}
