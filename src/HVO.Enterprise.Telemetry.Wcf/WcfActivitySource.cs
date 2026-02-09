using System.Diagnostics;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf
{
    /// <summary>
    /// Provides the shared <see cref="ActivitySource"/> for WCF telemetry operations.
    /// </summary>
    /// <remarks>
    /// All WCF client and server message inspectors share this single
    /// <see cref="ActivitySource"/> instance, registered under the name
    /// <see cref="TraceContextConstants.ActivitySourceName"/>.
    /// Consumers can listen for activities from this source by adding an
    /// <see cref="ActivityListener"/> that filters on the source name.
    /// </remarks>
    internal static class WcfActivitySource
    {
        /// <summary>
        /// The shared <see cref="ActivitySource"/> for all WCF telemetry operations.
        /// </summary>
        internal static readonly ActivitySource Instance = new ActivitySource(
            TraceContextConstants.ActivitySourceName,
            "1.0.0");
    }
}
