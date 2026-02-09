using System;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class TelemetryOptionsValidatorTests
    {
        private readonly TelemetryOptionsValidator _validator = new TelemetryOptionsValidator();

        [TestMethod]
        public void Validate_ValidOptions_ReturnsSuccess()
        {
            var options = new TelemetryOptions
            {
                ServiceName = "TestService",
                DefaultSamplingRate = 0.5
            };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void Validate_NullOptions_ReturnsFail()
        {
            var result = _validator.Validate(null, null!);

            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_EmptyServiceName_ReturnsFail()
        {
            var options = new TelemetryOptions { ServiceName = "" };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
            Assert.IsTrue(result.FailureMessage!.Contains("ServiceName"));
        }

        [TestMethod]
        public void Validate_InvalidSamplingRate_ReturnsFail()
        {
            var options = new TelemetryOptions { DefaultSamplingRate = 1.5 };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_InvalidQueueCapacity_ReturnsFail()
        {
            var options = new TelemetryOptions();
            options.Queue.Capacity = 10; // Below minimum of 100

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_DefaultOptions_ReturnsSuccess()
        {
            var options = new TelemetryOptions();

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Succeeded);
        }
    }
}
