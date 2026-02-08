namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Interface for sampling strategies.
    /// </summary>
    public interface ISampler
    {
        /// <summary>
        /// Makes a sampling decision for the given context.
        /// </summary>
        /// <param name="context">Sampling context.</param>
        /// <returns>Sampling result.</returns>
        SamplingResult ShouldSample(SamplingContext context);
    }
}
