using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Lifecycle;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Benchmarks.Lifecycle
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-005")]
    public class LifecycleBenchmarks
    {
        private const int OperationsPerInvoke = 100;
        private readonly Consumer _consumer = new Consumer();
        private TelemetryBackgroundWorker _worker = null!;
        private TelemetryLifetimeManager _manager = null!;
        private TelemetryBackgroundWorker _shutdownWorker = null!;
        private TelemetryLifetimeManager _shutdownManager = null!;

        [GlobalSetup]
        public void Setup()
        {
            _worker = new TelemetryBackgroundWorker(capacity: 16, logger: NullLogger<TelemetryBackgroundWorker>.Instance);
            _manager = new TelemetryLifetimeManager(_worker, NullLogger<TelemetryLifetimeManager>.Instance);
        }

        [IterationSetup(Target = nameof(ShutdownAsync_EmptyQueue))]
        public void SetupShutdown()
        {
            _shutdownWorker = new TelemetryBackgroundWorker(capacity: 1, logger: NullLogger<TelemetryBackgroundWorker>.Instance);
            _shutdownManager = new TelemetryLifetimeManager(_shutdownWorker, NullLogger<TelemetryLifetimeManager>.Instance);
        }

        [IterationCleanup(Target = nameof(ShutdownAsync_EmptyQueue))]
        public void CleanupShutdown()
        {
            _shutdownManager.Dispose();
            _shutdownWorker.Dispose();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _manager.Dispose();
            _worker.Dispose();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Registration")]
        public void Register_Unregister_Events()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var manager = new TelemetryLifetimeManager(_worker, NullLogger<TelemetryLifetimeManager>.Instance);
                manager.Dispose();
                _consumer.Consume(manager.IsShuttingDown);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Shutdown")]
        public Task ShutdownAsync_EmptyQueue()
        {
            return _shutdownManager.ShutdownAsync(TimeSpan.FromMilliseconds(10));
        }
    }
}
