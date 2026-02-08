namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Default gRPC request accessor using async-local storage.
    /// </summary>
    internal sealed class DefaultGrpcRequestAccessor : IGrpcRequestAccessor
    {
        /// <inheritdoc />
        public GrpcRequestInfo? GetCurrentRequest()
        {
            return GrpcRequestContextStore.Current;
        }
    }
}
