using System;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Represents work to be processed on the background worker thread.
    /// </summary>
    internal abstract class TelemetryWorkItem
    {
        /// <summary>
        /// Gets the operation type for monitoring and logging purposes.
        /// </summary>
        public abstract string OperationType { get; }
        
        /// <summary>
        /// Executes the work item. Called on the background worker thread.
        /// </summary>
        public abstract void Execute();
    }
}
