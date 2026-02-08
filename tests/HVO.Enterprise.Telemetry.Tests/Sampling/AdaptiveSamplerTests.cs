using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Sampling
{
    [TestClass]
    public class AdaptiveSamplerTests
    {
        [TestMethod]
        public void AdaptiveSampler_AdjustsRateDown_WhenOverTarget()
        {
            var now = DateTimeOffset.UtcNow;
            var currentTime = now;

            var sampler = new AdaptiveSampler(
                targetOperationsPerSecond: 1,
                minSamplingRate: 0.1,
                maxSamplingRate: 1.0,
                timeProvider: () => currentTime);

            for (int i = 0; i < 100; i++)
            {
                sampler.ShouldSample(new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal));
            }

            currentTime = currentTime.AddSeconds(2);
            sampler.ShouldSample(new SamplingContext(ActivityTraceId.CreateRandom(), "op", "source", ActivityKind.Internal));

            Assert.AreEqual(0.1, sampler.CurrentSamplingRate, 0.0001);
        }
    }
}
