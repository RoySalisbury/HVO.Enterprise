using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Tracks sampling statistics.
    /// </summary>
    public static class SamplingMetrics
    {
        private static readonly ICounter<long> TotalOperations;
        private static readonly ICounter<long> SampledOperations;
        private static readonly ICounter<long> DroppedOperations;

        static SamplingMetrics()
        {
            var recorder = MetricRecorderFactory.Instance;

            TotalOperations = recorder.CreateCounter(
                "telemetry.sampling.total",
                "operations",
                "Total operations evaluated for sampling");

            SampledOperations = recorder.CreateCounter(
                "telemetry.sampling.sampled",
                "operations",
                "Operations that were sampled");

            DroppedOperations = recorder.CreateCounter(
                "telemetry.sampling.dropped",
                "operations",
                "Operations that were dropped");
        }

        /// <summary>
        /// Records a sampling decision.
        /// </summary>
        /// <param name="result">Sampling result.</param>
        /// <param name="activitySourceName">ActivitySource name.</param>
        public static void RecordDecision(SamplingResult result, string activitySourceName)
        {
            var tag = new MetricTag("source", activitySourceName);

            TotalOperations.Add(1, tag);

            if (result.Decision == SamplingDecision.RecordAndSample)
            {
                SampledOperations.Add(1, tag);
            }
            else
            {
                DroppedOperations.Add(1, tag);
            }
        }
    }
}
