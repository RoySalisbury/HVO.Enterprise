using System;
using HVO.Enterprise.Telemetry.Wcf.Configuration;

namespace HVO.Enterprise.Telemetry.Wcf.Extensions
{
    /// <summary>
    /// Extension methods for integrating WCF telemetry with the
    /// <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds WCF client telemetry instrumentation to the telemetry builder.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="WcfExtensionOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> is null.
        /// </exception>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =&gt;
        /// {
        ///     builder.WithWcfInstrumentation(options =&gt;
        ///     {
        ///         options.PropagateTraceContextInReply = true;
        ///         options.OperationFilter = op =&gt; !op.Contains("Health");
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithWcfInstrumentation(
            this TelemetryBuilder builder,
            Action<WcfExtensionOptions>? configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddWcfTelemetryInstrumentation(configure);
            return builder;
        }
    }
}
