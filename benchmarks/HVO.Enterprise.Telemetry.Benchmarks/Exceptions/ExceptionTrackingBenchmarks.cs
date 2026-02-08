using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Exceptions;

namespace HVO.Enterprise.Telemetry.Benchmarks.Exceptions
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-007")]
    public class ExceptionTrackingBenchmarks
    {
        private const int OperationsPerInvoke = 200;
        private readonly Consumer _consumer = new Consumer();
        private Exception _exception = null!;
        private ExceptionAggregator _aggregator = null!;
        private string _fingerprint = string.Empty;

        [GlobalSetup]
        public void Setup()
        {
            _exception = new InvalidOperationException("Bench error 12345 at https://example.com");
            _aggregator = new ExceptionAggregator();
            _fingerprint = ExceptionFingerprinter.GenerateFingerprint(_exception);
            _aggregator.RecordException(_exception);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Fingerprint")]
        public void GenerateFingerprint()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var fingerprint = ExceptionFingerprinter.GenerateFingerprint(_exception);
                _consumer.Consume(fingerprint);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Aggregation")]
        public void RecordException()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var group = _aggregator.RecordException(_exception);
                _consumer.Consume(group.Count);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Aggregation")]
        public void GetGroup()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var group = _aggregator.GetGroup(_fingerprint);
                _consumer.Consume(group?.Count ?? 0);
            }
        }
    }
}
