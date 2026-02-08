using System;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.BackgroundJobs;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.Benchmarks.BackgroundJobs
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-003")]
    public class BackgroundJobBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private const int ActivityOperationsPerInvoke = 100;
        private readonly Consumer _consumer = new Consumer();
        private readonly string _correlationId = "bench-correlation-id";
        private readonly ActivitySource _activitySource = new ActivitySource("HVO.Bench.BackgroundJobs");
        private BackgroundJobContext _context = null!;
        private MethodInfo _attributeMethod = null!;

        [GlobalSetup]
        public void Setup()
        {
            CorrelationContext.Current = _correlationId;
            using var activity = _activitySource.StartActivity("bench");
            _context = BackgroundJobContext.Capture();
            _attributeMethod = typeof(AnnotatedJob).GetMethod(nameof(AnnotatedJob.Execute))!;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Capture")]
        public void Capture_NoActivity()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var context = BackgroundJobContext.Capture();
                _consumer.Consume(context.CorrelationId);
            }
        }

        [Benchmark(OperationsPerInvoke = ActivityOperationsPerInvoke)]
        [BenchmarkCategory("Capture")]
        public void Capture_WithActivity()
        {
            for (int i = 0; i < ActivityOperationsPerInvoke; i++)
            {
                using var activity = _activitySource.StartActivity("bench");
                var context = BackgroundJobContext.Capture();
                _consumer.Consume(context.CorrelationId);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Restore")]
        public void Restore_Scope()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var scope = _context.Restore();
                _consumer.Consume(CorrelationContext.Current);
                scope.Dispose();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Attribute")]
        public void Attribute_ReflectionLookup()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var attribute = (TelemetryJobContextAttribute?)Attribute.GetCustomAttribute(
                    _attributeMethod,
                    typeof(TelemetryJobContextAttribute));
                _consumer.Consume(attribute != null);
            }
        }

        private sealed class AnnotatedJob
        {
            [TelemetryJobContext]
            public void Execute()
            {
            }
        }
    }
}
