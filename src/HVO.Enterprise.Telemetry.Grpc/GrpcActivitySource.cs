using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Grpc
{
    /// <summary>
    /// Provides the shared <see cref="ActivitySource"/> for gRPC telemetry operations.
    /// </summary>
    /// <remarks>
    /// All gRPC server and client interceptors share this single
    /// <see cref="ActivitySource"/> instance, registered under the name
    /// <c>"HVO.Enterprise.Telemetry.Grpc"</c>.
    /// Consumers can listen for activities from this source by adding an
    /// <see cref="ActivityListener"/> that filters on the source name.
    /// </remarks>
    internal static class GrpcActivitySource
    {
        /// <summary>
        /// The default ActivitySource name for gRPC telemetry.
        /// </summary>
        internal const string DefaultSourceName = "HVO.Enterprise.Telemetry.Grpc";

        /// <summary>
        /// The shared <see cref="ActivitySource"/> for all gRPC telemetry operations.
        /// </summary>
        internal static readonly ActivitySource Instance = new ActivitySource(
            DefaultSourceName,
            "1.0.0");
    }
}
