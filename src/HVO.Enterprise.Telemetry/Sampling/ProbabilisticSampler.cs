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
        private readonly double _samplingRate;
        private readonly ulong _threshold;

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
        }

        /// <inheritdoc />
        public SamplingResult ShouldSample(SamplingContext context)
        {
            if (_samplingRate >= 1.0)
                return SamplingResult.Sample("100% sampling");

            if (_samplingRate <= 0.0)
                return SamplingResult.Drop("0% sampling");

            var traceIdValue = ExtractTraceIdValue(context.TraceId);

            var shouldSample = traceIdValue <= _threshold;

            return shouldSample
                ? SamplingResult.Sample("TraceId hash below threshold (rate: " + _samplingRate.ToString("P1") + ")")
                : SamplingResult.Drop("TraceId hash above threshold (rate: " + _samplingRate.ToString("P1") + ")");
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
#if NET8_0_OR_GREATER
            // On .NET 8+, use Span-based parsing to avoid allocations
            Span<byte> bytes = stackalloc byte[16];
            traceId.CopyTo(bytes);

            // Interpret the lower 8 bytes as a big-endian unsigned integer
            ulong value = 0;
            for (int i = 8; i < 16; i++)
            {
                value = (value << 8) | bytes[i];
            }

            return value;
#else
            // Fallback for .NET Standard 2.0: use string parsing
            var hex = traceId.ToString();
            if (string.IsNullOrEmpty(hex) || hex.Length < 16)
                return 0;

            var start = hex.Length - 16;
            var slice = hex.Substring(start, 16);

            if (ulong.TryParse(slice, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var value))
                return value;

            return 0;
#endif
        }
    }
}
