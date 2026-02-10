using System;
using HVO.Enterprise.Telemetry.Abstractions;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Factory for creating instrumented proxy instances that wrap interface methods
    /// with automatic telemetry (operation scopes, Activity creation, timing, parameter capture).
    /// </summary>
    public interface ITelemetryProxyFactory
    {
        /// <summary>
        /// Creates an instrumented proxy for the specified interface type.
        /// The proxy intercepts method calls on <paramref name="target"/> and wraps instrumented
        /// methods with an <see cref="IOperationScope"/> for automatic telemetry.
        /// </summary>
        /// <typeparam name="T">The interface type to proxy. Must be an interface.</typeparam>
        /// <param name="target">The real implementation to delegate to.</param>
        /// <param name="options">
        /// Optional <see cref="InstrumentationOptions"/> controlling capture depth, PII detection, etc.
        /// When <c>null</c> the default options are used.
        /// </param>
        /// <returns>A proxy instance that implements <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an interface.</exception>
        T CreateProxy<T>(T target, InstrumentationOptions? options = null) where T : class;
    }
}
