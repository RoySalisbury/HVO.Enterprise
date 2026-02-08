using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class GrpcRequestContextStoreTests
    {
        [TestMethod]
        public void GrpcRequestContextStore_RoundTripsValue()
        {
            var request = new GrpcRequestInfo { Service = "Test" };

            GrpcRequestContextStore.Current = request;

            Assert.AreEqual(request, GrpcRequestContextStore.Current);
        }
    }
}
