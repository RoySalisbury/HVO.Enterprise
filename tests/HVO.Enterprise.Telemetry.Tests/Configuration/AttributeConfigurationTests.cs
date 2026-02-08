using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class AttributeConfigurationTests
    {
        [TestMethod]
        public void TelemetryConfigurationAttribute_ToConfiguration_UsesInheritedDefaults()
        {
            var attribute = new TelemetryConfigurationAttribute();
            var configuration = attribute.ToConfiguration();

            Assert.IsNull(configuration.SamplingRate);
            Assert.IsNull(configuration.Enabled);
            Assert.IsNull(configuration.ParameterCapture);
            Assert.IsNull(configuration.RecordExceptions);
            Assert.IsNull(configuration.TimeoutThresholdMs);
        }

        [TestMethod]
        public void ConfigurationProvider_ApplyAttributeConfiguration_AppliesTypeAndMethod()
        {
            var provider = new ConfigurationProvider();
            provider.ApplyAttributeConfiguration(typeof(AttributedService));

            var method = typeof(AttributedService).GetMethod(nameof(AttributedService.Run));
            var effective = provider.GetEffectiveConfiguration(typeof(AttributedService), method);

            Assert.IsTrue(effective.SamplingRate.HasValue);
            Assert.AreEqual(0.8, effective.SamplingRate.GetValueOrDefault(), 0.0001);
            Assert.AreEqual(false, effective.Enabled);
            Assert.AreEqual(ParameterCaptureMode.Full, effective.ParameterCapture);
            Assert.AreEqual(1200, effective.TimeoutThresholdMs);
        }

        [TelemetryConfiguration(Enabled = ConfigurationToggle.Disabled, SamplingRate = 0.2)]
        private sealed class AttributedService
        {
            [TelemetryConfiguration(SamplingRate = 0.8, ParameterCapture = ParameterCaptureMode.Full, TimeoutThresholdMs = 1200)]
            public void Run()
            {
            }
        }
    }
}
