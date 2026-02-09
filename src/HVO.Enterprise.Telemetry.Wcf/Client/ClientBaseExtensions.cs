using System;
using System.ServiceModel.Description;
using HVO.Enterprise.Telemetry.Wcf.Configuration;

namespace HVO.Enterprise.Telemetry.Wcf.Client
{
    /// <summary>
    /// Extension methods for adding WCF telemetry to client endpoints.
    /// </summary>
    public static class ClientBaseExtensions
    {
        /// <summary>
        /// Adds telemetry behavior to a WCF <see cref="ServiceEndpoint"/> for automatic
        /// distributed tracing of client operations.
        /// </summary>
        /// <param name="endpoint">The service endpoint to instrument.</param>
        /// <param name="options">Optional WCF extension options.</param>
        /// <returns>The service endpoint for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="endpoint"/> is null.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This is the primary way to add telemetry to WCF client channels.
        /// Works with both <c>ChannelFactory&lt;T&gt;</c> and generated client proxies
        /// inheriting from <c>ClientBase&lt;T&gt;</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // With ChannelFactory&lt;T&gt;
        /// var factory = new ChannelFactory&lt;IMyService&gt;(binding, address);
        /// factory.Endpoint.AddTelemetryBehavior();
        /// var client = factory.CreateChannel();
        ///
        /// // With generated client proxy
        /// var client = new MyServiceClient();
        /// client.Endpoint.AddTelemetryBehavior();
        /// </code>
        /// </example>
        public static ServiceEndpoint AddTelemetryBehavior(
            this ServiceEndpoint endpoint,
            WcfExtensionOptions? options = null)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            var behavior = new TelemetryClientEndpointBehavior(options);
            endpoint.EndpointBehaviors.Add(behavior);

            return endpoint;
        }
    }
}
