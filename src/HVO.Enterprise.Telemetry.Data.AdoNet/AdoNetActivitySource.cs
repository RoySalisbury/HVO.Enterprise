using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Data.AdoNet
{
    /// <summary>
    /// <see cref="ActivitySource"/> for ADO.NET telemetry.
    /// </summary>
    public static class AdoNetActivitySource
    {
        /// <summary>
        /// The activity source name for ADO.NET operations.
        /// </summary>
        public const string Name = "HVO.Enterprise.Telemetry.Data.AdoNet";

        /// <summary>
        /// Gets the <see cref="ActivitySource"/> for ADO.NET telemetry.
        /// </summary>
        public static ActivitySource Source { get; } = new ActivitySource(Name, "1.0.0");
    }
}
