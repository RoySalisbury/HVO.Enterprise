using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Grpc.Server
{
    /// <summary>
    /// gRPC server interceptor that creates <see cref="Activity"/> spans for incoming calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Extracts W3C TraceContext from gRPC metadata headers (<c>traceparent</c>, <c>tracestate</c>)
    /// and creates a server-side Activity with <see cref="ActivityKind.Server"/>.
    /// </para>
    /// <para>
    /// Also extracts HVO <c>x-correlation-id</c> from metadata and sets
    /// <see cref="CorrelationContext.Current"/> for the duration of the call.
    /// </para>
    /// <para>
    /// Tags activities with OpenTelemetry <c>rpc.*</c> semantic conventions.
    /// </para>
    /// </remarks>
    public sealed class TelemetryServerInterceptor : Interceptor
    {
        private readonly GrpcTelemetryOptions _options;
        private readonly ILogger<TelemetryServerInterceptor>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryServerInterceptor"/> class.
        /// </summary>
        /// <param name="options">The gRPC telemetry options.</param>
        /// <param name="logger">Optional logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public TelemetryServerInterceptor(
            GrpcTelemetryOptions options,
            ILogger<TelemetryServerInterceptor>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc />
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            if (!_options.EnableServerInterceptor || ShouldSuppress(context.Method))
            {
                return await continuation(request, context).ConfigureAwait(false);
            }

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);
            var parentContext = GrpcMetadataHelper.ExtractTraceContext(context.RequestHeaders);

            using var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.server/{serviceName}/{methodName}",
                ActivityKind.Server,
                parentContext);

            IDisposable? correlationScope = null;
            var correlationId = GrpcMetadataHelper.GetMetadataValue(
                context.RequestHeaders, _options.CorrelationHeaderName);
            if (!string.IsNullOrEmpty(correlationId))
            {
                correlationScope = CorrelationContext.BeginScope(correlationId!);
            }

            try
            {
                SetActivityTags(activity, serviceName, methodName);
                _logger?.LogDebug("gRPC server call started: {Service}/{Method}", serviceName, methodName);

                var response = await continuation(request, context).ConfigureAwait(false);

                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
                return response;
            }
            catch (RpcException ex)
            {
                SetErrorStatus(activity, ex.StatusCode, ex.Status.Detail);
                RecordException(activity, ex);
                _logger?.LogError(ex, "gRPC server call failed: {Service}/{Method} StatusCode={StatusCode}",
                    serviceName, methodName, ex.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                SetErrorStatus(activity, StatusCode.Internal, ex.Message);
                RecordException(activity, ex);
                _logger?.LogError(ex, "gRPC server call failed with exception: {Service}/{Method}",
                    serviceName, methodName);
                throw;
            }
            finally
            {
                correlationScope?.Dispose();
            }
        }

        /// <inheritdoc />
        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            if (!_options.EnableServerInterceptor || ShouldSuppress(context.Method))
            {
                return await continuation(requestStream, context).ConfigureAwait(false);
            }

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);
            var parentContext = GrpcMetadataHelper.ExtractTraceContext(context.RequestHeaders);

            using var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.server/{serviceName}/{methodName}",
                ActivityKind.Server,
                parentContext);

            IDisposable? correlationScope = null;
            var correlationId = GrpcMetadataHelper.GetMetadataValue(
                context.RequestHeaders, _options.CorrelationHeaderName);
            if (!string.IsNullOrEmpty(correlationId))
            {
                correlationScope = CorrelationContext.BeginScope(correlationId!);
            }

            try
            {
                SetActivityTags(activity, serviceName, methodName);

                var response = await continuation(requestStream, context).ConfigureAwait(false);

                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
                return response;
            }
            catch (RpcException ex)
            {
                SetErrorStatus(activity, ex.StatusCode, ex.Status.Detail);
                RecordException(activity, ex);
                throw;
            }
            catch (Exception ex)
            {
                SetErrorStatus(activity, StatusCode.Internal, ex.Message);
                RecordException(activity, ex);
                throw;
            }
            finally
            {
                correlationScope?.Dispose();
            }
        }

        /// <inheritdoc />
        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            if (!_options.EnableServerInterceptor || ShouldSuppress(context.Method))
            {
                await continuation(request, responseStream, context).ConfigureAwait(false);
                return;
            }

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);
            var parentContext = GrpcMetadataHelper.ExtractTraceContext(context.RequestHeaders);

            using var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.server/{serviceName}/{methodName}",
                ActivityKind.Server,
                parentContext);

            IDisposable? correlationScope = null;
            var correlationId = GrpcMetadataHelper.GetMetadataValue(
                context.RequestHeaders, _options.CorrelationHeaderName);
            if (!string.IsNullOrEmpty(correlationId))
            {
                correlationScope = CorrelationContext.BeginScope(correlationId!);
            }

            try
            {
                SetActivityTags(activity, serviceName, methodName);

                await continuation(request, responseStream, context).ConfigureAwait(false);

                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
            }
            catch (RpcException ex)
            {
                SetErrorStatus(activity, ex.StatusCode, ex.Status.Detail);
                RecordException(activity, ex);
                throw;
            }
            catch (Exception ex)
            {
                SetErrorStatus(activity, StatusCode.Internal, ex.Message);
                RecordException(activity, ex);
                throw;
            }
            finally
            {
                correlationScope?.Dispose();
            }
        }

        /// <inheritdoc />
        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            if (!_options.EnableServerInterceptor || ShouldSuppress(context.Method))
            {
                await continuation(requestStream, responseStream, context).ConfigureAwait(false);
                return;
            }

            var (serviceName, methodName) = GrpcMethodParser.Parse(context.Method);
            var parentContext = GrpcMetadataHelper.ExtractTraceContext(context.RequestHeaders);

            using var activity = GrpcActivitySource.Instance.StartActivity(
                $"grpc.server/{serviceName}/{methodName}",
                ActivityKind.Server,
                parentContext);

            IDisposable? correlationScope = null;
            var correlationId = GrpcMetadataHelper.GetMetadataValue(
                context.RequestHeaders, _options.CorrelationHeaderName);
            if (!string.IsNullOrEmpty(correlationId))
            {
                correlationScope = CorrelationContext.BeginScope(correlationId!);
            }

            try
            {
                SetActivityTags(activity, serviceName, methodName);

                await continuation(requestStream, responseStream, context).ConfigureAwait(false);

                activity?.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)StatusCode.OK);
            }
            catch (RpcException ex)
            {
                SetErrorStatus(activity, ex.StatusCode, ex.Status.Detail);
                RecordException(activity, ex);
                throw;
            }
            catch (Exception ex)
            {
                SetErrorStatus(activity, StatusCode.Internal, ex.Message);
                RecordException(activity, ex);
                throw;
            }
            finally
            {
                correlationScope?.Dispose();
            }
        }

        private static void SetActivityTags(Activity? activity, string serviceName, string methodName)
        {
            if (activity == null) return;

            activity.SetTag(GrpcActivityTags.RpcSystem, GrpcActivityTags.GrpcSystemValue);
            activity.SetTag(GrpcActivityTags.RpcService, serviceName);
            activity.SetTag(GrpcActivityTags.RpcMethod, methodName);
        }

        private static void SetErrorStatus(Activity? activity, StatusCode statusCode, string? detail)
        {
            if (activity == null) return;

            activity.SetTag(GrpcActivityTags.RpcGrpcStatusCode, (int)statusCode);
            activity.SetStatus(ActivityStatusCode.Error, detail);
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

        internal bool ShouldSuppress(string method)
        {
            if (_options.SuppressHealthChecks && method.Contains("grpc.health.v1.Health"))
                return true;
            if (_options.SuppressReflection && method.Contains("grpc.reflection"))
                return true;
            return false;
        }
    }
}
