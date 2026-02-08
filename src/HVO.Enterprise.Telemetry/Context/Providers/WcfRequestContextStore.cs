using System.Threading;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Async-local store for WCF request context.
    /// </summary>
    public static class WcfRequestContextStore
    {
        private static readonly AsyncLocal<WcfRequestInfo?> CurrentRequest = new AsyncLocal<WcfRequestInfo?>();

        /// <summary>
        /// Gets or sets the current request info.
        /// </summary>
        public static WcfRequestInfo? Current
        {
            get => CurrentRequest.Value;
            set => CurrentRequest.Value = value;
        }
    }
}
