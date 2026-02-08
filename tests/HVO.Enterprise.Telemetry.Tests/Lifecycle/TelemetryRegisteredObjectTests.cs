using System;
using HVO.Enterprise.Telemetry.Lifecycle;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    [TestClass]
    public class TelemetryRegisteredObjectTests
    {
        [TestMethod]
        public void Stop_Immediate_DoesNotThrow()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var registered = new TelemetryRegisteredObject(manager);

            try
            {
                registered.Stop(true);
            }
            catch (Exception ex)
            {
                Assert.Fail("Stop(true) threw exception: " + ex.Message);
            }
        }

        [TestMethod]
        public void Stop_Graceful_DoesNotThrow()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var registered = new TelemetryRegisteredObject(manager);

            try
            {
                registered.Stop(false);
            }
            catch (Exception ex)
            {
                Assert.Fail("Stop(false) threw exception: " + ex.Message);
            }
        }
    }
}
