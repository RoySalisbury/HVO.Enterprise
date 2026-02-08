using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Sampler that applies conditional logic (always sample errors, slow operations, etc.).
    /// </summary>
    public sealed class ConditionalSampler : ISampler
    {
        private readonly ISampler _baseSampler;
        private readonly bool _alwaysSampleErrors;
        private readonly TimeSpan? _slowOperationThreshold;
        private readonly Func<SamplingContext, bool>? _customPredicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalSampler"/> class.
        /// </summary>
        /// <param name="baseSampler">Base sampler to use when conditions do not match.</param>
        /// <param name="alwaysSampleErrors">Whether to always sample errors.</param>
        /// <param name="slowOperationThreshold">Optional slow operation threshold.</param>
        /// <param name="customPredicate">Optional custom predicate.</param>
        public ConditionalSampler(
            ISampler baseSampler,
            bool alwaysSampleErrors = true,
            TimeSpan? slowOperationThreshold = null,
            Func<SamplingContext, bool>? customPredicate = null)
        {
            _baseSampler = baseSampler ?? throw new ArgumentNullException(nameof(baseSampler));
            _alwaysSampleErrors = alwaysSampleErrors;
            _slowOperationThreshold = slowOperationThreshold;
            _customPredicate = customPredicate;
        }

        /// <inheritdoc />
        public SamplingResult ShouldSample(SamplingContext context)
        {
            if (_customPredicate != null && _customPredicate(context))
                return SamplingResult.Sample("Custom predicate matched");

            if (_alwaysSampleErrors && context.Tags != null)
            {
                foreach (var tag in context.Tags)
                {
                    if (string.Equals(tag.Key, "error", StringComparison.OrdinalIgnoreCase)
                        && tag.Value is bool errorValue && errorValue)
                        return SamplingResult.Sample("Error detected");

                    if (string.Equals(tag.Key, "exception.type", StringComparison.OrdinalIgnoreCase))
                        return SamplingResult.Sample("Exception detected");
                }
            }

            if (_slowOperationThreshold.HasValue && context.Tags != null)
            {
                foreach (var tag in context.Tags)
                {
                    if (!string.Equals(tag.Key, "duration.ms", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var durationMs = ExtractDurationMs(tag.Value);
                    if (!durationMs.HasValue)
                        continue;

                    if (durationMs.Value > _slowOperationThreshold.Value.TotalMilliseconds)
                    {
                        return SamplingResult.Sample(
                            "Slow operation (" + durationMs.Value.ToString("F0") + "ms > " +
                            _slowOperationThreshold.Value.TotalMilliseconds.ToString("F0") + "ms)");
                    }
                }
            }

            return _baseSampler.ShouldSample(context);
        }

        private static double? ExtractDurationMs(object? value)
        {
            if (value == null)
                return null;

            if (value is long longValue)
                return longValue;

            if (value is double doubleValue)
                return doubleValue;

            if (value is int intValue)
                return intValue;

            return null;
        }
    }
}
