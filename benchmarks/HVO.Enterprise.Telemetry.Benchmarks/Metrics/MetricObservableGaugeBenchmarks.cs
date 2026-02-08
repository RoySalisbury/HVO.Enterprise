using System;
using System.Diagnostics.Metrics;
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
    public class MetricObservableGaugeBenchmarks
    {
        private const int OperationsPerInvoke = 200;
        private readonly Consumer _consumer = new Consumer();
        private readonly IMetricRecorder _recorder = MetricRecorderFactory.Instance;
        private MeterListener _listener = null!;
        private IDisposable _gauge = null!;
        private double _lastValue;

        [GlobalSetup]
        public void Setup()
        {
            _gauge = _recorder.CreateObservableGauge("bench.gauge", ObserveValue, "items", "Benchmark gauge");

            _listener = new MeterListener();
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "HVO.Enterprise.Telemetry")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                _lastValue = measurement;
            });

            _listener.Start();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _gauge.Dispose();
            _listener.Dispose();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Gauge")]
        public void ObservableGauge_Callback()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _listener.RecordObservableInstruments();
                _consumer.Consume(_lastValue);
            }
        }

        private double ObserveValue()
        {
            return 42.0;
        }
    }
}
