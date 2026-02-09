using System;
using System.Diagnostics;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using HVO.Enterprise.Telemetry.Wcf.Configuration;

namespace HVO.Enterprise.Telemetry.Wcf.Client
{
    /// <summary>
    /// WCF endpoint behavior that adds <see cref="TelemetryClientMessageInspector"/>
    /// to client channel runtimes for automatic distributed tracing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Add this behavior to a WCF client endpoint to automatically inject W3C Trace Context
    /// into outgoing SOAP headers and create <see cref="Activity"/> instances for each
    /// WCF operation call.
    /// </para>
    /// <para>
    /// This behavior only modifies the client-side runtime. The
    /// <see cref="ApplyDispatchBehavior"/> method is a no-op because server-side
    /// dispatch types are not available in the <c>System.ServiceModel.Primitives</c>
    /// NuGet package. For server-side instrumentation, use the reflection-based
    /// <see cref="Server.WcfServerIntegration"/> class.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Add to an endpoint programmatically
    /// endpoint.EndpointBehaviors.Add(new TelemetryClientEndpointBehavior());
    /// </code>
    /// </example>
    public sealed class TelemetryClientEndpointBehavior : IEndpointBehavior
    {
        private readonly ActivitySource _activitySource;
        private readonly WcfExtensionOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClientEndpointBehavior"/> class
        /// using the shared <see cref="WcfActivitySource"/>.
        /// </summary>
        /// <param name="options">WCF extension options. Uses defaults if <c>null</c>.</param>
        public TelemetryClientEndpointBehavior(WcfExtensionOptions? options = null)
            : this(WcfActivitySource.Instance, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClientEndpointBehavior"/> class
        /// with a custom <see cref="ActivitySource"/>.
        /// </summary>
        /// <param name="activitySource">The <see cref="ActivitySource"/> for creating activities.</param>
        /// <param name="options">WCF extension options. Uses defaults if <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="activitySource"/> is null.
        /// </exception>
        public TelemetryClientEndpointBehavior(
            ActivitySource activitySource,
            WcfExtensionOptions? options = null)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _options = options ?? new WcfExtensionOptions();
        }

        /// <inheritdoc />
        /// <remarks>No binding parameters are required for telemetry.</remarks>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // No binding parameters needed for telemetry
        }

        /// <inheritdoc />
        /// <remarks>
        /// Adds a <see cref="TelemetryClientMessageInspector"/> to the client runtime's
        /// message inspector collection.
        /// </remarks>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            if (clientRuntime == null)
                throw new ArgumentNullException(nameof(clientRuntime));

            var inspector = new TelemetryClientMessageInspector(_activitySource, _options);
            clientRuntime.ClientMessageInspectors.Add(inspector);
        }

        /// <inheritdoc />
        /// <remarks>
        /// No-op for this client-side behavior. Server-side dispatch instrumentation
        /// is handled via <see cref="Server.WcfServerIntegration"/> using reflection.
        /// </remarks>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // Server-side dispatch instrumentation uses reflection via WcfServerIntegration.
            // This behavior is client-side only.
        }

        /// <inheritdoc />
        /// <remarks>No validation is required for the telemetry behavior.</remarks>
        public void Validate(ServiceEndpoint endpoint)
        {
            // No validation needed
        }
    }
}
