using System;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Tests
{
    [TestClass]
    public class EfCoreTelemetryOptionsTests
    {
        [TestMethod]
        public void Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new EfCoreTelemetryOptions();

            // Assert
            Assert.IsTrue(options.RecordStatements);
            Assert.AreEqual(2000, options.MaxStatementLength);
            Assert.IsFalse(options.RecordParameters);
            Assert.AreEqual(10, options.MaxParameters);
            Assert.IsNull(options.OperationFilter);
            Assert.IsFalse(options.RecordConnectionInfo);
        }

        [TestMethod]
        public void RecordConnectionInfo_IsSettable()
        {
            // Arrange & Act
            var options = new EfCoreTelemetryOptions { RecordConnectionInfo = true };

            // Assert
            Assert.IsTrue(options.RecordConnectionInfo);
        }

        [TestMethod]
        public void Validate_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var validator = new EfCoreTelemetryOptionsValidator();
            var options = new EfCoreTelemetryOptions();

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void Validate_NullOptions_ReturnsFail()
        {
            // Arrange
            var validator = new EfCoreTelemetryOptionsValidator();

            // Act
            var result = validator.Validate(null, null!);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void EfCoreActivitySource_HasExpectedName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.EfCore", EfCoreActivitySource.Name);
        }

        [TestMethod]
        public void EfCoreActivitySource_SourceNotNull()
        {
            Assert.IsNotNull(EfCoreActivitySource.Source);
        }
    }
}
