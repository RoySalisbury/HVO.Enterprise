using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfigurationDiagnosticsTests
    {
        [TestMethod]
        public void ConfigurationDiagnostics_ExplainConfiguration_ReturnsLayers()
        {
            var provider = ConfigurationProvider.Instance;
            provider.Clear();

            provider.SetGlobalConfiguration(new OperationConfiguration { SamplingRate = 0.1 });

            var report = ConfigurationDiagnostics.ExplainConfiguration(typeof(DiagnosticService));

            Assert.IsNotNull(report);
            Assert.IsTrue(report.Layers.Count >= 2);
            Assert.IsTrue(report.EffectiveConfiguration.SamplingRate.HasValue);
            Assert.AreEqual(0.1, report.EffectiveConfiguration.SamplingRate.GetValueOrDefault(), 0.0001);
        }

        private sealed class DiagnosticService
        {
        }
    }
}
