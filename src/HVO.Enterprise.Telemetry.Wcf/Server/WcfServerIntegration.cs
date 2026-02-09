using System;
using System.Diagnostics;
using System.Reflection;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Wcf.Server
{
    /// <summary>
    /// Provides reflection-based server-side WCF telemetry integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Server-side WCF types (<c>IDispatchMessageInspector</c>, <c>IServiceBehavior</c>,
    /// <c>ServiceHostBase</c>, <c>ChannelDispatcher</c>, <c>EndpointDispatcher</c>) are
    /// only available in the full .NET Framework <c>System.ServiceModel</c> assembly,
    /// not in the <c>System.ServiceModel.Primitives</c> NuGet package.
    /// </para>
    /// <para>
    /// This class uses reflection and <see cref="DispatchProxy"/> to create
    /// <c>IDispatchMessageInspector</c> implementations at runtime when the
    /// required types are loaded in the current AppDomain.
    /// </para>
    /// </remarks>
    public static class WcfServerIntegration
    {
        private static readonly object _lock = new object();
        private static volatile bool _typesChecked;
        private static Type? _dispatchMessageInspectorType;
        private static Type? _serviceHostBaseType;

        /// <summary>
        /// Gets whether the server-side WCF types are available at runtime.
        /// </summary>
        /// <remarks>
        /// Returns <c>true</c> on .NET Framework 4.8+ when <c>System.ServiceModel</c>
        /// is loaded with full hosting support. Returns <c>false</c> on .NET Core/.NET 5+
        /// (unless CoreWCF is present) because the WCF client NuGet package includes
        /// dispatcher interfaces but not the full hosting infrastructure.
        /// </remarks>
        public static bool IsWcfServerAvailable
        {
            get
            {
                EnsureTypesChecked();
                return _dispatchMessageInspectorType != null && _serviceHostBaseType != null;
            }
        }

        /// <summary>
        /// Attempts to add a telemetry dispatch message inspector to all endpoints
        /// of a WCF <c>ServiceHostBase</c>.
        /// </summary>
        /// <param name="serviceHost">
        /// The WCF ServiceHost or ServiceHostBase instance. Must be a valid
        /// <c>System.ServiceModel.ServiceHostBase</c> or derived type.
        /// </param>
        /// <param name="options">Optional WCF extension options.</param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        /// <returns><c>true</c> if the inspector was successfully added; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceHost"/> is null.
        /// </exception>
        public static bool TryAddTelemetryInspector(
            object serviceHost,
            WcfExtensionOptions? options = null,
            ILogger? logger = null)
        {
            if (serviceHost == null)
                throw new ArgumentNullException(nameof(serviceHost));

            if (!IsWcfServerAvailable)
            {
                logger?.LogDebug(
                    "WCF server types not available. Server-side telemetry inspector not registered.");
                return false;
            }

            try
            {
                // Verify the serviceHost is actually a ServiceHostBase
                if (_serviceHostBaseType != null &&
                    !_serviceHostBaseType.IsInstanceOfType(serviceHost))
                {
                    logger?.LogWarning(
                        "Object is not a ServiceHostBase. Type: {ServiceHostType}",
                        serviceHost.GetType().FullName);
                    return false;
                }

                // Create the dispatch inspector proxy
                var inspectorProxy = CreateDispatchInspectorProxy(options ?? new WcfExtensionOptions());
                if (inspectorProxy == null)
                {
                    logger?.LogWarning("Failed to create dispatch inspector proxy.");
                    return false;
                }

                // Add the inspector to all endpoint dispatchers via reflection
                return TryAddInspectorToEndpoints(serviceHost, inspectorProxy, logger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to add telemetry inspector to WCF service host.");
                return false;
            }
        }

        /// <summary>
        /// Creates a <see cref="WcfDispatchInspectorProxy"/> that implements
        /// <c>IDispatchMessageInspector</c> at runtime.
        /// </summary>
        internal static object? CreateDispatchInspectorProxy(WcfExtensionOptions options)
        {
            EnsureTypesChecked();

            if (_dispatchMessageInspectorType == null)
                return null;

            try
            {
                var createMethod = typeof(DispatchProxy)
                    .GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

                if (createMethod == null)
                    return null;

                var genericCreate = createMethod.MakeGenericMethod(
                    _dispatchMessageInspectorType,
                    typeof(WcfDispatchInspectorProxy));

                var proxy = genericCreate.Invoke(null, null);

                if (proxy is WcfDispatchInspectorProxy wcfProxy)
                {
                    wcfProxy.Initialize(WcfActivitySource.Instance, options);
                }

                return proxy;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryAddInspectorToEndpoints(
            object serviceHost,
            object inspector,
            ILogger? logger)
        {
            try
            {
                // Get ChannelDispatchers collection
                var channelDispatchersProperty = serviceHost.GetType()
                    .GetProperty("ChannelDispatchers");

                if (channelDispatchersProperty == null)
                {
                    logger?.LogDebug("ChannelDispatchers property not found on service host.");
                    return false;
                }

                var channelDispatchers = channelDispatchersProperty.GetValue(serviceHost);
                if (channelDispatchers == null)
                    return false;

                var enumerableType = channelDispatchers.GetType();
                var enumerator = enumerableType.GetMethod("GetEnumerator")?.Invoke(channelDispatchers, null);
                if (enumerator == null)
                    return false;

                var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
                var currentProperty = enumerator.GetType().GetProperty("Current");

                if (moveNextMethod == null || currentProperty == null)
                    return false;

                var inspectorAdded = false;

                while ((bool)(moveNextMethod.Invoke(enumerator, null) ?? false))
                {
                    var channelDispatcher = currentProperty.GetValue(enumerator);
                    if (channelDispatcher == null)
                        continue;

                    // Get Endpoints collection from ChannelDispatcher
                    var endpointsProperty = channelDispatcher.GetType()
                        .GetProperty("Endpoints");

                    if (endpointsProperty == null)
                        continue;

                    var endpoints = endpointsProperty.GetValue(channelDispatcher);
                    if (endpoints == null)
                        continue;

                    // Iterate endpoint dispatchers
                    var epEnumerator = endpoints.GetType()
                        .GetMethod("GetEnumerator")?.Invoke(endpoints, null);

                    if (epEnumerator == null)
                        continue;

                    var epMoveNext = epEnumerator.GetType().GetMethod("MoveNext");
                    var epCurrent = epEnumerator.GetType().GetProperty("Current");

                    if (epMoveNext == null || epCurrent == null)
                        continue;

                    while ((bool)(epMoveNext.Invoke(epEnumerator, null) ?? false))
                    {
                        var endpointDispatcher = epCurrent.GetValue(epEnumerator);
                        if (endpointDispatcher == null)
                            continue;

                        // Get DispatchRuntime.MessageInspectors
                        var dispatchRuntimeProperty = endpointDispatcher.GetType()
                            .GetProperty("DispatchRuntime");

                        if (dispatchRuntimeProperty == null)
                            continue;

                        var dispatchRuntime = dispatchRuntimeProperty.GetValue(endpointDispatcher);
                        if (dispatchRuntime == null)
                            continue;

                        var messageInspectorsProperty = dispatchRuntime.GetType()
                            .GetProperty("MessageInspectors");

                        if (messageInspectorsProperty == null)
                            continue;

                        var messageInspectors = messageInspectorsProperty.GetValue(dispatchRuntime);
                        if (messageInspectors == null)
                            continue;

                        // Add our inspector proxy
                        var addMethod = messageInspectors.GetType().GetMethod("Add");
                        if (addMethod != null)
                        {
                            addMethod.Invoke(messageInspectors, new[] { inspector });
                            inspectorAdded = true;
                            logger?.LogDebug("Telemetry inspector added to endpoint dispatcher.");
                        }
                    }
                }

                return inspectorAdded;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to add inspector to WCF endpoint dispatchers.");
                return false;
            }
        }

        private static void EnsureTypesChecked()
        {
            if (_typesChecked)
                return;

            lock (_lock)
            {
                if (_typesChecked)
                    return;

                try
                {
                    // Try to find IDispatchMessageInspector in loaded assemblies
                    _dispatchMessageInspectorType = Type.GetType(
                        "System.ServiceModel.Dispatcher.IDispatchMessageInspector, System.ServiceModel, " +
                        "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

                    // Also check CoreWCF if available
                    if (_dispatchMessageInspectorType == null)
                    {
                        _dispatchMessageInspectorType = Type.GetType(
                            "System.ServiceModel.Dispatcher.IDispatchMessageInspector, System.ServiceModel");
                    }

                    _serviceHostBaseType = Type.GetType(
                        "System.ServiceModel.ServiceHostBase, System.ServiceModel, " +
                        "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

                    if (_serviceHostBaseType == null)
                    {
                        _serviceHostBaseType = Type.GetType(
                            "System.ServiceModel.ServiceHostBase, System.ServiceModel");
                    }
                }
                catch
                {
                    _dispatchMessageInspectorType = null;
                    _serviceHostBaseType = null;
                }

                _typesChecked = true;
            }
        }
    }
}
