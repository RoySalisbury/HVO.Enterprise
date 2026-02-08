using System;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Represents an operation scope with exception tracking support.
    /// </summary>
    public interface IOperationScope : IDisposable
    {
        /// <summary>
        /// Records an exception that occurred during this operation.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        void RecordException(Exception exception);
    }
}
