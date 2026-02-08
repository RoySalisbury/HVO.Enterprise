using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class GrpcRequestContextProviderTests
    {
        [TestMethod]
        public void GrpcRequestContextProvider_AddsTags()
        {
            var accessor = new FakeGrpcRequestAccessor(new GrpcRequestInfo
            {
                Service = "TestService",
                Method = "Get",
                Metadata = new Dictionary<string, string> { { "x-trace", "value" } }
            });
            var provider = new GrpcRequestContextProvider(accessor);
            var activity = new Activity("test");
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose };

            provider.EnrichActivity(activity, options);

            Assert.AreEqual("TestService", activity.GetTagItem("rpc.service"));
            Assert.AreEqual("Get", activity.GetTagItem("rpc.method"));
            Assert.AreEqual("value", activity.GetTagItem("rpc.metadata.x-trace"));
        }

        private sealed class FakeGrpcRequestAccessor : IGrpcRequestAccessor
        {
            private readonly GrpcRequestInfo? _request;

            public FakeGrpcRequestAccessor(GrpcRequestInfo? request)
            {
                _request = request;
            }

            public GrpcRequestInfo? GetCurrentRequest()
            {
                return _request;
            }
        }
    }
}
