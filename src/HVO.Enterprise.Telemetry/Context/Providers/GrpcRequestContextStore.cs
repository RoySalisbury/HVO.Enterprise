using System.Threading;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Async-local store for gRPC request context.
    /// </summary>
    public static class GrpcRequestContextStore
    {
        private static readonly AsyncLocal<GrpcRequestInfo?> CurrentRequest = new AsyncLocal<GrpcRequestInfo?>();

        /// <summary>
        /// Gets or sets the current request info.
        /// </summary>
        public static GrpcRequestInfo? Current
        {
            get => CurrentRequest.Value;
            set => CurrentRequest.Value = value;
        }
    }
}
