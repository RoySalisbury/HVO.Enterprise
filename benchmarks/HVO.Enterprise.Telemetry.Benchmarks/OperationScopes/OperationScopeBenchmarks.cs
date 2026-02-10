using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Context;

namespace HVO.Enterprise.Telemetry.Benchmarks.OperationScopes
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-012")]
    public class OperationScopeBenchmarks
    {
        private const int OperationsPerInvoke = 200;
        private readonly Consumer _consumer = new Consumer();
        private OperationScopeFactory _factory = null!;
        private OperationScopeOptions _minimalOptions = null!;
        private OperationScopeOptions _defaultOptions = null!;
        private IOperationScope[] _disposeScopes = null!;

        [GlobalSetup]
        public void Setup()
        {
            _factory = new OperationScopeFactory("HVO.Bench.OperationScope");
            _minimalOptions = new OperationScopeOptions
            {
                CreateActivity = false,
                EnrichContext = false,
                RecordMetrics = false,
                LogEvents = false,
                CaptureExceptions = false,
                SerializeComplexTypes = false
            };
            _defaultOptions = new OperationScopeOptions();
        }

        [IterationSetup(Target = nameof(Dispose_Minimal))]
        public void DisposeIterationSetup()
        {
            _disposeScopes = new IOperationScope[OperationsPerInvoke];
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _disposeScopes[i] = _factory.Begin("bench", _minimalOptions);
            }
        }

        [IterationCleanup(Target = nameof(Dispose_Minimal))]
        public void DisposeIterationCleanup()
        {
            if (_disposeScopes == null)
                return;

            for (int i = 0; i < _disposeScopes.Length; i++)
            {
                _disposeScopes[i]?.Dispose();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Create")]
        public void CreateDispose_Minimal()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                using var scope = _factory.Begin("bench", _minimalOptions);
                _consumer.Consume(scope.CorrelationId);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Create")]
        public void CreateDispose_Default()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                using var scope = _factory.Begin("bench", _defaultOptions);
                _consumer.Consume(scope.CorrelationId);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Tags")]
        public void WithTag_Minimal()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                using var scope = _factory.Begin("bench", _minimalOptions);
                scope.WithTag("key", "value");
                _consumer.Consume(scope.Elapsed);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Properties")]
        public void WithProperty_Minimal()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                using var scope = _factory.Begin("bench", _minimalOptions);
                scope.WithProperty("count", () => 42);
                _consumer.Consume(scope.Elapsed);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Dispose")]
        public void Dispose_Minimal()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _disposeScopes[i].Dispose();
            }
            _consumer.Consume(_disposeScopes.Length);
        }
    }
}
