namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Represents an interface compatible with System.Web.Hosting.IRegisteredObject
    /// for IIS integration without requiring a reference to System.Web.
    /// This interface is used via reflection when running in IIS.
    /// </summary>
    internal interface IRegisteredObject
    {
        /// <summary>
        /// Notifies the registered object to stop.
        /// </summary>
        /// <param name="immediate">
        /// True to indicate that the object should unregister from the hosting environment before returning;
        /// otherwise, false.
        /// </param>
        void Stop(bool immediate);
    }
}
