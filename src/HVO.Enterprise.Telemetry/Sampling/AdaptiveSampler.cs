using System;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Adaptive sampler that adjusts sampling rate based on throughput.
    /// </summary>
    public sealed class AdaptiveSampler : ISampler
    {
        private readonly int _targetOperationsPerSecond;
        private readonly double _minSamplingRate;
        private readonly double _maxSamplingRate;
        private readonly Func<DateTimeOffset> _timeProvider;
        private readonly object _adjustmentLock = new object();

        private long _totalOperations;
        private long _sampledOperations;
        private long _lastAdjustmentTicks;
        private double _currentSamplingRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveSampler"/> class.
        /// </summary>
        /// <param name="targetOperationsPerSecond">Target sampled operations per second.</param>
        /// <param name="minSamplingRate">Minimum sampling rate.</param>
        /// <param name="maxSamplingRate">Maximum sampling rate.</param>
        public AdaptiveSampler(
            int targetOperationsPerSecond = 1000,
            double minSamplingRate = 0.01,
            double maxSamplingRate = 1.0)
            : this(targetOperationsPerSecond, minSamplingRate, maxSamplingRate, () => DateTimeOffset.UtcNow)
        {
        }

        internal AdaptiveSampler(
            int targetOperationsPerSecond,
            double minSamplingRate,
            double maxSamplingRate,
            Func<DateTimeOffset> timeProvider)
        {
            if (targetOperationsPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetOperationsPerSecond));
            if (minSamplingRate < 0.0 || minSamplingRate > 1.0)
                throw new ArgumentOutOfRangeException(nameof(minSamplingRate));
            if (maxSamplingRate < 0.0 || maxSamplingRate > 1.0)
                throw new ArgumentOutOfRangeException(nameof(maxSamplingRate));
            if (timeProvider == null)
                throw new ArgumentNullException(nameof(timeProvider));

            _targetOperationsPerSecond = targetOperationsPerSecond;
            _minSamplingRate = Math.Min(minSamplingRate, maxSamplingRate);
            _maxSamplingRate = Math.Max(minSamplingRate, maxSamplingRate);
            _currentSamplingRate = _maxSamplingRate;
            _timeProvider = timeProvider;
            _lastAdjustmentTicks = _timeProvider().UtcTicks;
        }

        /// <summary>
        /// Gets the current adaptive sampling rate.
        /// </summary>
        public double CurrentSamplingRate => Volatile.Read(ref _currentSamplingRate);

        /// <inheritdoc />
        public SamplingResult ShouldSample(SamplingContext context)
        {
            Interlocked.Increment(ref _totalOperations);

            var nowTicks = _timeProvider().UtcTicks;
            var elapsedTicks = nowTicks - Interlocked.Read(ref _lastAdjustmentTicks);
            if (elapsedTicks >= TimeSpan.TicksPerSecond)
            {
                AdjustSamplingRate(new TimeSpan(elapsedTicks), nowTicks);
            }

            // Inline probabilistic sampling logic to avoid allocation
            var currentRate = CurrentSamplingRate;
            SamplingDecision decision;
            string reason;

            if (currentRate >= 1.0)
            {
                decision = SamplingDecision.RecordAndSample;
                reason = "100% sampling";
            }
            else if (currentRate <= 0.0)
            {
                decision = SamplingDecision.Drop;
                reason = "0% sampling";
            }
            else
            {
                var threshold = (ulong)(currentRate * ulong.MaxValue);
                var traceIdValue = ProbabilisticSampler.ExtractTraceIdValueInternal(context.TraceId);
                var shouldSample = traceIdValue <= threshold;
                
                decision = shouldSample ? SamplingDecision.RecordAndSample : SamplingDecision.Drop;
                reason = shouldSample 
                    ? string.Format("TraceId hash below threshold (rate: {0:P1})", currentRate)
                    : string.Format("TraceId hash above threshold (rate: {0:P1})", currentRate);
            }

            if (decision == SamplingDecision.RecordAndSample)
            {
                Interlocked.Increment(ref _sampledOperations);
            }

            return new SamplingResult(
                decision,
                string.Format("Adaptive: {0} (current rate: {1:P1})", reason, CurrentSamplingRate));
        }

        private void AdjustSamplingRate(TimeSpan elapsed, long nowTicks)
        {
            lock (_adjustmentLock)
            {
                var lastTicks = Interlocked.Read(ref _lastAdjustmentTicks);
                if (nowTicks - lastTicks < TimeSpan.TicksPerSecond)
                    return;

                var totalOps = Interlocked.Read(ref _totalOperations);
                var sampledOps = Interlocked.Read(ref _sampledOperations);

                var actualOpsPerSecond = totalOps / elapsed.TotalSeconds;
                var sampledOpsPerSecond = sampledOps / elapsed.TotalSeconds;

                var currentRate = Volatile.Read(ref _currentSamplingRate);
                var newRate = currentRate;

                if (sampledOpsPerSecond > _targetOperationsPerSecond && actualOpsPerSecond > 0)
                {
                    var targetRate = _targetOperationsPerSecond / actualOpsPerSecond;
                    newRate = Math.Max(_minSamplingRate, targetRate);
                }
                else if (sampledOpsPerSecond < _targetOperationsPerSecond * 0.8)
                {
                    newRate = Math.Min(_maxSamplingRate, currentRate * 1.2);
                }

                Volatile.Write(ref _currentSamplingRate, newRate);

                Interlocked.Exchange(ref _totalOperations, 0);
                Interlocked.Exchange(ref _sampledOperations, 0);
                Interlocked.Exchange(ref _lastAdjustmentTicks, nowTicks);
            }
        }
    }
}
