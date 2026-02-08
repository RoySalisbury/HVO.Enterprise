using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Sampling
{
    [TestClass]
    public class ConditionalSamplerTests
    {
        [TestMethod]
        public void ConditionalSampler_AlwaysSamplesErrors()
        {
            var baseSampler = new ProbabilisticSampler(0.0);
            var sampler = new ConditionalSampler(baseSampler, alwaysSampleErrors: true);

            var tags = new ActivityTagsCollection { { "error", true } };
            var context = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal, tags);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.RecordAndSample, result.Decision);
        }

        [TestMethod]
        public void ConditionalSampler_SamplesSlowOperations()
        {
            var baseSampler = new ProbabilisticSampler(0.0);
            var sampler = new ConditionalSampler(baseSampler, slowOperationThreshold: TimeSpan.FromMilliseconds(100));

            var tags = new ActivityTagsCollection { { "duration.ms", 250L } };
            var context = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal, tags);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.RecordAndSample, result.Decision);
        }

        [TestMethod]
        public void ConditionalSampler_CustomPredicate_Wins()
        {
            var baseSampler = new ProbabilisticSampler(0.0);
            var sampler = new ConditionalSampler(baseSampler, customPredicate: _ => true);

            var context = new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.RecordAndSample, result.Decision);
        }
    }
}
