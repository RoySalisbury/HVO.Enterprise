using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Metrics
{
    internal sealed class MetricTagCardinalityTracker
    {
        private readonly ConcurrentDictionary<string, byte> _uniqueCombinations;
        private readonly ConcurrentDictionary<string, int> _uniqueCounts;
        private readonly ConcurrentDictionary<string, bool> _warningsLogged;
        private readonly ILogger _logger;
        private readonly int _warningThreshold;
        private readonly int _maxTrackedCombinations;

        public MetricTagCardinalityTracker(ILogger logger, int warningThreshold = 100, int maxTrackedCombinations = 1000)
        {
            _logger = logger ?? NullLogger.Instance;
            _warningThreshold = warningThreshold;
            _maxTrackedCombinations = maxTrackedCombinations;
            _uniqueCombinations = new ConcurrentDictionary<string, byte>();
            _uniqueCounts = new ConcurrentDictionary<string, int>();
            _warningsLogged = new ConcurrentDictionary<string, bool>();
        }

        public void Track(string metricName, in MetricTag tag1)
        {
            Track(metricName, MetricTagKeyBuilder.BuildTaggedName(metricName, in tag1));
        }

        public void Track(string metricName, in MetricTag tag1, in MetricTag tag2)
        {
            Track(metricName, MetricTagKeyBuilder.BuildTaggedName(metricName, in tag1, in tag2));
        }

        public void Track(string metricName, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
        {
            Track(metricName, MetricTagKeyBuilder.BuildTaggedName(metricName, in tag1, in tag2, in tag3));
        }

        public void Track(string metricName, MetricTag[] tags)
        {
            Track(metricName, MetricTagKeyBuilder.BuildTaggedName(metricName, tags));
        }

        private void Track(string metricName, string tagKey)
        {
            // Stop tracking new combinations once max is reached to prevent unbounded memory growth
            if (_uniqueCombinations.Count >= _maxTrackedCombinations)
                return;

            if (_uniqueCombinations.TryAdd(tagKey, 0))
            {
                var count = _uniqueCounts.AddOrUpdate(metricName, 1, (_, existing) => existing + 1);
                if (count >= _warningThreshold && _warningsLogged.TryAdd(metricName, true))
                {
                    _logger.LogWarning(
                        "Metric {MetricName} exceeded tag cardinality threshold {Threshold}. Unique tag combinations: {Count}",
                        metricName,
                        _warningThreshold,
                        count);
                }
            }
        }
    }
}
