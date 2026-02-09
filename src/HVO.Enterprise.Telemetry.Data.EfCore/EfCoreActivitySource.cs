using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Data.EfCore
{
    /// <summary>
    /// <see cref="ActivitySource"/> for Entity Framework Core telemetry.
    /// </summary>
    public static class EfCoreActivitySource
    {
        /// <summary>
        /// The activity source name for EF Core operations.
        /// </summary>
        public const string Name = "HVO.Enterprise.Telemetry.Data.EfCore";

        /// <summary>
        /// Gets the <see cref="ActivitySource"/> for EF Core telemetry.
        /// </summary>
        public static ActivitySource Source { get; } = new ActivitySource(Name, "1.0.0");
    }
}
