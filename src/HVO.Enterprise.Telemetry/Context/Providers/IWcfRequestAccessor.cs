namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Accesses the current WCF request context.
    /// </summary>
    public interface IWcfRequestAccessor
    {
        /// <summary>
        /// Gets the current WCF request info.
        /// </summary>
        /// <returns>Request info or null if unavailable.</returns>
        WcfRequestInfo? GetCurrentRequest();
    }
}
