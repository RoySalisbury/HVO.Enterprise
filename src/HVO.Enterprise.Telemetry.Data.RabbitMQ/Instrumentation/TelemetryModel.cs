using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Data.Common;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration;
using RabbitMQ.Client;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Instrumentation
{
    /// <summary>
    /// Wraps an <see cref="IModel"/> to add telemetry to publish and basic consume operations.
    /// Creates <see cref="Activity"/> spans following OpenTelemetry messaging semantic conventions.
    /// </summary>
    public sealed class TelemetryModel : IDisposable
    {
        private readonly IModel _innerModel;
        private readonly RabbitMqTelemetryOptions _options;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryModel"/> class.
        /// </summary>
        /// <param name="innerModel">The inner RabbitMQ model to wrap.</param>
        /// <param name="options">Telemetry options. Defaults used if null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerModel"/> is null.</exception>
        public TelemetryModel(IModel innerModel, RabbitMqTelemetryOptions? options = null)
        {
            _innerModel = innerModel ?? throw new ArgumentNullException(nameof(innerModel));
            _options = options ?? new RabbitMqTelemetryOptions();
        }

        /// <summary>
        /// Gets the inner <see cref="IModel"/> being wrapped.
        /// </summary>
        public IModel InnerModel => _innerModel;

        /// <summary>
        /// Publishes a message with telemetry instrumentation.
        /// Creates a "publish" activity, injects trace context into message headers,
        /// and enriches the activity with OpenTelemetry semantic convention tags.
        /// </summary>
        /// <param name="exchange">The exchange to publish to.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="mandatory">Whether the message is mandatory.</param>
        /// <param name="basicProperties">The message basic properties.</param>
        /// <param name="body">The message body.</param>
        public void BasicPublish(
            string exchange,
            string routingKey,
            bool mandatory,
            IBasicProperties? basicProperties,
            ReadOnlyMemory<byte> body)
        {
            string destinationName = !string.IsNullOrEmpty(exchange)
                ? exchange
                : routingKey;

            string activityName = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} publish",
                destinationName);

            using (var activity = RabbitMqActivitySource.Source.StartActivity(
                activityName,
                ActivityKind.Producer))
            {
                if (activity != null)
                {
                    EnrichActivity(activity, "publish", exchange, routingKey, body.Length, basicProperties);
                }

                // Inject trace context into headers
                if (_options.PropagateTraceContext)
                {
                    if (basicProperties == null)
                    {
                        basicProperties = _innerModel.CreateBasicProperties();
                    }

                    basicProperties.Headers = RabbitMqHeaderPropagator.Inject(
                        basicProperties.Headers,
                        activity);
                }

                try
                {
                    _innerModel.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
                }
                catch (Exception ex)
                {
                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                        activity.SetTag("error.type", ex.GetType().FullName);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a telemetry activity for a consume/process operation.
        /// The caller is responsible for disposing the returned activity.
        /// </summary>
        /// <param name="queue">The queue name being consumed from.</param>
        /// <param name="basicProperties">The received message properties.</param>
        /// <param name="bodyLength">The message body length in bytes.</param>
        /// <returns>
        /// An <see cref="Activity"/> representing the consume operation, or null if not sampled.
        /// </returns>
        public Activity? StartConsumeActivity(
            string queue,
            IBasicProperties? basicProperties,
            int bodyLength)
        {
            // Extract parent context from message headers
            ActivityContext parentContext = default;
            if (_options.PropagateTraceContext && basicProperties?.Headers != null)
            {
                parentContext = RabbitMqHeaderPropagator.Extract(basicProperties.Headers);
            }

            string activityName = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} process",
                queue);

            Activity? activity;
            if (parentContext != default)
            {
                activity = RabbitMqActivitySource.Source.StartActivity(
                    activityName,
                    ActivityKind.Consumer,
                    parentContext);
            }
            else
            {
                activity = RabbitMqActivitySource.Source.StartActivity(
                    activityName,
                    ActivityKind.Consumer);
            }

            if (activity != null)
            {
                string exchange = basicProperties?.Headers != null
                    && basicProperties.Headers.TryGetValue("x-first-death-exchange", out object? exchangeObj)
                    && exchangeObj != null
                        ? exchangeObj.ToString()!
                        : string.Empty;

                EnrichActivity(activity, "process", exchange, string.Empty, bodyLength, basicProperties);

                if (_options.RecordQueueName)
                {
                    activity.SetTag(DataActivityTags.MessagingDestinationName, queue);
                }
            }

            return activity;
        }

        /// <summary>
        /// Enriches an activity with standard messaging tags.
        /// </summary>
        private void EnrichActivity(
            Activity activity,
            string operation,
            string exchange,
            string routingKey,
            int bodyLength,
            IBasicProperties? properties)
        {
            activity.SetTag(DataActivityTags.MessagingSystem, DataActivityTags.SystemRabbitMq);
            activity.SetTag(DataActivityTags.MessagingOperation, operation);

            if (_options.RecordExchange && !string.IsNullOrEmpty(exchange))
            {
                activity.SetTag(DataActivityTags.MessagingDestinationName, exchange);
            }

            if (_options.RecordRoutingKey && !string.IsNullOrEmpty(routingKey))
            {
                activity.SetTag(DataActivityTags.MessagingRabbitMqRoutingKey, routingKey);
            }

            if (_options.RecordBodySize)
            {
                activity.SetTag(DataActivityTags.MessagingMessageBodySize, bodyLength);
            }

            if (_options.RecordMessageIds && properties != null)
            {
                if (!string.IsNullOrEmpty(properties.MessageId))
                {
                    activity.SetTag(DataActivityTags.MessagingMessageId, properties.MessageId);
                }

                if (!string.IsNullOrEmpty(properties.CorrelationId))
                {
                    activity.SetTag(DataActivityTags.MessagingMessageConversationId, properties.CorrelationId);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                // We do NOT dispose the inner model;
                // ownership remains with the caller.
            }
        }
    }
}
