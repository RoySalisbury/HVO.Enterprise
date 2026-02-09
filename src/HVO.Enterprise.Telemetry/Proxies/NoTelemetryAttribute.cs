using System;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// Excludes a method from automatic telemetry instrumentation.
    /// Use on methods within an <see cref="InstrumentClassAttribute"/>-decorated interface
    /// to opt out individual methods (e.g., health checks, internal state accessors).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NoTelemetryAttribute : Attribute
    {
    }
}
