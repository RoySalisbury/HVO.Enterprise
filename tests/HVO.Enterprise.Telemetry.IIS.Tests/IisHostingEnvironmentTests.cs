using System;
using HVO.Enterprise.Telemetry.IIS;

namespace HVO.Enterprise.Telemetry.IIS.Tests
{
    /// <summary>
    /// Tests for <see cref="IisHostingEnvironment"/> IIS detection logic.
    /// </summary>
    [TestClass]
    public sealed class IisHostingEnvironmentTests
    {
        [TestMethod]
        public void IsIisHosted_ReturnsFalse_InNonIisEnvironment()
        {
            // In our dev container / test environment, we are not running under IIS
            Assert.IsFalse(IisHostingEnvironment.IsIisHosted);
        }

        [TestMethod]
        public void IsIisHosted_IsCached_ReturnsConsistentResults()
        {
            // Multiple accesses should return the same value (Lazy<bool>)
            var first = IisHostingEnvironment.IsIisHosted;
            var second = IisHostingEnvironment.IsIisHosted;
            var third = IisHostingEnvironment.IsIisHosted;

            Assert.AreEqual(first, second);
            Assert.AreEqual(second, third);
        }

        [TestMethod]
        public void WorkerProcessId_ReturnsNull_WhenNotHostedInIis()
        {
            if (!IisHostingEnvironment.IsIisHosted)
            {
                Assert.IsNull(IisHostingEnvironment.WorkerProcessId);
            }
        }

        [TestMethod]
        public void WorkerProcessId_ReturnsPositiveId_WhenHostedInIis()
        {
            if (IisHostingEnvironment.IsIisHosted)
            {
                Assert.IsNotNull(IisHostingEnvironment.WorkerProcessId);
                Assert.IsTrue(IisHostingEnvironment.WorkerProcessId > 0);
            }
        }

        [TestMethod]
        public void EnvironmentDescription_ReturnsNonIisMessage_WhenNotHosted()
        {
            if (!IisHostingEnvironment.IsIisHosted)
            {
                Assert.AreEqual("Not running under IIS", IisHostingEnvironment.EnvironmentDescription);
            }
        }

        [TestMethod]
        public void EnvironmentDescription_ContainsProcessInfo_WhenHosted()
        {
            if (IisHostingEnvironment.IsIisHosted)
            {
                var description = IisHostingEnvironment.EnvironmentDescription;
                Assert.IsTrue(description.StartsWith("IIS (", StringComparison.Ordinal));
                Assert.IsTrue(description.Contains("PID:"));
            }
        }
    }
}
