namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Result of a flush operation on the telemetry background worker.
    /// </summary>
    public sealed class FlushResult
    {
        /// <summary>
        /// Gets or sets whether the flush completed successfully within the timeout.
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets the number of items flushed during the operation.
        /// </summary>
        public long ItemsFlushed { get; set; }
        
        /// <summary>
        /// Gets or sets the number of items remaining in the queue after flush.
        /// </summary>
        public int ItemsRemaining { get; set; }
        
        /// <summary>
        /// Gets or sets whether the flush operation timed out.
        /// </summary>
        public bool TimedOut { get; set; }
    }
}
