using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    [TestClass]
    public class MetricRecorderPerformanceTests
    {
        [TestMethod]
        public void Counter_Add_NoTags_IsFast()
        {
            var recorder = MetricRecorderFactory.Instance;
            var counter = recorder.CreateCounter("perf.counter.speed");

            // Warmup
            for (int i = 0; i < 1000; i++)
            {
                counter.Add(1);
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                counter.Add(1);
            }
            sw.Stop();

            Assert.IsTrue(sw.ElapsedMilliseconds < 500,
                $"Expected counter adds to be fast; took {sw.ElapsedMilliseconds}ms.");
        }

        [TestMethod]
        public void Histogram_Record_NoTags_IsFast()
        {
            var recorder = MetricRecorderFactory.Instance;
            var histogram = recorder.CreateHistogram("perf.histogram.speed");

            // Warmup
            for (int i = 0; i < 1000; i++)
            {
                histogram.Record(i);
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                histogram.Record(i);
            }
            sw.Stop();

            Assert.IsTrue(sw.ElapsedMilliseconds < 500,
                $"Expected histogram records to be fast; took {sw.ElapsedMilliseconds}ms.");
        }
    }
}
