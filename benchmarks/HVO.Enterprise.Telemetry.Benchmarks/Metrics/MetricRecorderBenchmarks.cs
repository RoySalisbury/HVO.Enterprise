using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Benchmarks.Metrics
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-006")]
    public class MetricRecorderBenchmarks
    {
        private readonly ICounter<long> _counter;
        private readonly IHistogram<long> _histogram;
        private readonly IHistogram<double> _doubleHistogram;
        private readonly MetricTag _tag1 = new MetricTag("status", 200);
        private readonly MetricTag _tag2 = new MetricTag("region", "east");
        private readonly MetricTag _tag3 = new MetricTag("route", "/bench");

        public MetricRecorderBenchmarks()
        {
            var recorder = MetricRecorderFactory.Instance;
            _counter = recorder.CreateCounter("bench.counter");
            _histogram = recorder.CreateHistogram("bench.histogram");
            _doubleHistogram = recorder.CreateHistogramDouble("bench.histogram.double");
        }

        [Benchmark]
        [BenchmarkCategory("Acceptance")]
        public void Counter_Add_NoTags()
        {
            _counter.Add(1);
        }

        [Benchmark]
        [BenchmarkCategory("Acceptance")]
        public void Counter_Add_TwoTags()
        {
            _counter.Add(1, in _tag1, in _tag2);
        }

        [Benchmark]
        [BenchmarkCategory("Acceptance")]
        public void Counter_Add_OneTag()
        {
            _counter.Add(1, in _tag1);
        }

        [Benchmark]
        [BenchmarkCategory("Acceptance")]
        public void Counter_Add_ThreeTags()
        {
            _counter.Add(1, in _tag1, in _tag2, in _tag3);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public void Histogram_Record_NoTags()
        {
            _histogram.Record(42);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public void Histogram_Record_OneTag()
        {
            _histogram.Record(42, in _tag1);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public void Histogram_Record_TwoTags()
        {
            _histogram.Record(42, in _tag1, in _tag2);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public void Histogram_Record_ThreeTags()
        {
            _histogram.Record(42, in _tag1, in _tag2, in _tag3);
        }

        [Benchmark]
        [BenchmarkCategory("Logic")]
        public void HistogramDouble_Record_NoTags()
        {
            _doubleHistogram.Record(42.5);
        }
    }
}
