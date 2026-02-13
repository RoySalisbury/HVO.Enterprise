using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Grpc.Client
{
    /// <summary>
    /// gRPC client interceptor that creates <see cref="Activity"/> spans for outgoing calls
    /// and propagates W3C TraceContext and HVO correlation via gRPC metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates a client-side Activity with <see cref="ActivityKind.Client"/> for each outgoing
    /// gRPC call, injecting <c>traceparent</c>/<c>tracestate</c> into outgoing metadata headers.
    /// </para>
    /// <para>
    /// Also injects HVO <c>x-correlation-id</c> from <see cref="CorrelationContext.Current"/>
    /// into gRPC metadata for end-to-end correlation flow.
    /// </para>
    /// <para>
    /// Tags activities with OpenTelemetry <c>rpc.*</c> semantic conventions including
    /// service name, method name, server address/port, and gRPC status code.
    /// </para>
    /// </remarks>
    public sealed class TelemetryClientInterceptor : Interceptor
    {
        private readonly GrpcTelemetryOptions _options;
        private readonly ILogger<TelemetryClientInterceptor>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClientInterceptor"/> class.
        /// </summary>
        /// <param name="options">The gRPC telemetry options.</param>
        /// <param name="logger">Optional logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public TelemetryClientInterceptor(
            GrpcTelemetryOptions options,
            ILogger<TelemetryClientInterceptor>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc />
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            if (!_options.EnableClientInterceptor)
                return continuation(request, context);

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);

            var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.client/{serviceName}/{methodName}",
                ActivityKind.Client);

            SetActivityTags(activity, serviceName, methodName, context.Host);

            var newContext = InjectHeaders(context, activity);

            _logger?.LogDebug("gRPC client call started: {Service}/{Method}", serviceName, methodName);

            try
            {
                var call = continuation(request, newContext);

                var responseAsync = WrapResponseAsync(call.ResponseAsync, activity, serviceName, methodName);

                return new AsyncUnaryCall<TResponse>(
                    responseAsync,
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordException(activity, ex);
                activity?.Dispose();
                _logger?.LogError(ex, "gRPC client call failed: {Service}/{Method}", serviceName, methodName);
                throw;
            }
        }

        /// <inheritdoc />
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            if (!_options.EnableClientInterceptor)
                return continuation(context);

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);

            var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.client/{serviceName}/{methodName}",
                ActivityKind.Client);

            SetActivityTags(activity, serviceName, methodName, context.Host);

            var newContext = InjectHeaders(context, activity);

            try
            {
                var call = continuation(newContext);

                var responseAsync = WrapResponseAsync(call.ResponseAsync, activity, serviceName, methodName);

                return new AsyncClientStreamingCall<TRequest, TResponse>(
                    call.RequestStream,
                    responseAsync,
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordException(activity, ex);
                activity?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            if (!_options.EnableClientInterceptor)
                return continuation(request, context);

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);

            var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.client/{serviceName}/{methodName}",
                ActivityKind.Client);

            SetActivityTags(activity, serviceName, methodName, context.Host);

            var newContext = InjectHeaders(context, activity);

            try
            {
                var call = continuation(request, newContext);

                // For server streaming, the activity stays open for the lifetime of the stream.
                // We wrap the response stream to record status on completion.
                return new AsyncServerStreamingCall<TResponse>(
                    call.ResponseStream,
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    () =>
                    {
                        activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
                        activity?.Dispose();
                        call.Dispose();
                    });
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordException(activity, ex);
                activity?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            if (!_options.EnableClientInterceptor)
                return continuation(context);

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);

            var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.client/{serviceName}/{methodName}",
                ActivityKind.Client);

            SetActivityTags(activity, serviceName, methodName, context.Host);

            var newContext = InjectHeaders(context, activity);

            try
            {
                var call = continuation(newContext);

                return new AsyncDuplexStreamingCall<TRequest, TResponse>(
                    call.RequestStream,
                    call.ResponseStream,
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    () =>
                    {
                        activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
                        activity?.Dispose();
                        call.Dispose();
                    });
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordException(activity, ex);
                activity?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            if (!_options.EnableClientInterceptor)
                return continuation(request, context);

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);

            using var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.client/{serviceName}/{methodName}",
                ActivityKind.Client);

            SetActivityTags(activity, serviceName, methodName, context.Host);

            var newContext = InjectHeaders(context, activity);

            try
            {
                var response = continuation(request, newContext);

                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
                return response;
            }
            catch (RpcException ex)
            {
                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)ex.StatusCode);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Status.Detail);
                RecordException(activity, ex);
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.Internal);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordException(activity, ex);
                throw;
            }
        }

        private static async Task<TResponse> WrapResponseAsync<TResponse>(
            Task<TResponse> responseTask, Activity? activity,
            string serviceName, string methodName)
        {
            try
            {
                var response = await responseTask.ConfigureAwait(false);
                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
                return response;
            }
            catch (RpcException ex)
            {
                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)ex.StatusCode);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Status.Detail);
                RecordException(activity, ex);
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.Internal);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordException(activity, ex);
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private ClientInterceptorContext<TRequest, TResponse> InjectHeaders<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            Activity? activity)
            where TRequest : class
            where TResponse : class
        {
            var metadata = context.Options.Headers ?? new Metadata();

            GrpcMetadataHelper.InjectTraceContext(activity, metadata);
            GrpcMetadataHelper.InjectCorrelation(metadata, _options.CorrelationHeaderName);

            var newOptions = context.Options.WithHeaders(metadata);
            return new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, newOptions);
        }

        private static void SetActivityTags(Activity? activity, string serviceName,
            string methodName, string? host)
        {
            if (activity == null) return;

            activity.SetTag(GrpcActivityTags.RpcSystem, GrpcActivityTags.GrpcSystemValue);
            activity.SetTag(GrpcActivityTags.RpcService, serviceName);
            activity.SetTag(GrpcActivityTags.RpcMethod, methodName);

            if (!string.IsNullOrEmpty(host))
            {
                var colonIndex = host!.LastIndexOf(':');
                if (colonIndex > 0 && int.TryParse(host.Substring(colonIndex + 1), out var port))
                {
                    activity.SetTag(GrpcActivityTags.ServerAddress, host.Substring(0, colonIndex));
                    activity.SetTag(GrpcActivityTags.ServerPort, port);
                }
                else
                {
                    activity.SetTag(GrpcActivityTags.ServerAddress, host);
                }
            }
        }

        private static void RecordException(Activity? activity, Exception ex)
        {
            if (activity == null) return;

            var tags = new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            };
            activity.AddEvent(new ActivityEvent("exception", tags: tags));
        }
    }
}
