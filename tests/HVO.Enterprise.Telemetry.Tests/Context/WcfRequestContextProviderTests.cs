using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class WcfRequestContextProviderTests
    {
        [TestMethod]
        public void WcfRequestContextProvider_AddsTags()
        {
            var accessor = new FakeWcfRequestAccessor(new WcfRequestInfo
            {
                Action = "urn:action",
                Endpoint = "net.tcp://localhost/service",
                Binding = "netTcp"
            });
            var provider = new WcfRequestContextProvider(accessor);
            var activity = new Activity("test");
            var options = new EnrichmentOptions();

            provider.EnrichActivity(activity, options);

            Assert.AreEqual("urn:action", activity.GetTagItem("wcf.action"));
            Assert.AreEqual("net.tcp://localhost/service", activity.GetTagItem("wcf.endpoint"));
            Assert.AreEqual("netTcp", activity.GetTagItem("wcf.binding"));
        }

        private sealed class FakeWcfRequestAccessor : IWcfRequestAccessor
        {
            private readonly WcfRequestInfo? _request;

            public FakeWcfRequestAccessor(WcfRequestInfo? request)
            {
                _request = request;
            }

            public WcfRequestInfo? GetCurrentRequest()
            {
                return _request;
            }
        }
    }
}
