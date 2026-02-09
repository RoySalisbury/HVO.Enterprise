using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Data
{
    /// <summary>
    /// Shared <see cref="ActivitySource"/> for data instrumentation packages.
    /// </summary>
    public static class DataActivitySource
    {
        /// <summary>
        /// The shared activity source name prefix for all data extensions.
        /// </summary>
        public const string BaseName = "HVO.Enterprise.Telemetry.Data";

        /// <summary>
        /// Gets the shared <see cref="ActivitySource"/> for general data telemetry.
        /// </summary>
        public static ActivitySource Source { get; } = new ActivitySource(BaseName, "1.0.0");

        /// <summary>
        /// Creates a technology-specific <see cref="ActivitySource"/>.
        /// </summary>
        /// <param name="technology">The technology suffix (e.g., "EfCore", "Redis").</param>
        /// <returns>A new <see cref="ActivitySource"/> with the qualified name.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="technology"/> is null or empty.</exception>
        public static ActivitySource CreateSource(string technology)
        {
            if (string.IsNullOrEmpty(technology))
                throw new ArgumentException("Technology name cannot be null or empty.", nameof(technology));

            return new ActivitySource($"{BaseName}.{technology}", "1.0.0");
        }
    }
}
