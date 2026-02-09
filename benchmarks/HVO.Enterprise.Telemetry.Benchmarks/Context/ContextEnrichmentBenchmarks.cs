using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Context;

namespace HVO.Enterprise.Telemetry.Benchmarks.Context
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-011")]
    public class ContextEnrichmentBenchmarks
    {
        private const int OperationsPerInvoke = 200;
        private readonly Consumer _consumer = new Consumer();
        private ContextEnricher _minimalEnricher = null!;
        private ContextEnricher _standardEnricher = null!;
        private ContextEnricher _verboseEnricher = null!;
        private Activity _activity = null!;
        private Dictionary<string, object> _properties = null!;

        [GlobalSetup]
        public void Setup()
        {
            _minimalEnricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Minimal });
            _standardEnricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard });
            _verboseEnricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose });
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _activity = new Activity("bench");
            _activity.Start();
            _properties = new Dictionary<string, object>();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _activity.Stop();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Activity")]
        public void EnrichActivity_Minimal()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _minimalEnricher.EnrichActivity(_activity);
            }
            _consumer.Consume(_activity.GetTagItem("service.name")!);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Activity")]
        public void EnrichActivity_Standard()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _standardEnricher.EnrichActivity(_activity);
            }
            _consumer.Consume(_activity.GetTagItem("service.name")!);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Activity")]
        public void EnrichActivity_Verbose()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _verboseEnricher.EnrichActivity(_activity);
            }
            _consumer.Consume(_activity.GetTagItem("service.name")!);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Properties")]
        public void EnrichProperties_Minimal()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _minimalEnricher.EnrichProperties(_properties);
            }
            _consumer.Consume(_properties.Count);
        }
    }
}
