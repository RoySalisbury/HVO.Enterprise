using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class WcfRequestContextStoreTests
    {
        [TestMethod]
        public void WcfRequestContextStore_RoundTripsValue()
        {
            var request = new WcfRequestInfo { Action = "urn:action" };

            WcfRequestContextStore.Current = request;

            Assert.AreEqual(request, WcfRequestContextStore.Current);
        }
    }
}
