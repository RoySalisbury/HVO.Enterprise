using System;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class WcfExtensionOptionsTests
    {
        [TestMethod]
        public void Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new WcfExtensionOptions();

            // Assert
            Assert.IsTrue(options.PropagateTraceContextInReply);
            Assert.IsNull(options.OperationFilter);
            Assert.IsTrue(options.CaptureFaultDetails);
        }

        [TestMethod]
        public void Validate_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var validator = new WcfExtensionOptionsValidator();
            var options = new WcfExtensionOptions();

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void Validate_NullOptions_ReturnsFail()
        {
            // Arrange
            var validator = new WcfExtensionOptionsValidator();

            // Act
            var result = validator.Validate(null, null!);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void OperationFilter_WhenSet_FiltersOperations()
        {
            // Arrange
            var options = new WcfExtensionOptions
            {
                OperationFilter = op => !op.Contains("Health")
            };

            // Assert
            Assert.IsTrue(options.OperationFilter("CreateOrder"));
            Assert.IsFalse(options.OperationFilter("HealthCheck"));
        }

        [TestMethod]
        public void AllProperties_AreSettable()
        {
            // Arrange & Act
            var options = new WcfExtensionOptions
            {
                PropagateTraceContextInReply = false,
                OperationFilter = _ => true,
                CaptureFaultDetails = false
            };

            // Assert
            Assert.IsFalse(options.PropagateTraceContextInReply);
            Assert.IsNotNull(options.OperationFilter);
            Assert.IsFalse(options.CaptureFaultDetails);
        }
    }
}
