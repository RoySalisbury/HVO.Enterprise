using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Configuration;

namespace HVO.Enterprise.Telemetry.Benchmarks.Configuration
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-009")]
    public class ConfigurationBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();
        private ConfigurationProvider _defaultProvider = null!;
        private ConfigurationProvider _configuredProvider = null!;
        private OperationConfiguration _callConfig = null!;
        private MethodInfo _method = null!;
        private ConfigurationProvider _attributeProvider = null!;
        private TelemetryOptions _options = null!;

        [GlobalSetup]
        public void Setup()
        {
            _method = typeof(ConfigurationBenchmarkTarget).GetMethod(nameof(ConfigurationBenchmarkTarget.DoWork))!;

            _defaultProvider = new ConfigurationProvider();

            _attributeProvider = new ConfigurationProvider();

            _configuredProvider = new ConfigurationProvider();
            _configuredProvider.SetGlobalConfiguration(new OperationConfiguration { SamplingRate = 0.5 });
            _configuredProvider.SetNamespaceConfiguration("HVO.Enterprise.Telemetry.Benchmarks.*",
                new OperationConfiguration { Enabled = true, SamplingRate = 0.2 });
            _configuredProvider.SetTypeConfiguration(typeof(ConfigurationBenchmarkTarget),
                new OperationConfiguration { SamplingRate = 0.8 });
            _configuredProvider.SetMethodConfiguration(_method,
                new OperationConfiguration { SamplingRate = 0.9, TimeoutThresholdMs = 10 });

            _callConfig = new OperationConfiguration { SamplingRate = 1.0, Enabled = true };

            _options = new TelemetryOptions
            {
                DefaultSamplingRate = 0.1,
                Queue = new QueueOptions { Capacity = 1000, BatchSize = 100 },
                Metrics = new MetricsOptions { CollectionIntervalSeconds = 10 }
            };
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Lookup")]
        public void GetEffectiveConfiguration_Default()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var config = _defaultProvider.GetEffectiveConfiguration(typeof(ConfigurationBenchmarkTarget), _method, null);
                _consumer.Consume(config.SamplingRate);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Lookup")]
        public void GetEffectiveConfiguration_AllOverrides()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var config = _configuredProvider.GetEffectiveConfiguration(typeof(ConfigurationBenchmarkTarget), _method, _callConfig);
                _consumer.Consume(config.SamplingRate);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Merge")]
        public void OperationConfiguration_Merge()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var merged = _callConfig.MergeWith(new OperationConfiguration { SamplingRate = 0.25, Enabled = true });
                _consumer.Consume(merged.SamplingRate);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Validate")]
        [BenchmarkCategory("US-008")]
        public void TelemetryOptions_Validate()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _options.Validate();
                _consumer.Consume(_options.DefaultSamplingRate);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Reflection")]
        public void ApplyAttributeConfiguration()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var provider = new ConfigurationProvider();
                provider.ApplyAttributeConfiguration(typeof(AnnotatedBenchmarkTarget));
                _consumer.Consume(provider.GetAllConfigurations().Count);
            }
        }

        private sealed class ConfigurationBenchmarkTarget
        {
            public void DoWork()
            {
            }
        }

        [TelemetryConfiguration(SamplingRate = 0.75, Enabled = ConfigurationToggle.Enabled, TimeoutThresholdMs = 5)]
        private sealed class AnnotatedBenchmarkTarget
        {
            [TelemetryConfiguration(SamplingRate = 0.5, TimeoutThresholdMs = 10)]
            public void AnnotatedMethod()
            {
            }
        }
    }
}
