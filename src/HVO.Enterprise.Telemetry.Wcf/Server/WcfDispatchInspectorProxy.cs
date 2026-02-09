using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel.Channels;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf.Server
{
    /// <summary>
    /// A <see cref="DispatchProxy"/> that implements <c>IDispatchMessageInspector</c>
    /// via reflection for server-side WCF telemetry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>IDispatchMessageInspector</c> interface is only available in the full
    /// .NET Framework <c>System.ServiceModel</c> assembly, not in the
    /// <c>System.ServiceModel.Primitives</c> NuGet package. This proxy uses
    /// <see cref="DispatchProxy"/> to implement the interface at runtime when
    /// the required types are available.
    /// </para>
    /// <para>
    /// The proxy intercepts two methods:
    /// <list type="bullet">
    ///   <item><c>AfterReceiveRequest</c> - extracts W3C Trace Context and starts a server Activity</item>
    ///   <item><c>BeforeSendReply</c> - captures fault details and stops the Activity</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class WcfDispatchInspectorProxy : DispatchProxy
    {
        private ActivitySource? _activitySource;
        private WcfExtensionOptions? _options;

        /// <summary>
        /// Initializes the proxy with the required dependencies.
        /// Must be called after <see cref="DispatchProxy.Create{T,TProxy}"/>.
        /// </summary>
        /// <param name="activitySource">The <see cref="ActivitySource"/> for creating activities.</param>
        /// <param name="options">WCF extension options.</param>
        internal void Initialize(ActivitySource activitySource, WcfExtensionOptions options)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Dispatches method invocations to the appropriate handler.
        /// </summary>
        /// <param name="targetMethod">The interface method being invoked.</param>
        /// <param name="args">The method arguments.</param>
        /// <returns>The return value for the method, or <c>null</c>.</returns>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null || _activitySource == null)
                return null;

            switch (targetMethod.Name)
            {
                case "AfterReceiveRequest":
                    return HandleAfterReceiveRequest(args);

                case "BeforeSendReply":
                    HandleBeforeSendReply(args);
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Handles the <c>AfterReceiveRequest(ref Message, IClientChannel, InstanceContext)</c> call.
        /// Extracts trace context from SOAP headers and creates a server <see cref="Activity"/>.
        /// </summary>
        private object? HandleAfterReceiveRequest(object?[]? args)
        {
            // args[0] = ref Message request
            // args[1] = IClientChannel channel
            // args[2] = InstanceContext instanceContext
            if (args == null || args.Length < 1)
                return null;

            var message = args[0] as Message;
            if (message == null)
                return null;

            var operationName = GetOperationName(message);

            if (_options?.OperationFilter != null)
            {
                try
                {
                    if (!_options.OperationFilter(operationName))
                        return null;
                }
                catch
                {
                    // If filter throws, trace the operation
                }
            }

            // Extract W3C Trace Context from SOAP headers
            string? traceparent = null;
            string? tracestate = null;
            try
            {
                traceparent = SoapHeaderAccessor.GetHeader(
                    message.Headers, TraceContextConstants.TraceParentHeaderName);
                tracestate = SoapHeaderAccessor.GetHeader(
                    message.Headers, TraceContextConstants.TraceStateHeaderName);
            }
            catch
            {
                // Headers may not be readable; start a new trace
            }

            Activity? activity = !string.IsNullOrEmpty(traceparent) &&
                W3CTraceContextPropagator.TryParseTraceParent(
                    traceparent, out _, out _, out _)
                ? _activitySource!.StartActivity(
                    operationName,
                    ActivityKind.Server,
                    traceparent!)
                : _activitySource!.StartActivity(
                    operationName,
                    ActivityKind.Server);

            if (activity == null)
                return null;

            // Set tracestate if available
            if (!string.IsNullOrEmpty(tracestate))
            {
                activity.TraceStateString = tracestate;
            }

            // Set RPC semantic convention tags
            activity.SetTag("rpc.system", "wcf");
            activity.SetTag("rpc.method", operationName);

            // Try to get service name from InstanceContext (reflection, as type is not compile-time available)
            TrySetServiceTags(activity, args);

            return activity;
        }

        /// <summary>
        /// Handles the <c>BeforeSendReply(ref Message, object)</c> call.
        /// Captures fault details and stops the <see cref="Activity"/>.
        /// </summary>
        private void HandleBeforeSendReply(object?[]? args)
        {
            // args[0] = ref Message reply
            // args[1] = object correlationState (Activity from AfterReceiveRequest)
            if (args == null || args.Length < 2)
                return;

            if (!(args[1] is Activity activity))
                return;

            using (activity)
            {
                var reply = args[0] as Message;
                if (reply != null && reply.IsFault)
                {
                    activity.SetStatus(ActivityStatusCode.Error, "WCF fault");

                    if (_options?.CaptureFaultDetails == true)
                    {
                        TryCaptureFaultDetails(reply, activity);
                    }
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Ok);
                }

                // Inject trace context into reply if configured
                if (_options?.PropagateTraceContextInReply == true && reply != null)
                {
                    try
                    {
                        var responseTraceparent = W3CTraceContextPropagator.CreateTraceParent(activity);
                        SoapHeaderAccessor.SetHeader(
                            reply.Headers,
                            TraceContextConstants.TraceParentHeaderName,
                            responseTraceparent);

                        var responseTracestate = W3CTraceContextPropagator.GetTraceState(activity);
                        if (!string.IsNullOrEmpty(responseTracestate))
                        {
                            SoapHeaderAccessor.SetHeader(
                                reply.Headers,
                                TraceContextConstants.TraceStateHeaderName,
                                responseTracestate!);
                        }
                    }
                    catch
                    {
                        // Best effort header injection
                    }
                }

                activity.Stop();
            }
        }

        private static string GetOperationName(Message message)
        {
            try
            {
                if (message.Headers?.Action != null)
                    return message.Headers.Action;
            }
            catch
            {
                // Best effort
            }

            return "UnknownWcfOperation";
        }

        private static void TrySetServiceTags(Activity activity, object?[]? args)
        {
            try
            {
                // args[2] = InstanceContext; try to get service type via reflection
                if (args != null && args.Length > 2 && args[2] != null)
                {
                    var instanceContext = args[2]!;
                    var hostProp = instanceContext.GetType().GetProperty("Host");
                    if (hostProp != null)
                    {
                        var host = hostProp.GetValue(instanceContext);
                        if (host != null)
                        {
                            var descProp = host.GetType().GetProperty("Description");
                            if (descProp != null)
                            {
                                var desc = descProp.GetValue(host);
                                if (desc != null)
                                {
                                    var nameProp = desc.GetType().GetProperty("Name");
                                    if (nameProp != null)
                                    {
                                        var name = nameProp.GetValue(desc) as string;
                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            activity.SetTag("rpc.service", name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Reflection-based tag extraction is best effort
            }
        }

        private static void TryCaptureFaultDetails(Message reply, Activity activity)
        {
            try
            {
                const int MaxFaultBufferSize = 64 * 1024; // 64KB limit to avoid excessive allocations
                var messageCopy = reply.CreateBufferedCopy(MaxFaultBufferSize);
                var faultMessage = messageCopy.CreateMessage();
                var fault = MessageFault.CreateFault(faultMessage, MaxFaultBufferSize);

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
            }
            catch
            {
                // Best effort
                activity.SetTag("error.type", "WcfFault");
            }
        }
    }
}
