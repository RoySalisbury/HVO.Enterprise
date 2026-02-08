using System.Diagnostics;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Sampling
{
    [TestClass]
    public class PerSourceSamplerTests
    {
        [TestMethod]
        public void PerSourceSampler_UsesSourceSpecificSampler()
        {
            var sampler = new PerSourceSampler(new ProbabilisticSampler(1.0));
            sampler.ConfigureSource("special", new ProbabilisticSampler(0.0));

            var specialContext = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "special", ActivityKind.Internal);
            var defaultContext = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "default", ActivityKind.Internal);

            var specialResult = sampler.ShouldSample(specialContext);
            var defaultResult = sampler.ShouldSample(defaultContext);

            Assert.AreEqual(SamplingDecision.Drop, specialResult.Decision);
            Assert.AreEqual(SamplingDecision.RecordAndSample, defaultResult.Decision);
        }

        [TestMethod]
        public void PerSourceSampler_UsesOperationSpecificSampler()
        {
            var sampler = new PerSourceSampler(new ProbabilisticSampler(1.0));
            sampler.ConfigureOperation("source", "operation", new ProbabilisticSampler(0.0));

            var operationContext = new SamplingContext(ActivityTraceId.CreateRandom(), "operation", "source", ActivityKind.Internal);
            var otherContext = new SamplingContext(ActivityTraceId.CreateRandom(), "other", "source", ActivityKind.Internal);

            var operationResult = sampler.ShouldSample(operationContext);
            var otherResult = sampler.ShouldSample(otherContext);

            Assert.AreEqual(SamplingDecision.Drop, operationResult.Decision);
            Assert.AreEqual(SamplingDecision.RecordAndSample, otherResult.Decision);
        }
    }
}
