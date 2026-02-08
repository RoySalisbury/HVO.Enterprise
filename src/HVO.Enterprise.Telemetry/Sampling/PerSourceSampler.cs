using System;
using System.Collections.Concurrent;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Sampler that applies different sampling rates per ActivitySource.
    /// </summary>
    public sealed class PerSourceSampler : ISampler
    {
        private readonly ISampler _defaultSampler;
        private readonly ConcurrentDictionary<string, ISampler> _sourceSamplers;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ISampler>> _operationSamplers;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerSourceSampler"/> class.
        /// </summary>
        /// <param name="defaultSampler">Default sampler.</param>
        public PerSourceSampler(ISampler defaultSampler)
        {
            _defaultSampler = defaultSampler ?? throw new ArgumentNullException(nameof(defaultSampler));
            _sourceSamplers = new ConcurrentDictionary<string, ISampler>(StringComparer.OrdinalIgnoreCase);
            _operationSamplers = new ConcurrentDictionary<string, ConcurrentDictionary<string, ISampler>>(
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Configures sampling for a specific ActivitySource.
        /// </summary>
        /// <param name="activitySourceName">ActivitySource name.</param>
        /// <param name="sampler">Sampler to apply.</param>
        public void ConfigureSource(string activitySourceName, ISampler sampler)
        {
            if (string.IsNullOrWhiteSpace(activitySourceName))
                throw new ArgumentNullException(nameof(activitySourceName));
            if (sampler == null)
                throw new ArgumentNullException(nameof(sampler));

            _sourceSamplers[activitySourceName] = sampler;
        }

        /// <summary>
        /// Configures sampling rate for a specific ActivitySource.
        /// </summary>
        /// <param name="activitySourceName">ActivitySource name.</param>
        /// <param name="samplingRate">Sampling rate.</param>
        public void ConfigureSource(string activitySourceName, double samplingRate)
        {
            ConfigureSource(activitySourceName, new ProbabilisticSampler(samplingRate));
        }

        /// <summary>
        /// Configures sampling for a specific operation within an ActivitySource.
        /// </summary>
        /// <param name="activitySourceName">ActivitySource name.</param>
        /// <param name="activityName">Activity name.</param>
        /// <param name="sampler">Sampler to apply.</param>
        public void ConfigureOperation(string activitySourceName, string activityName, ISampler sampler)
        {
            if (string.IsNullOrWhiteSpace(activitySourceName))
                throw new ArgumentNullException(nameof(activitySourceName));
            if (string.IsNullOrWhiteSpace(activityName))
                throw new ArgumentNullException(nameof(activityName));
            if (sampler == null)
                throw new ArgumentNullException(nameof(sampler));

            var operations = _operationSamplers.GetOrAdd(
                activitySourceName,
                _ => new ConcurrentDictionary<string, ISampler>(StringComparer.OrdinalIgnoreCase));

            operations[activityName] = sampler;
        }

        /// <inheritdoc />
        public SamplingResult ShouldSample(SamplingContext context)
        {
            if (_operationSamplers.TryGetValue(context.ActivitySourceName, out var operations)
                && operations.TryGetValue(context.ActivityName, out var operationSampler))
            {
                var result = operationSampler.ShouldSample(context);
                return new SamplingResult(result.Decision, "Operation-specific: " + result.Reason);
            }

            if (_sourceSamplers.TryGetValue(context.ActivitySourceName, out var sampler))
            {
                var result = sampler.ShouldSample(context);
                return new SamplingResult(result.Decision, "Source-specific: " + result.Reason);
            }

            return _defaultSampler.ShouldSample(context);
        }
    }
}
