using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Benchmarks.Metrics
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-006")]
    public class MetricRecorderFactoryBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Runtime")]
        public void Instance_Access()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var recorder = MetricRecorderFactory.Instance;
                _consumer.Consume(recorder.GetType());
            }
        }
    }
}
