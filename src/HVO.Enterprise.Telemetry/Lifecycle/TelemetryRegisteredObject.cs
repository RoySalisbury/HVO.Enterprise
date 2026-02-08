using System;
using System.Reflection;
using System.Threading.Tasks;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// IIS HostingEnvironment integration for graceful shutdown notification.
    /// Implements the compatible interface pattern to work with System.Web.Hosting.IRegisteredObject
    /// without requiring a reference to System.Web.
    /// </summary>
    internal sealed class TelemetryRegisteredObject : IRegisteredObject
    {
        private readonly TelemetryLifetimeManager _lifetimeManager;

        /// <summary>
        /// Creates a new registered object for IIS integration.
        /// </summary>
        /// <param name="lifetimeManager">The lifetime manager to notify on shutdown.</param>
        /// <exception cref="ArgumentNullException">Thrown when lifetimeManager is null.</exception>
        public TelemetryRegisteredObject(TelemetryLifetimeManager lifetimeManager)
        {
            if (lifetimeManager == null)
                throw new ArgumentNullException(nameof(lifetimeManager));

            _lifetimeManager = lifetimeManager;
        }

        /// <summary>
        /// Called by IIS to notify that the application is shutting down.
        /// </summary>
        /// <param name="immediate">
        /// True if the shutdown is immediate; false if there is time for graceful shutdown.
        /// </param>
        public void Stop(bool immediate)
        {
            if (!immediate)
            {
                // Graceful shutdown - give telemetry time to flush
                try
                {
                    Task.Run(async () => await _lifetimeManager.ShutdownAsync(TimeSpan.FromSeconds(5)))
                        .GetAwaiter()
                        .GetResult();
                }
                catch
                {
                    // Swallow exceptions during shutdown to avoid crashing the app pool
                }
            }

            // Unregister from HostingEnvironment
            try
            {
                var hostingEnvironmentType = Type.GetType(
                    "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

                var unregisterMethod = hostingEnvironmentType?.GetMethod(
                    "UnregisterObject",
                    BindingFlags.Public | BindingFlags.Static);

                unregisterMethod?.Invoke(null, new object[] { this });
            }
            catch
            {
                // Swallow exceptions during unregister
            }
        }
    }
}
