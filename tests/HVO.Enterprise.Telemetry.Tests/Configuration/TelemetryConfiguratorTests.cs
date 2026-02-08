using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class TelemetryConfiguratorTests
    {
        [TestMethod]
        public void TelemetryConfigurator_Global_AppliesConfiguration()
        {
            var provider = new ConfigurationProvider();
            var configurator = new TelemetryConfigurator(provider);

            configurator.Global()
                .SamplingRate(0.15)
                .Enabled(true)
                .AddTag("env", "test")
                .Apply();

            var effective = provider.GetEffectiveConfiguration();

            Assert.AreEqual(0.15, effective.SamplingRate);
            Assert.AreEqual(true, effective.Enabled);
            Assert.AreEqual("test", effective.Tags["env"]);
        }

        [TestMethod]
        public void TelemetryConfigurator_Namespace_AppliesConfiguration()
        {
            var provider = new ConfigurationProvider();
            var configurator = new TelemetryConfigurator(provider);

            configurator.Namespace("HVO.Enterprise.Telemetry.Tests.*")
                .SamplingRate(0.55)
                .Enabled(false)
                .Apply();

            var effective = provider.GetEffectiveConfiguration(typeof(ConfiguredService));

            Assert.AreEqual(0.55, effective.SamplingRate);
            Assert.AreEqual(false, effective.Enabled);
        }

        [TestMethod]
        public void TelemetryConfigurator_SetsTypeConfiguration()
        {
            var provider = new ConfigurationProvider();
            var configurator = new TelemetryConfigurator(provider);

            configurator.ForType<ConfiguredService>()
                .SamplingRate(0.25)
                .Enabled(false)
                .AddTag("service", "configured")
                .Apply();

            var effective = provider.GetEffectiveConfiguration(typeof(ConfiguredService));

            Assert.IsTrue(effective.SamplingRate.HasValue);
            Assert.AreEqual(0.25, effective.SamplingRate.GetValueOrDefault(), 0.0001);
            Assert.AreEqual(false, effective.Enabled);
            Assert.AreEqual("configured", effective.Tags["service"]);
        }

        private sealed class ConfiguredService
        {
        }
    }
}
