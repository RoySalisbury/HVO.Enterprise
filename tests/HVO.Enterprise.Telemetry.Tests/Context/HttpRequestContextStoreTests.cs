using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class HttpRequestContextStoreTests
    {
        [TestMethod]
        public void HttpRequestContextStore_RoundTripsValue()
        {
            var request = new HttpRequestInfo { Method = "GET" };

            HttpRequestContextStore.Current = request;

            Assert.AreEqual(request, HttpRequestContextStore.Current);
        }
    }
}
