using HVO.Enterprise.Telemetry.AppInsights;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class AppInsightsOptionsTests
    {
        [TestMethod]
        public void Defaults_EnableBridge_True()
        {
            var options = new AppInsightsOptions();
            Assert.IsTrue(options.EnableBridge);
        }

        [TestMethod]
        public void Defaults_EnableActivityInitializer_True()
        {
            var options = new AppInsightsOptions();
            Assert.IsTrue(options.EnableActivityInitializer);
        }

        [TestMethod]
        public void Defaults_EnableCorrelationInitializer_True()
        {
            var options = new AppInsightsOptions();
            Assert.IsTrue(options.EnableCorrelationInitializer);
        }

        [TestMethod]
        public void Defaults_CorrelationFallbackToActivity_True()
        {
            var options = new AppInsightsOptions();
            Assert.IsTrue(options.CorrelationFallbackToActivity);
        }

        [TestMethod]
        public void Defaults_CorrelationPropertyName_IsCorrelationId()
        {
            var options = new AppInsightsOptions();
            Assert.AreEqual("CorrelationId", options.CorrelationPropertyName);
        }

        [TestMethod]
        public void Defaults_ForceOtlpMode_IsNull()
        {
            var options = new AppInsightsOptions();
            Assert.IsNull(options.ForceOtlpMode);
        }

        [TestMethod]
        public void Defaults_ConnectionString_IsNull()
        {
            var options = new AppInsightsOptions();
            Assert.IsNull(options.ConnectionString);
        }

        [TestMethod]
        public void Defaults_InstrumentationKey_IsNull()
        {
            var options = new AppInsightsOptions();
            Assert.IsNull(options.InstrumentationKey);
        }

        [TestMethod]
        public void GetEffectiveConnectionString_WithConnectionString_ReturnsConnectionString()
        {
            var options = new AppInsightsOptions
            {
                ConnectionString = "InstrumentationKey=key;IngestionEndpoint=https://endpoint"
            };

            Assert.AreEqual(
                "InstrumentationKey=key;IngestionEndpoint=https://endpoint",
                options.GetEffectiveConnectionString());
        }

        [TestMethod]
        public void GetEffectiveConnectionString_WithInstrumentationKey_ConvertsToConnectionString()
        {
            var options = new AppInsightsOptions
            {
                InstrumentationKey = "test-key"
            };

            Assert.AreEqual("InstrumentationKey=test-key", options.GetEffectiveConnectionString());
        }

        [TestMethod]
        public void GetEffectiveConnectionString_BothSet_PrefersConnectionString()
        {
            var options = new AppInsightsOptions
            {
                ConnectionString = "InstrumentationKey=preferred;IngestionEndpoint=https://endpoint",
                InstrumentationKey = "fallback-key"
            };

            Assert.AreEqual(
                "InstrumentationKey=preferred;IngestionEndpoint=https://endpoint",
                options.GetEffectiveConnectionString());
        }

        [TestMethod]
        public void GetEffectiveConnectionString_NeitherSet_ReturnsNull()
        {
            var options = new AppInsightsOptions();
            Assert.IsNull(options.GetEffectiveConnectionString());
        }
    }
}
