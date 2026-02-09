using System;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Marks a parameter or property as containing sensitive data that should be
    /// redacted when captured by telemetry instrumentation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class SensitiveDataAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the redaction strategy to apply when this value is captured.
        /// Defaults to <see cref="RedactionStrategy.Mask"/>.
        /// </summary>
        public RedactionStrategy Strategy { get; set; } = RedactionStrategy.Mask;
    }
}
