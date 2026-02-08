using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class EnvironmentContextProviderTests
    {
        [TestMethod]
        public void EnvironmentContextProvider_AddsMinimalTags()
        {
            var provider = new EnvironmentContextProvider();
            var activity = new Activity("test");
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Minimal };

            provider.EnrichActivity(activity, options);

            Assert.IsNotNull(activity.GetTagItem("service.name"));
            Assert.IsNotNull(activity.GetTagItem("host.name"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_AddsVerboseTags()
        {
            var provider = new EnvironmentContextProvider();
            var activity = new Activity("test");
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose };

            provider.EnrichActivity(activity, options);

            Assert.IsNotNull(activity.GetTagItem("process.pid"));
            Assert.IsNotNull(activity.GetTagItem("host.cpu_count"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_AddsCustomTags()
        {
            var provider = new EnvironmentContextProvider();
            var activity = new Activity("test");
            var options = new EnrichmentOptions
            {
                MaxLevel = EnrichmentLevel.Standard,
                CustomEnvironmentTags = new Dictionary<string, string> { { "region", "us-east" } }
            };

            provider.EnrichActivity(activity, options);

            Assert.AreEqual("us-east", activity.GetTagItem("env.region"));
        }
    }
}
