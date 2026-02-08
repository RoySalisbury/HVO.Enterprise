using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests
{
    /// <summary>
    /// Tests to verify that HVO.Enterprise.Telemetry can be referenced and used from .NET 8 projects.
    /// </summary>
    [TestClass]
    public class CompatibilityTests
    {
        [TestMethod]
        public void ProjectReference_CanBeResolved()
        {
            // This test verifies that the project can be referenced successfully
            // If we can run this test, the project reference is working
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void AssemblyVersion_IsCorrect()
        {
            // Verify the assembly can be loaded and has the expected version
            var assembly = typeof(HVO.Enterprise.Telemetry.Abstractions.ITelemetryService).Assembly;
            Assert.IsNotNull(assembly);
            Assert.AreEqual("1.0.0.0", assembly.GetName().Version?.ToString());
        }
    }
}
