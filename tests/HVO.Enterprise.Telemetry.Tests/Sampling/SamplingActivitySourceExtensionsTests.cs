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
            provider.SetGlobalConfiguration(new OperationConfiguration { SamplingRate = 0.25 });

            var options = new TelemetryOptions
            {
                DefaultSamplingRate = 1.0
            };

            var sampler = SamplingActivitySourceExtensions.BuildSampler(options, provider);
            var context = new SamplingContext(System.Diagnostics.ActivityTraceId.CreateRandom(), "op", "source", System.Diagnostics.ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            Assert.IsNotNull(result);
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
