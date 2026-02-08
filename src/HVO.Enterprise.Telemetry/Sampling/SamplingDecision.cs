namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Sampling decision for a trace.
    /// </summary>
    public enum SamplingDecision
    {
        /// <summary>
        /// Do not sample this trace.
        /// </summary>
        Drop = 0,

        /// <summary>
        /// Sample this trace and record it.
        /// </summary>
        RecordAndSample = 1
    }
}
