using HVO.Enterprise.Telemetry.Grpc;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class GrpcActivityTagsTests
    {
        [TestMethod]
        public void RpcSystem_MatchesOTelConvention()
        {
            Assert.AreEqual("rpc.system", GrpcActivityTags.RpcSystem);
        }

        [TestMethod]
        public void RpcService_MatchesOTelConvention()
        {
            Assert.AreEqual("rpc.service", GrpcActivityTags.RpcService);
        }

        [TestMethod]
        public void RpcMethod_MatchesOTelConvention()
        {
            Assert.AreEqual("rpc.method", GrpcActivityTags.RpcMethod);
        }

        [TestMethod]
        public void RpcGrpcStatusCode_MatchesOTelConvention()
        {
            Assert.AreEqual("rpc.grpc.status_code", GrpcActivityTags.RpcGrpcStatusCode);
        }

        [TestMethod]
        public void ServerAddress_MatchesOTelConvention()
        {
            Assert.AreEqual("server.address", GrpcActivityTags.ServerAddress);
        }

        [TestMethod]
        public void ServerPort_MatchesOTelConvention()
        {
            Assert.AreEqual("server.port", GrpcActivityTags.ServerPort);
        }

        [TestMethod]
        public void GrpcSystemValue_IsGrpc()
        {
            Assert.AreEqual("grpc", GrpcActivityTags.GrpcSystemValue);
        }
    }
}
