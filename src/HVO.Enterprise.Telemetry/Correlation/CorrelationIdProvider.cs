using System;

namespace HVO.Enterprise.Telemetry.Correlation
{
    /// <summary>
    /// Default implementation of <see cref="ICorrelationIdProvider"/> that delegates
    /// to <see cref="CorrelationContext"/> for correlation ID management.
    /// </summary>
    public sealed class CorrelationIdProvider : ICorrelationIdProvider
    {
        /// <inheritdoc />
        public string GenerateCorrelationId()
        {
            var id = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = id;
            return id;
        }

        /// <inheritdoc />
        public bool TryGetCorrelationId(out string? correlationId)
        {
            var raw = CorrelationContext.GetRawValue();
            if (raw != null)
            {
                correlationId = raw;
                return true;
            }

            correlationId = null;
            return false;
        }
    }
}
