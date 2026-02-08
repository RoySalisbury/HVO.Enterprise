using System.Threading;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Async-local store for HTTP request context.
    /// </summary>
    public static class HttpRequestContextStore
    {
        private static readonly AsyncLocal<HttpRequestInfo?> CurrentRequest = new AsyncLocal<HttpRequestInfo?>();

        /// <summary>
        /// Gets or sets the current request info.
        /// </summary>
        public static HttpRequestInfo? Current
        {
            get => CurrentRequest.Value;
            set => CurrentRequest.Value = value;
        }
    }
}
