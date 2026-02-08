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
            using (var activity = new Activity("test"))
            {
                var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose };

                provider.EnrichActivity(activity, options);

                Assert.AreEqual("TestService", activity.GetTagItem("rpc.service"));
                Assert.AreEqual("Get", activity.GetTagItem("rpc.method"));
                Assert.AreEqual("value", activity.GetTagItem("rpc.metadata.x-trace"));
            }
        }

        [TestMethod]
        public void GrpcRequestContextProvider_RespectsExcludedHeaders()
        {
            var accessor = new FakeGrpcRequestAccessor(new GrpcRequestInfo
            {
                Service = "TestService",
                Method = "Get",
                Metadata = new Dictionary<string, string>
                {
                    { "authorization", "secret" },
                    { "x-trace", "value" }
                }
            });
            var provider = new GrpcRequestContextProvider(accessor);
            using (var activity = new Activity("test"))
            {
                var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose };
                options.ExcludedHeaders.Add("authorization");

                provider.EnrichActivity(activity, options);

                Assert.IsNull(activity.GetTagItem("rpc.metadata.authorization"));
                Assert.AreEqual("value", activity.GetTagItem("rpc.metadata.x-trace"));
            }
        }

        [TestMethod]
        public void GrpcRequestContextProvider_EnrichesProperties()
        {
            var accessor = new FakeGrpcRequestAccessor(new GrpcRequestInfo
            {
                Service = "TestService",
                Method = "Get"
            });
            var provider = new GrpcRequestContextProvider(accessor);
            var properties = new Dictionary<string, object>();

            provider.EnrichProperties(properties, new EnrichmentOptions());

            Assert.AreEqual("TestService", properties["rpc.service"]);
            Assert.AreEqual("Get", properties["rpc.method"]);
        }

        [TestMethod]
        public void GrpcRequestContextProvider_IgnoresNullRequest()
        {
            var accessor = new FakeGrpcRequestAccessor(null);
            var provider = new GrpcRequestContextProvider(accessor);
            using (var activity = new Activity("test"))
            {
                provider.EnrichActivity(activity, new EnrichmentOptions());

                Assert.IsNull(activity.GetTagItem("rpc.service"));
            }
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
