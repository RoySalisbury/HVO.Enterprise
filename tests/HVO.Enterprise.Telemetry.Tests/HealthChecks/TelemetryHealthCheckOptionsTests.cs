using System;
using HVO.Enterprise.Telemetry.HealthChecks;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class TelemetryHealthCheckOptionsTests
    {
        [TestMethod]
        public void Defaults_HaveSensibleValues()
        {
            var options = new TelemetryHealthCheckOptions();

            Assert.AreEqual(1.0, options.DegradedErrorRateThreshold);
            Assert.AreEqual(10.0, options.UnhealthyErrorRateThreshold);
            Assert.AreEqual(10000, options.MaxExpectedQueueDepth);
            Assert.AreEqual(75.0, options.DegradedQueueDepthPercent);
            Assert.AreEqual(95.0, options.UnhealthyQueueDepthPercent);
            Assert.AreEqual(0.1, options.DegradedDropRatePercent);
            Assert.AreEqual(1.0, options.UnhealthyDropRatePercent);
        }

        [TestMethod]
        public void StaticDefault_MatchesNewInstance()
        {
            var defaults = TelemetryHealthCheckOptions.Default;

            Assert.AreEqual(1.0, defaults.DegradedErrorRateThreshold);
            Assert.AreEqual(10.0, defaults.UnhealthyErrorRateThreshold);
            Assert.AreEqual(10000, defaults.MaxExpectedQueueDepth);
        }

        [TestMethod]
        public void Validate_ValidOptions_DoesNotThrow()
        {
            var options = new TelemetryHealthCheckOptions();
            options.Validate();
        }

        [TestMethod]
        public void Validate_NegativeDegradedErrorRate_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = -1.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_NegativeUnhealthyErrorRate_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                UnhealthyErrorRateThreshold = -1.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_UnhealthyLessThanDegraded_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = 10.0,
                UnhealthyErrorRateThreshold = 5.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_ZeroMaxQueueDepth_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                MaxExpectedQueueDepth = 0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_QueueDepthPercentOver100_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                DegradedQueueDepthPercent = 101.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_UnhealthyQueueLessThanDegraded_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                DegradedQueueDepthPercent = 90.0,
                UnhealthyQueueDepthPercent = 80.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_DropRatePercentOver100_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                DegradedDropRatePercent = 101.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_UnhealthyDropLessThanDegraded_Throws()
        {
            var options = new TelemetryHealthCheckOptions
            {
                DegradedDropRatePercent = 5.0,
                UnhealthyDropRatePercent = 1.0
            };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.Validate());
        }
    }
}
