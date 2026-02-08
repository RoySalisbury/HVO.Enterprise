using System;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Result of a telemetry shutdown operation.
    /// </summary>
    public sealed class ShutdownResult
    {
        /// <summary>
        /// Gets or sets whether the shutdown completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of items flushed during shutdown.
        /// </summary>
        public long ItemsFlushed { get; set; }

        /// <summary>
        /// Gets or sets the number of items remaining in the queue after shutdown.
        /// </summary>
        public int ItemsRemaining { get; set; }

        /// <summary>
        /// Gets or sets the duration of the shutdown operation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the reason for shutdown failure, if applicable.
        /// </summary>
        public string? Reason { get; set; }
    }
}
