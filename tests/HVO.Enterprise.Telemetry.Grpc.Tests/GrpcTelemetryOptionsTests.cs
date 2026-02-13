using System;
using HVO.Enterprise.Telemetry.Grpc;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class GrpcTelemetryOptionsTests
    {
        [TestMethod]
        public void Defaults_AreCorrect()
        {
            var options = new GrpcTelemetryOptions();

            Assert.IsTrue(options.EnableServerInterceptor);
            Assert.IsTrue(options.EnableClientInterceptor);
            Assert.AreEqual("x-correlation-id", options.CorrelationHeaderName);
            Assert.IsTrue(options.SuppressHealthChecks);
            Assert.IsTrue(options.SuppressReflection);
        }

        [TestMethod]
        public void EnableServerInterceptor_CanBeDisabled()
        {
            var options = new GrpcTelemetryOptions { EnableServerInterceptor = false };
            Assert.IsFalse(options.EnableServerInterceptor);
        }

        [TestMethod]
        public void EnableClientInterceptor_CanBeDisabled()
        {
            var options = new GrpcTelemetryOptions { EnableClientInterceptor = false };
            Assert.IsFalse(options.EnableClientInterceptor);
        }

        [TestMethod]
        public void CorrelationHeaderName_CanBeCustomized()
        {
            var options = new GrpcTelemetryOptions { CorrelationHeaderName = "x-request-id" };
            Assert.AreEqual("x-request-id", options.CorrelationHeaderName);
        }

        [TestMethod]
        public void SuppressHealthChecks_CanBeDisabled()
        {
            var options = new GrpcTelemetryOptions { SuppressHealthChecks = false };
            Assert.IsFalse(options.SuppressHealthChecks);
        }

        [TestMethod]
        public void SuppressReflection_CanBeDisabled()
        {
            var options = new GrpcTelemetryOptions { SuppressReflection = false };
            Assert.IsFalse(options.SuppressReflection);
        }
    }
}
