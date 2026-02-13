using System;
using HVO.Enterprise.Telemetry.Grpc;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class GrpcMethodParserTests
    {
        [TestMethod]
        public void Parse_StandardFormat_ExtractsCorrectly()
        {
            var (service, method) = GrpcMethodParser.Parse("/mypackage.OrderService/GetOrder");

            Assert.AreEqual("mypackage.OrderService", service);
            Assert.AreEqual("GetOrder", method);
        }

        [TestMethod]
        public void Parse_SimpleServiceName_ExtractsCorrectly()
        {
            var (service, method) = GrpcMethodParser.Parse("/Greeter/SayHello");

            Assert.AreEqual("Greeter", service);
            Assert.AreEqual("SayHello", method);
        }

        [TestMethod]
        public void Parse_DeepNamespace_ExtractsCorrectly()
        {
            var (service, method) = GrpcMethodParser.Parse("/com.example.api.v1.UserService/GetUser");

            Assert.AreEqual("com.example.api.v1.UserService", service);
            Assert.AreEqual("GetUser", method);
        }

        [TestMethod]
        public void Parse_NullMethod_ReturnsUnknown()
        {
            var (service, method) = GrpcMethodParser.Parse(null!);

            Assert.AreEqual("unknown", service);
            Assert.AreEqual("unknown", method);
        }

        [TestMethod]
        public void Parse_EmptyMethod_ReturnsUnknown()
        {
            var (service, method) = GrpcMethodParser.Parse("");

            Assert.AreEqual("unknown", service);
            Assert.AreEqual("unknown", method);
        }

        [TestMethod]
        public void Parse_NoLeadingSlash_ReturnsUnknown()
        {
            var (service, method) = GrpcMethodParser.Parse("Greeter/SayHello");

            Assert.AreEqual("unknown", service);
            Assert.AreEqual("unknown", method);
        }

        [TestMethod]
        public void Parse_OnlySlash_ReturnsUnknown()
        {
            var (service, method) = GrpcMethodParser.Parse("/");

            Assert.AreEqual("unknown", service);
            Assert.AreEqual("/", method);
        }

        [TestMethod]
        public void Parse_ServiceOnly_NoMethod_ReturnsUnknown()
        {
            var (service, method) = GrpcMethodParser.Parse("/ServiceName");

            Assert.AreEqual("unknown", service);
            Assert.AreEqual("/ServiceName", method);
        }

        [TestMethod]
        public void Parse_HealthCheckMethod_Parses()
        {
            var (service, method) = GrpcMethodParser.Parse("/grpc.health.v1.Health/Check");

            Assert.AreEqual("grpc.health.v1.Health", service);
            Assert.AreEqual("Check", method);
        }

        [TestMethod]
        public void Parse_ReflectionMethod_Parses()
        {
            var (service, method) = GrpcMethodParser.Parse("/grpc.reflection.v1alpha.ServerReflection/ServerReflectionInfo");

            Assert.AreEqual("grpc.reflection.v1alpha.ServerReflection", service);
            Assert.AreEqual("ServerReflectionInfo", method);
        }
    }
}
