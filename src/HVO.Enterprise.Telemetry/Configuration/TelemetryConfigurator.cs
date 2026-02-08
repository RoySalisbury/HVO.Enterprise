using System;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Fluent API for configuring telemetry.
    /// </summary>
    public sealed class TelemetryConfigurator
    {
        private readonly ConfigurationProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryConfigurator"/> class.
        /// </summary>
        public TelemetryConfigurator()
            : this(ConfigurationProvider.Instance)
        {
        }

        internal TelemetryConfigurator(ConfigurationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Configures global defaults.
        /// </summary>
        /// <returns>Global configurator.</returns>
        public GlobalConfigurator Global()
        {
            return new GlobalConfigurator(_provider);
        }

        /// <summary>
        /// Configures a specific namespace.
        /// </summary>
        /// <param name="namespacePattern">Namespace pattern.</param>
        /// <returns>Namespace configurator.</returns>
        public NamespaceConfigurator Namespace(string namespacePattern)
        {
            return new NamespaceConfigurator(_provider, namespacePattern);
        }

        /// <summary>
        /// Configures a specific type.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <returns>Type configurator.</returns>
        public TypeConfigurator<T> ForType<T>()
        {
            return new TypeConfigurator<T>(_provider);
        }

        /// <summary>
        /// Configures a specific method.
        /// </summary>
        /// <param name="method">Target method.</param>
        /// <returns>Method configurator.</returns>
        public MethodConfigurator ForMethod(MethodInfo method)
        {
            return new MethodConfigurator(_provider, method);
        }
    }
}
