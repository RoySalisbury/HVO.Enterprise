namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Accesses the current gRPC request context.
    /// </summary>
    public interface IGrpcRequestAccessor
    {
        /// <summary>
        /// Gets the current gRPC request info.
        /// </summary>
        /// <returns>Request info or null if unavailable.</returns>
        GrpcRequestInfo? GetCurrentRequest();
    }
}
