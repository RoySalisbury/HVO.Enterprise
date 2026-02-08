using System;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Sampling
{
    [TestClass]
    public class SamplingActivitySourceExtensionsTests
    {
        [TestMethod]
        public void SamplingActivitySourceExtensions_BuildSampler_UsesGlobalOverride()
        {
            var provider = new ConfigurationProvider();
            provider.SetGlobalConfiguration(new OperationConfiguration { SamplingRate = 0.0 });

            var options = new TelemetryOptions
            {
                DefaultSamplingRate = 1.0
            };

            var sampler = SamplingActivitySourceExtensions.BuildSampler(options, provider);
            
            // Use a fixed TraceId to ensure deterministic behavior (32 hex chars)
            var traceId = System.Diagnostics.ActivityTraceId.CreateFromString("00000000000000000000000000000001");
            var context = new SamplingContext(traceId, "op", "source", System.Diagnostics.ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            // With 0.0 global override, should always drop (regardless of DefaultSamplingRate = 1.0)
            Assert.AreEqual(SamplingDecision.Drop, result.Decision);
        }

        [TestMethod]
        public void SamplingActivitySourceExtensions_BuildSampler_RespectsPerSourceRate()
        {
            var options = new TelemetryOptions
            {
                DefaultSamplingRate = 1.0
            };
            options.Sampling["source"] = new SamplingOptions { Rate = 0.0, AlwaysSampleErrors = false };

            var sampler = SamplingActivitySourceExtensions.BuildSampler(options, new ConfigurationProvider());
            var context = new SamplingContext(System.Diagnostics.ActivityTraceId.CreateRandom(), "op", "source", System.Diagnostics.ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.Drop, result.Decision);
        }
    }
}
