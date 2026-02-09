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
            Assert.IsFalse(options.RecordMessageBodies);
            Assert.AreEqual(4096, options.MaxMessageBodySize);
            Assert.IsNull(options.CustomSoapNamespace);
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
        public void Validate_NegativeMaxMessageBodySize_ReturnsFail()
        {
            // Arrange
            var validator = new WcfExtensionOptionsValidator();
            var options = new WcfExtensionOptions { MaxMessageBodySize = -1 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
            Assert.IsTrue(result.FailureMessage!.Contains("MaxMessageBodySize"));
        }

        [TestMethod]
        public void Validate_ExcessiveMaxMessageBodySize_ReturnsFail()
        {
            // Arrange
            var validator = new WcfExtensionOptionsValidator();
            var options = new WcfExtensionOptions { MaxMessageBodySize = 2_000_000 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
            Assert.IsTrue(result.FailureMessage!.Contains("MaxMessageBodySize"));
        }

        [TestMethod]
        public void Validate_ZeroMaxMessageBodySize_ReturnsSuccess()
        {
            // Arrange
            var validator = new WcfExtensionOptionsValidator();
            var options = new WcfExtensionOptions { MaxMessageBodySize = 0 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void Validate_MaxBoundaryBodySize_ReturnsSuccess()
        {
            // Arrange
            var validator = new WcfExtensionOptionsValidator();
            var options = new WcfExtensionOptions { MaxMessageBodySize = 1_048_576 };

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
        public void InternalValidate_ValidOptions_DoesNotThrow()
        {
            // Arrange
            var options = new WcfExtensionOptions();

            // Act & Assert - should not throw
            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void InternalValidate_NegativeMaxMessageBodySize_Throws()
        {
            // Arrange
            var options = new WcfExtensionOptions { MaxMessageBodySize = -1 };

            // Act
            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void InternalValidate_ExcessiveMaxMessageBodySize_Throws()
        {
            // Arrange
            var options = new WcfExtensionOptions { MaxMessageBodySize = 2_000_000 };

            // Act
            options.Validate();
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
                RecordMessageBodies = true,
                MaxMessageBodySize = 8192,
                CustomSoapNamespace = "http://custom.ns",
                CaptureFaultDetails = false
            };

            // Assert
            Assert.IsFalse(options.PropagateTraceContextInReply);
            Assert.IsNotNull(options.OperationFilter);
            Assert.IsTrue(options.RecordMessageBodies);
            Assert.AreEqual(8192, options.MaxMessageBodySize);
            Assert.AreEqual("http://custom.ns", options.CustomSoapNamespace);
            Assert.IsFalse(options.CaptureFaultDetails);
        }
    }
}
