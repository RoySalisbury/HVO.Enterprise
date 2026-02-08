using System;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Sampling;

namespace HVO.Enterprise.Telemetry.Benchmarks.Sampling
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-010")]
    public class SamplingBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();
        private ProbabilisticSampler _probabilisticSampler = null!;
        private ConditionalSampler _conditionalSampler = null!;
        private PerSourceSampler _perSourceSampler = null!;
        private AdaptiveSampler _adaptiveSampler = null!;
        private SamplingContext[] _contexts = null!;
        private SamplingContext _errorContext = null!;
        private SamplingContext _sourceContext = null!;
        private FieldInfo _lastAdjustmentTicksField = null!;
        private FieldInfo _totalOperationsField = null!;
        private FieldInfo _sampledOperationsField = null!;

        [GlobalSetup]
        public void Setup()
        {
            _probabilisticSampler = new ProbabilisticSampler(0.5);
            _conditionalSampler = new ConditionalSampler(_probabilisticSampler, alwaysSampleErrors: true);
            _perSourceSampler = new PerSourceSampler(new ProbabilisticSampler(0.25));
            _perSourceSampler.ConfigureSource("bench", new ProbabilisticSampler(0.1));
            _adaptiveSampler = new AdaptiveSampler(targetOperationsPerSecond: 1000, minSamplingRate: 0.01, maxSamplingRate: 1.0);

            _lastAdjustmentTicksField = typeof(AdaptiveSampler).GetField("_lastAdjustmentTicks", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _totalOperationsField = typeof(AdaptiveSampler).GetField("_totalOperations", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _sampledOperationsField = typeof(AdaptiveSampler).GetField("_sampledOperations", BindingFlags.NonPublic | BindingFlags.Instance)!;

            _contexts = new SamplingContext[OperationsPerInvoke];
            for (int i = 0; i < _contexts.Length; i++)
            {
                _contexts[i] = new SamplingContext(
                    ActivityTraceId.CreateRandom(),
                    "bench",
                    "bench",
                    ActivityKind.Internal,
                    tags: null);
            }

            var errorTags = new ActivityTagsCollection { { "error", true } };
            _errorContext = new SamplingContext(
                ActivityTraceId.CreateRandom(),
                "bench",
                "bench",
                ActivityKind.Internal,
                errorTags);

            _sourceContext = new SamplingContext(
                ActivityTraceId.CreateRandom(),
                "bench",
                "bench",
                ActivityKind.Internal,
                tags: null);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _lastAdjustmentTicksField.SetValue(_adaptiveSampler, DateTime.UtcNow.AddSeconds(-2).Ticks);
            _totalOperationsField.SetValue(_adaptiveSampler, 2000L);
            _sampledOperationsField.SetValue(_adaptiveSampler, 1500L);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Decision")]
        public void Probabilistic_ShouldSample()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = _probabilisticSampler.ShouldSample(_contexts[i]);
                _consumer.Consume(result.Decision);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Decision")]
        public void Conditional_ShouldSample_ErrorTag()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = _conditionalSampler.ShouldSample(_errorContext);
                _consumer.Consume(result.Decision);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Decision")]
        public void PerSource_ShouldSample()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = _perSourceSampler.ShouldSample(_sourceContext);
                _consumer.Consume(result.Decision);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Decision")]
        public void Adaptive_ShouldSample()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = _adaptiveSampler.ShouldSample(_contexts[i]);
                _consumer.Consume(result.Decision);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Adjustment")]
        public void Adaptive_AdjustmentTriggered()
        {
            var result = _adaptiveSampler.ShouldSample(_contexts[0]);
            _consumer.Consume(result.Decision);
        }
    }
}
