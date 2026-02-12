namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// OTLP transport protocol.
    /// </summary>
    public enum OtlpTransport
    {
        /// <summary>gRPC transport (default, port 4317).</summary>
        Grpc = 0,

        /// <summary>HTTP/protobuf transport (port 4318).</summary>
        HttpProtobuf = 1
    }
}
