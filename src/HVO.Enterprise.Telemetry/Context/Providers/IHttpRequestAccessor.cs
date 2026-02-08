namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Accesses the current HTTP request context.
    /// </summary>
    public interface IHttpRequestAccessor
    {
        /// <summary>
        /// Gets the current HTTP request info.
        /// </summary>
        /// <returns>Request info or null if unavailable.</returns>
        HttpRequestInfo? GetCurrentRequest();
    }
}
