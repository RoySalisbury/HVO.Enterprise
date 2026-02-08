using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Benchmarks.Metrics
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-004")]
    public class TelemetryBackgroundWorkerBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private const int DropWorkerCapacity = 32;
        private const int ThroughputOperationsPerInvoke = 10000;

        private readonly Consumer _consumer = new Consumer();
        private TelemetryBackgroundWorker _fastWorker = null!;
        private TelemetryBackgroundWorker _dropWorker = null!;
        private TelemetryBackgroundWorker _throughputWorker = null!;
        private ManualResetEventSlim _blocker = null!;
        private BlockWorkItem _blockItem = null!;
        private NoopWorkItem _noopItem = null!;

        [GlobalSetup]
        public void Setup()
        {
            _fastWorker = new TelemetryBackgroundWorker(
                capacity: 1024,
                logger: NullLogger<TelemetryBackgroundWorker>.Instance);

            _dropWorker = new TelemetryBackgroundWorker(
                capacity: DropWorkerCapacity,
                logger: NullLogger<TelemetryBackgroundWorker>.Instance);

            _throughputWorker = new TelemetryBackgroundWorker(
                capacity: 200000,
                logger: NullLogger<TelemetryBackgroundWorker>.Instance);

            _blocker = new ManualResetEventSlim(false);
            _blockItem = new BlockWorkItem(_blocker);
            _noopItem = new NoopWorkItem();

            _dropWorker.TryEnqueue(_blockItem);
            _blockItem.WaitForStart();

            for (int i = 0; i < DropWorkerCapacity; i++)
            {
                _dropWorker.TryEnqueue(_noopItem);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _blocker.Set();
            _fastWorker.Dispose();
            _dropWorker.Dispose();
            _throughputWorker.Dispose();
            _blocker.Dispose();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Enqueue")]
        public void TryEnqueue_FastPath()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = _fastWorker.TryEnqueue(_noopItem);
                _consumer.Consume(result);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Enqueue")]
        public void TryEnqueue_DropPath()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var result = _dropWorker.TryEnqueue(_noopItem);
                _consumer.Consume(result);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Queue")]
        public void QueueDepth_Read()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _consumer.Consume(_fastWorker.QueueDepth);
            }
        }

        [Benchmark(OperationsPerInvoke = ThroughputOperationsPerInvoke)]
        [BenchmarkCategory("Throughput")]
        public void TryEnqueue_Throughput()
        {
            var enqueued = 0;
            for (int i = 0; i < ThroughputOperationsPerInvoke; i++)
            {
                if (_throughputWorker.TryEnqueue(_noopItem))
                {
                    enqueued++;
                }
            }
            _consumer.Consume(enqueued);
        }

        private sealed class NoopWorkItem : TelemetryWorkItem
        {
            public override string OperationType => "noop";

            public override void Execute()
            {
            }
        }

        private sealed class BlockWorkItem : TelemetryWorkItem
        {
            private readonly ManualResetEventSlim _blocker;
            private readonly ManualResetEventSlim _started = new ManualResetEventSlim(false);

            public BlockWorkItem(ManualResetEventSlim blocker)
            {
                _blocker = blocker;
            }

            public override string OperationType => "block";

            public override void Execute()
            {
                _started.Set();
                _blocker.Wait();
            }

            public void WaitForStart()
            {
                _started.Wait();
            }
        }
    }
}
