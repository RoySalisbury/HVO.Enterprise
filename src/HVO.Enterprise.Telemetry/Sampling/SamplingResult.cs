namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Result of a sampling decision.
    /// </summary>
    public readonly struct SamplingResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingResult"/> struct.
        /// </summary>
        /// <param name="decision">Sampling decision.</param>
        /// <param name="reason">Optional decision reason.</param>
        public SamplingResult(SamplingDecision decision, string? reason = null)
        {
            Decision = decision;
            Reason = reason;
        }

        /// <summary>
        /// Gets the sampling decision.
        /// </summary>
        public SamplingDecision Decision { get; }

        /// <summary>
        /// Gets the decision reason.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Creates a drop decision.
        /// </summary>
        /// <param name="reason">Optional reason.</param>
        /// <returns>Sampling result.</returns>
        public static SamplingResult Drop(string? reason = null)
        {
            return new SamplingResult(SamplingDecision.Drop, reason);
        }

        /// <summary>
        /// Creates a record decision.
        /// </summary>
        /// <param name="reason">Optional reason.</param>
        /// <returns>Sampling result.</returns>
        public static SamplingResult Sample(string? reason = null)
        {
            return new SamplingResult(SamplingDecision.RecordAndSample, reason);
        }
    }
}
