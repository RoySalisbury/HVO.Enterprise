using System;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Marks a property as containing PII.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PiiAttribute : Attribute
    {
    }
}
