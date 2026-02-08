using System.Diagnostics;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Sampling
{
    [TestClass]
    public class ProbabilisticSamplerTests
    {
        [TestMethod]
        public void ProbabilisticSampler_DeterministicForSameTraceId()
        {
            var sampler = new ProbabilisticSampler(0.5);
            var traceId = ActivityTraceId.CreateFromString("00000000000000000000000000000001");
            var context = new SamplingContext(traceId, "op", "source", ActivityKind.Internal);

            var result1 = sampler.ShouldSample(context);
            var result2 = sampler.ShouldSample(context);

            Assert.AreEqual(result1.Decision, result2.Decision);
        }

        [TestMethod]
        public void ProbabilisticSampler_ZeroRate_Drops()
        {
            var sampler = new ProbabilisticSampler(0.0);
            var context = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.Drop, result.Decision);
        }

        [TestMethod]
        public void ProbabilisticSampler_OneRate_Samples()
        {
            var sampler = new ProbabilisticSampler(1.0);
            var context = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.RecordAndSample, result.Decision);
        }
    }
}
