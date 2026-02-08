using System;
using System.Globalization;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    [TestClass]
    public class MetricTagKeyBuilderTests
    {
        [TestMethod]
        public void BuildTaggedName_SingleTag_FormatsValue()
        {
            var tag = new MetricTag("status", 200);

            var result = MetricTagKeyBuilder.BuildTaggedName("metric", in tag);

            Assert.AreEqual("metric.status=200", result);
        }

        [TestMethod]
        public void BuildTaggedName_MultipleTags_PreservesOrder()
        {
            var tag1 = new MetricTag("region", "east");
            var tag2 = new MetricTag("status", 200);

            var result = MetricTagKeyBuilder.BuildTaggedName("metric", in tag1, in tag2);

            Assert.AreEqual("metric.region=east.status=200", result);
        }

        [TestMethod]
        public void BuildTaggedName_ThreeTags_UsesAllTags()
        {
            var tag1 = new MetricTag("region", "east");
            var tag2 = new MetricTag("status", 200);
            var tag3 = new MetricTag("route", "/api");

            var result = MetricTagKeyBuilder.BuildTaggedName("metric", in tag1, in tag2, in tag3);

            Assert.AreEqual("metric.region=east.status=200.route=/api", result);
        }

        [TestMethod]
        public void BuildTaggedName_Array_UsesInvariantFormatting()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                var tags = new[]
                {
                    new MetricTag("value", 1.25),
                    new MetricTag("empty", null)
                };

                var result = MetricTagKeyBuilder.BuildTaggedName("metric", tags);

                Assert.AreEqual("metric.value=1.25.empty=null", result);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [TestMethod]
        public void BuildTaggedName_Array_ReturnsNameWhenEmpty()
        {
            var result = MetricTagKeyBuilder.BuildTaggedName("metric", Array.Empty<MetricTag>());

            Assert.AreEqual("metric", result);
        }
    }
}
