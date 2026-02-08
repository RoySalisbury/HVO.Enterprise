namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Accesses the current user context.
    /// </summary>
    public interface IUserContextAccessor
    {
        /// <summary>
        /// Gets the current user context.
        /// </summary>
        /// <returns>User context or null if unavailable.</returns>
        UserContext? GetUserContext();
    }
}
