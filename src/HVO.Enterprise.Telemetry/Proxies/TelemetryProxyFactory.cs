using System;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Default implementation of <see cref="ITelemetryProxyFactory"/> that creates
    /// <see cref="TelemetryDispatchProxy{T}"/> instances backed by an <see cref="IOperationScopeFactory"/>.
    /// </summary>
    public sealed class TelemetryProxyFactory : ITelemetryProxyFactory
    {
        private readonly IOperationScopeFactory _scopeFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="TelemetryProxyFactory"/>.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating operation scopes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="scopeFactory"/> is <c>null</c>.</exception>
        public TelemetryProxyFactory(IOperationScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        /// <inheritdoc />
        public T CreateProxy<T>(T target, InstrumentationOptions? options = null) where T : class
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException(
                    $"Type {typeof(T).Name} must be an interface. DispatchProxy only supports interfaces.",
                    nameof(T));
            }

            var proxy = DispatchProxy.Create<T, TelemetryDispatchProxy<T>>();
            if (proxy == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create DispatchProxy for {typeof(T).Name}.");
            }

            var telemetryProxy = proxy as TelemetryDispatchProxy<T>;
            if (telemetryProxy == null)
            {
                throw new InvalidOperationException(
                    $"Created proxy is not a TelemetryDispatchProxy<{typeof(T).Name}>.");
            }

            telemetryProxy.Initialize(
                target,
                _scopeFactory,
                options ?? new InstrumentationOptions());

            return (T)(object)telemetryProxy;
        }
    }
}
