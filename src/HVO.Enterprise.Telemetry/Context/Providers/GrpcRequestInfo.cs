using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Represents gRPC request information.
    /// </summary>
    public sealed class GrpcRequestInfo
    {
        /// <summary>
        /// Gets or sets the gRPC service name.
        /// </summary>
        public string? Service { get; set; }

        /// <summary>
        /// Gets or sets the gRPC method name.
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// Gets or sets the metadata headers.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
