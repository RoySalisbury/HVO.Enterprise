using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Data.Configuration;
using HVO.Enterprise.Telemetry.Data.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Tests
{
    [TestClass]
    public class DataExtensionOptionsTests
    {
        [TestMethod]
        public void Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new DataExtensionOptions();

            // Assert
            Assert.IsTrue(options.RecordStatements);
            Assert.AreEqual(2000, options.MaxStatementLength);
            Assert.IsFalse(options.RecordParameters);
            Assert.AreEqual(10, options.MaxParameters);
            Assert.IsNull(options.OperationFilter);
        }

        [TestMethod]
        public void Validate_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var validator = new DataExtensionOptionsValidator();
            var options = new DataExtensionOptions();

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void Validate_NullOptions_ReturnsFail()
        {
            // Arrange
            var validator = new DataExtensionOptionsValidator();

            // Act
            var result = validator.Validate(null, null!);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_MaxStatementLengthTooLow_ReturnsFail()
        {
            // Arrange
            var validator = new DataExtensionOptionsValidator();
            var options = new DataExtensionOptions { MaxStatementLength = 50 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_MaxStatementLengthTooHigh_ReturnsFail()
        {
            // Arrange
            var validator = new DataExtensionOptionsValidator();
            var options = new DataExtensionOptions { MaxStatementLength = 100000 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_MaxParametersNegative_ReturnsFail()
        {
            // Arrange
            var validator = new DataExtensionOptionsValidator();
            var options = new DataExtensionOptions { MaxParameters = -1 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_MaxParametersTooHigh_ReturnsFail()
        {
            // Arrange
            var validator = new DataExtensionOptionsValidator();
            var options = new DataExtensionOptions { MaxParameters = 200 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void AllProperties_AreSettable()
        {
            // Arrange
            Func<string, bool> filter = _ => false;

            var options = new DataExtensionOptions
            {
                RecordStatements = false,
                MaxStatementLength = 5000,
                RecordParameters = true,
                MaxParameters = 50,
                OperationFilter = filter
            };

            // Assert
            Assert.IsFalse(options.RecordStatements);
            Assert.AreEqual(5000, options.MaxStatementLength);
            Assert.IsTrue(options.RecordParameters);
            Assert.AreEqual(50, options.MaxParameters);
            Assert.AreSame(filter, options.OperationFilter);
        }
    }
}
