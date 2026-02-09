using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf.Client
{
    /// <summary>
    /// Intercepts outgoing WCF client messages to inject W3C Trace Context
    /// and creates <see cref="Activity"/> instances for distributed tracing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This inspector is added to WCF client channels via
    /// <see cref="TelemetryClientEndpointBehavior"/> and automatically:
    /// </para>
    /// <list type="bullet">
    ///   <item>Creates a client <see cref="Activity"/> before sending the request</item>
    ///   <item>Injects traceparent and tracestate SOAP headers</item>
    ///   <item>Records RPC semantic convention tags</item>
    ///   <item>Captures fault details on error responses</item>
    ///   <item>Stops the <see cref="Activity"/> when the reply is received</item>
    /// </list>
    /// </remarks>
    public sealed class TelemetryClientMessageInspector : IClientMessageInspector
    {
        private readonly ActivitySource _activitySource;
        private readonly WcfExtensionOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClientMessageInspector"/> class.
        /// </summary>
        /// <param name="activitySource">The <see cref="ActivitySource"/> for creating activities.</param>
        /// <param name="options">WCF extension options. Uses defaults if <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="activitySource"/> is null.
        /// </exception>
        public TelemetryClientMessageInspector(
            ActivitySource activitySource,
            WcfExtensionOptions? options = null)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _options = options ?? new WcfExtensionOptions();
        }

        /// <summary>
        /// Called before a request message is sent to the server.
        /// Creates a client <see cref="Activity"/> and injects trace context into SOAP headers.
        /// </summary>
        /// <param name="request">The message to be sent to the service.</param>
        /// <param name="channel">The WCF client object channel.</param>
        /// <returns>
        /// The <see cref="Activity"/> as correlation state, or <c>null</c> if the operation
        /// is not being traced.
        /// </returns>
#pragma warning disable CS8603 // WCF runtime handles null correlation state
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
#pragma warning restore CS8603
        {
            var operationName = GetOperationName(request);

            if (!ShouldTraceOperation(operationName))
                return null!;

            var activity = _activitySource.StartActivity(
                operationName,
                ActivityKind.Client);

            if (activity == null)
                return null!;

            // Set RPC semantic convention tags
            activity.SetTag("rpc.system", "wcf");
            activity.SetTag("rpc.method", operationName);

            // Add endpoint information from the channel
            if (channel?.RemoteAddress != null)
            {
                try
                {
                    activity.SetTag("server.address", channel.RemoteAddress.Uri.Host);
                    activity.SetTag("server.port", channel.RemoteAddress.Uri.Port);
                    activity.SetTag("rpc.service", channel.RemoteAddress.Uri.AbsolutePath);
                }
                catch
                {
                    // Best effort - URI parsing may fail
                }
            }

            // Inject W3C Trace Context into SOAP headers
            var traceparent = W3CTraceContextPropagator.CreateTraceParent(activity);
            SoapHeaderAccessor.AddHeader(request.Headers, TraceContextConstants.TraceParentHeaderName, traceparent);

            var tracestate = W3CTraceContextPropagator.GetTraceState(activity);
            if (!string.IsNullOrEmpty(tracestate))
            {
                SoapHeaderAccessor.AddHeader(request.Headers, TraceContextConstants.TraceStateHeaderName, tracestate!);
            }

            return activity;
        }

        /// <summary>
        /// Called after a reply message is received from the server.
        /// Captures fault details and completes the <see cref="Activity"/>.
        /// </summary>
        /// <param name="reply">The message to be transformed into types and handed back to the client.</param>
        /// <param name="correlationState">The <see cref="Activity"/> returned from <see cref="BeforeSendRequest"/>.</param>
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (!(correlationState is Activity activity))
                return;

            try
            {
                if (reply != null && reply.IsFault)
                {
                    activity.SetStatus(ActivityStatusCode.Error, "WCF fault received");

                    if (_options.CaptureFaultDetails)
                    {
                        TryCaptureFaultDetails(reply, activity);
                    }
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Ok);
                }
            }
            finally
            {
                activity.Stop();
                activity.Dispose();
            }
        }

        private static string GetOperationName(Message message)
        {
            try
            {
                if (message?.Headers?.Action != null)
                    return message.Headers.Action;
            }
            catch
            {
                // Best effort
            }

            return "UnknownWcfOperation";
        }

        private bool ShouldTraceOperation(string operationName)
        {
            if (_options.OperationFilter == null)
                return true;

            try
            {
                return _options.OperationFilter(operationName);
            }
            catch
            {
                // If the filter throws, trace the operation
                return true;
            }
        }

        private static void TryCaptureFaultDetails(Message reply, Activity activity)
        {
            try
            {
                var messageCopy = reply.CreateBufferedCopy(int.MaxValue);
                var faultMessage = messageCopy.CreateMessage();
                var fault = MessageFault.CreateFault(faultMessage, int.MaxValue);

                if (fault.Code != null)
                {
                    activity.SetTag("error.type", fault.Code.Name);
                    if (fault.Code.SubCode != null)
                    {
                        activity.SetTag("wcf.fault.subcode", fault.Code.SubCode.Name);
                    }
                }

                if (fault.Reason != null)
                {
                    activity.SetTag("error.message", fault.Reason.ToString());
                }

                // Replace the consumed message with a copy
                reply = messageCopy.CreateMessage();
            }
            catch
            {
                // Best effort - fault details are supplementary
                activity.SetTag("error.type", "WcfFault");
            }
        }
    }
}
