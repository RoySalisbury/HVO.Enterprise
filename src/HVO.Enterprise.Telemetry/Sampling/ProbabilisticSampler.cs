using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Deterministic probabilistic sampler based on TraceId.
    /// Ensures consistent sampling decisions across distributed services.
    /// </summary>
    public sealed class ProbabilisticSampler : ISampler
    {
        private static readonly SamplingResult AlwaysSampleResult = SamplingResult.Sample("100% sampling");
        private static readonly SamplingResult AlwaysDropResult = SamplingResult.Drop("0% sampling");

        private readonly double _samplingRate;
        private readonly ulong _threshold;
        private readonly SamplingResult _cachedSampleResult;
        private readonly SamplingResult _cachedDropResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilisticSampler"/> class.
        /// </summary>
        /// <param name="samplingRate">Sampling rate from 0.0 to 1.0.</param>
        public ProbabilisticSampler(double samplingRate)
        {
            if (samplingRate < 0.0 || samplingRate > 1.0)
                throw new ArgumentOutOfRangeException(nameof(samplingRate), "Sampling rate must be between 0.0 and 1.0.");

            _samplingRate = samplingRate;
            _threshold = (ulong)(samplingRate * ulong.MaxValue);

            // Pre-compute reason strings to avoid per-call allocation
            var rateStr = samplingRate.ToString("P1");
            _cachedSampleResult = new SamplingResult(SamplingDecision.RecordAndSample, "TraceId hash below threshold (rate: " + rateStr + ")");
            _cachedDropResult = new SamplingResult(SamplingDecision.Drop, "TraceId hash above threshold (rate: " + rateStr + ")");
        }

        /// <inheritdoc />
        public SamplingResult ShouldSample(SamplingContext context)
        {
            if (_samplingRate >= 1.0)
                return AlwaysSampleResult;

            if (_samplingRate <= 0.0)
                return AlwaysDropResult;

            var traceIdValue = ExtractTraceIdValue(context.TraceId);

            return traceIdValue <= _threshold
                ? _cachedSampleResult
                : _cachedDropResult;
        }

        private static ulong ExtractTraceIdValue(ActivityTraceId traceId)
        {
            return ExtractTraceIdValueInternal(traceId);
        }

        /// <summary>
        /// Extracts a deterministic numeric value from a TraceId for probabilistic sampling.
        /// Internal for use by AdaptiveSampler.
        /// </summary>
        internal static ulong ExtractTraceIdValueInternal(ActivityTraceId traceId)
        {
            // Use string parsing for .NET Standard 2.0 compatibility
            var hex = traceId.ToString();
            if (string.IsNullOrEmpty(hex) || hex.Length < 16)
                return 0;

            // Parse the last 16 hex chars directly without Substring allocation
            ulong value = 0;
            var start = hex.Length - 16;
            for (int i = start; i < hex.Length; i++)
            {
                value <<= 4;
                var c = hex[i];
                if (c >= '0' && c <= '9')
                    value |= (uint)(c - '0');
                else if (c >= 'a' && c <= 'f')
                    value |= (uint)(c - 'a' + 10);
                else if (c >= 'A' && c <= 'F')
                    value |= (uint)(c - 'A' + 10);
                else
                    return 0;
            }
            return value;
        }
    }
}
