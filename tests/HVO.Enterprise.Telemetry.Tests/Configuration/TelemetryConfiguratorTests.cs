using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class TelemetryConfiguratorTests
    {
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
