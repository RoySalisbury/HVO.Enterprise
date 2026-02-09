using System;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Instrumentation;
using RabbitMQ.Client;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IModel"/> to add telemetry instrumentation.
    /// </summary>
    public static class ModelExtensions
    {
        /// <summary>
        /// Wraps an <see cref="IModel"/> with telemetry instrumentation.
        /// The returned <see cref="TelemetryModel"/> creates OpenTelemetry activities
        /// for publish and consume operations with W3C TraceContext propagation.
        /// </summary>
        /// <param name="model">The RabbitMQ model to wrap.</param>
        /// <param name="options">Optional telemetry options. Defaults used if null.</param>
        /// <returns>A <see cref="TelemetryModel"/> wrapping the original model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
        /// <example>
        /// <code>
        /// var channel = connection.CreateModel();
        /// var telemetryModel = channel.WithTelemetry();
        /// telemetryModel.BasicPublish("exchange", "routingKey", false, null, body);
        /// </code>
        /// </example>
        public static TelemetryModel WithTelemetry(
            this IModel model,
            RabbitMqTelemetryOptions? options = null)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new TelemetryModel(model, options);
        }
    }
}
