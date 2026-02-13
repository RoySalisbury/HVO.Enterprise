namespace HVO.Enterprise.Telemetry.Grpc
{
    /// <summary>
    /// OpenTelemetry semantic convention constants for gRPC instrumentation.
    /// </summary>
    /// <remarks>
    /// Follows <see href="https://opentelemetry.io/docs/specs/semconv/rpc/grpc/">
    /// OpenTelemetry RPC/gRPC Semantic Conventions</see>.
    /// </remarks>
    public static class GrpcActivityTags
    {
        /// <summary>The RPC system. Always <c>"grpc"</c>.</summary>
        public const string RpcSystem = "rpc.system";

        /// <summary>The gRPC service name (e.g., <c>"mypackage.MyService"</c>).</summary>
        public const string RpcService = "rpc.service";

        /// <summary>The gRPC method name (e.g., <c>"GetOrder"</c>).</summary>
        public const string RpcMethod = "rpc.method";

        /// <summary>Numeric gRPC status code (0=OK, 1=CANCELLED, etc.).</summary>
        public const string RpcGrpcStatusCode = "rpc.grpc.status_code";

        /// <summary>Server hostname or IP address.</summary>
        public const string ServerAddress = "server.address";

        /// <summary>Server port.</summary>
        public const string ServerPort = "server.port";

        /// <summary>The RPC system value for gRPC.</summary>
        public const string GrpcSystemValue = "grpc";

        /// <summary>gRPC request message size in bytes.</summary>
        public const string RpcMessageSentSize = "rpc.message.sent.compressed_size";

        /// <summary>gRPC response message size in bytes.</summary>
        public const string RpcMessageReceivedSize = "rpc.message.received.compressed_size";
    }
}
