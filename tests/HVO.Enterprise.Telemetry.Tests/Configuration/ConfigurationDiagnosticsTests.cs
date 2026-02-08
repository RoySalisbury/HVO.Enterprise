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

        [TestMethod]
        public void ConfigurationDiagnostics_ReportString_ContainsLayerDetails()
        {
            var provider = ConfigurationProvider.Instance;
            provider.Clear();

            provider.SetGlobalConfiguration(new OperationConfiguration { SamplingRate = 0.2 });
            provider.SetNamespaceConfiguration("HVO.Enterprise.Telemetry.Tests.*", new OperationConfiguration { Enabled = false });

            var report = ConfigurationDiagnostics.ExplainConfiguration(typeof(DiagnosticService));
            var text = report.ToReportString();

            Assert.IsTrue(text.Contains("GlobalDefault"));
            Assert.IsTrue(text.Contains("Global (Code)"));
            Assert.IsTrue(text.Contains("Namespace (Code)"));
        }

        private sealed class DiagnosticService
        {
        }
    }
}
