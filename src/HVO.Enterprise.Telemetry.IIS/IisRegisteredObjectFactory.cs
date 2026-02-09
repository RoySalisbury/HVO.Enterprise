using System;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.IIS
{
    /// <summary>
    /// Factory for creating <see cref="IisRegisteredObjectProxy"/> instances that implement
    /// <c>System.Web.Hosting.IRegisteredObject</c> via <see cref="DispatchProxy"/> at runtime.
    /// </summary>
    internal static class IisRegisteredObjectFactory
    {
        private static readonly Type? _iRegisteredObjectType;
        private static readonly Type? _hostingEnvironmentType;
        private static readonly MethodInfo? _registerMethod;
        private static readonly MethodInfo? _unregisterMethod;

        static IisRegisteredObjectFactory()
        {
            // Attempt to load System.Web types via reflection
            _iRegisteredObjectType = Type.GetType(
                "System.Web.Hosting.IRegisteredObject, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            _hostingEnvironmentType = Type.GetType(
                "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            if (_iRegisteredObjectType != null && _hostingEnvironmentType != null)
            {
                _registerMethod = _hostingEnvironmentType.GetMethod(
                    "RegisterObject",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { _iRegisteredObjectType },
                    null);

                _unregisterMethod = _hostingEnvironmentType.GetMethod(
                    "UnregisterObject",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { _iRegisteredObjectType },
                    null);
            }
        }

        /// <summary>
        /// Gets whether System.Web hosting types are available in the current runtime.
        /// </summary>
        internal static bool IsSystemWebAvailable =>
            _iRegisteredObjectType != null &&
            _hostingEnvironmentType != null &&
            _registerMethod != null &&
            _unregisterMethod != null;

        /// <summary>
        /// Attempts to create a <see cref="IisRegisteredObjectProxy"/> that implements
        /// <c>IRegisteredObject</c> and configure it with the provided handler and timeout.
        /// </summary>
        /// <param name="shutdownHandler">The shutdown handler to delegate to.</param>
        /// <param name="shutdownTimeout">The timeout for graceful shutdown.</param>
        /// <param name="proxy">When this method returns, contains the created proxy, or <c>null</c> if creation failed.</param>
        /// <returns><c>true</c> if the proxy was created and registered; <c>false</c> if System.Web is not available.</returns>
        internal static bool TryCreate(
            IisShutdownHandler shutdownHandler,
            TimeSpan shutdownTimeout,
            out object? proxy)
        {
            proxy = null;

            if (!IsSystemWebAvailable || _iRegisteredObjectType == null)
                return false;

            try
            {
                // Use DispatchProxy.Create<IRegisteredObject, IisRegisteredObjectProxy>()
                // via reflection since IRegisteredObject is not available at compile time
                var createMethod = typeof(DispatchProxy)
                    .GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

                if (createMethod == null)
                    return false;

                var genericCreate = createMethod.MakeGenericMethod(
                    _iRegisteredObjectType,
                    typeof(IisRegisteredObjectProxy));

                proxy = genericCreate.Invoke(null, null);
                if (proxy == null)
                    return false;

                // Configure the proxy
                var typedProxy = (IisRegisteredObjectProxy)proxy;
                typedProxy.ShutdownHandler = shutdownHandler;
                typedProxy.ShutdownTimeout = shutdownTimeout;

                // Set up unregister action
                var proxyRef = proxy;
                typedProxy.UnregisterSelf = () =>
                {
                    try
                    {
                        _unregisterMethod?.Invoke(null, new[] { proxyRef });
                    }
                    catch
                    {
                        // Best effort - may fail during late shutdown
                    }
                };

                return true;
            }
            catch
            {
                proxy = null;
                return false;
            }
        }

        /// <summary>
        /// Registers a proxy object with <c>HostingEnvironment.RegisterObject</c>.
        /// </summary>
        /// <param name="proxy">The proxy implementing <c>IRegisteredObject</c>.</param>
        /// <returns><c>true</c> if registration succeeded; <c>false</c> otherwise.</returns>
        internal static bool TryRegister(object proxy)
        {
            if (_registerMethod == null)
                return false;

            try
            {
                _registerMethod.Invoke(null, new[] { proxy });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Unregisters a proxy object from <c>HostingEnvironment.UnregisterObject</c>.
        /// </summary>
        /// <param name="proxy">The proxy implementing <c>IRegisteredObject</c>.</param>
        /// <returns><c>true</c> if unregistration succeeded; <c>false</c> otherwise.</returns>
        internal static bool TryUnregister(object proxy)
        {
            if (_unregisterMethod == null)
                return false;

            try
            {
                _unregisterMethod.Invoke(null, new[] { proxy });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
