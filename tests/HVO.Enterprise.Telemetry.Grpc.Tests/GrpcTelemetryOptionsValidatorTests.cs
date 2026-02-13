using System;
using HVO.Enterprise.Telemetry.Grpc;
using HVO.Enterprise.Telemetry.Grpc.Configuration;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class GrpcTelemetryOptionsValidatorTests
    {
        private readonly GrpcTelemetryOptionsValidator _validator = new GrpcTelemetryOptionsValidator();

        [TestMethod]
        public void Validate_DefaultOptions_Succeeds()
        {
            var options = new GrpcTelemetryOptions();

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void Validate_NullOptions_Fails()
        {
            var result = _validator.Validate(null, null!);

            Assert.IsTrue(result.Failed);
            Assert.IsTrue(result.FailureMessage!.Contains("cannot be null"));
        }

        [TestMethod]
        public void Validate_EmptyCorrelationHeaderName_Fails()
        {
            var options = new GrpcTelemetryOptions { CorrelationHeaderName = "" };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
            Assert.IsTrue(result.FailureMessage!.Contains("CorrelationHeaderName"));
        }

        [TestMethod]
        public void Validate_WhitespaceCorrelationHeaderName_Fails()
        {
            var options = new GrpcTelemetryOptions { CorrelationHeaderName = "   " };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_EmptyActivitySourceName_Fails()
        {
            var options = new GrpcTelemetryOptions { ActivitySourceName = "" };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
            Assert.IsTrue(result.FailureMessage!.Contains("ActivitySourceName"));
        }

        [TestMethod]
        public void Validate_WhitespaceActivitySourceName_Fails()
        {
            var options = new GrpcTelemetryOptions { ActivitySourceName = "   " };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void Validate_CustomValidOptions_Succeeds()
        {
            var options = new GrpcTelemetryOptions
            {
                EnableServerInterceptor = false,
                EnableClientInterceptor = false,
                CorrelationHeaderName = "x-request-id",
                RecordMessageSize = true,
                SuppressHealthChecks = false,
                SuppressReflection = false,
                ActivitySourceName = "Custom.Source"
            };

            var result = _validator.Validate(null, options);

            Assert.IsTrue(result.Succeeded);
        }
    }
}
