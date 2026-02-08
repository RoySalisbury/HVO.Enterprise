using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.Benchmarks.Correlation
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-002")]
    public class CorrelationBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private const int SlowOperationsPerInvoke = 100;
        private readonly Consumer _consumer = new Consumer();
        private readonly string _correlationId = "bench-correlation-id";
        private readonly ActivitySource _activitySource = new ActivitySource("HVO.Bench.Correlation");
        private Activity? _activity;

        [GlobalSetup]
        public void Setup()
        {
            CorrelationContext.Current = _correlationId;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _activity = _activitySource.StartActivity("bench");
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _activity?.Dispose();
            _activity = null;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("HotPath")]
        public void Current_Read()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var value = CorrelationContext.Current;
                _consumer.Consume(value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("HotPath")]
        public void Scope_CreateDispose()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var scope = CorrelationContext.BeginScope(_correlationId);
                _consumer.Consume(CorrelationContext.Current);
                scope.Dispose();
            }
        }

        [Benchmark(OperationsPerInvoke = SlowOperationsPerInvoke)]
        [BenchmarkCategory("HotPath")]
        public void Current_AutoGenerate()
        {
            for (int i = 0; i < SlowOperationsPerInvoke; i++)
            {
                CorrelationContext.Clear();
                var value = CorrelationContext.Current;
                _consumer.Consume(value);
            }
        }

        [Benchmark(OperationsPerInvoke = SlowOperationsPerInvoke)]
        [BenchmarkCategory("HotPath")]
        public void Current_FromActivity()
        {
            for (int i = 0; i < SlowOperationsPerInvoke; i++)
            {
                CorrelationContext.Clear();
                using var activity = _activitySource.StartActivity("bench");
                var value = CorrelationContext.Current;
                _consumer.Consume(value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Activity")]
        public void Activity_TagAddition()
        {
            var activity = _activity;
            if (activity == null)
                return;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                activity.SetTag("correlation.id", _correlationId);
            }
            _consumer.Consume(activity.GetTagItem("correlation.id"));
        }
    }
}
