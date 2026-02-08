using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class HttpRequestContextProviderTests
    {
        [TestMethod]
        public void HttpRequestContextProvider_AddsBasicTags()
        {
            var accessor = new FakeHttpRequestAccessor(new HttpRequestInfo
            {
                Method = "GET",
                Url = "https://example.com/api",
                Path = "/api"
            });
            var provider = new HttpRequestContextProvider(accessor);
            var activity = new Activity("test");
            var options = new EnrichmentOptions();

            provider.EnrichActivity(activity, options);

            Assert.AreEqual("GET", activity.GetTagItem("http.method"));
            Assert.AreEqual("https://example.com/api", activity.GetTagItem("http.url"));
            Assert.AreEqual("/api", activity.GetTagItem("http.target"));
        }

        [TestMethod]
        public void HttpRequestContextProvider_RedactsQueryString()
        {
            var accessor = new FakeHttpRequestAccessor(new HttpRequestInfo
            {
                Method = "GET",
                Url = "https://example.com/api",
                Path = "/api",
                QueryString = "user=john&apikey=secret123&page=1"
            });
            var provider = new HttpRequestContextProvider(accessor);
            var activity = new Activity("test");
            var options = new EnrichmentOptions { RedactPii = true };

            provider.EnrichActivity(activity, options);

            var query = activity.GetTagItem("http.query") as string;
            Assert.IsNotNull(query);
            Assert.IsTrue(query!.Contains("apikey=***"));
            Assert.IsTrue(query.Contains("user=john"));
        }

        [TestMethod]
        public void HttpRequestContextProvider_ExcludesHeaders()
        {
            var accessor = new FakeHttpRequestAccessor(new HttpRequestInfo
            {
                Method = "GET",
                Url = "https://example.com/api",
                Path = "/api",
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer token" },
                    { "X-Trace", "value" }
                }
            });
            var provider = new HttpRequestContextProvider(accessor);
            var activity = new Activity("test");
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose };

            provider.EnrichActivity(activity, options);

            Assert.IsNull(activity.GetTagItem("http.header.authorization"));
            Assert.AreEqual("value", activity.GetTagItem("http.header.x-trace"));
        }

        private sealed class FakeHttpRequestAccessor : IHttpRequestAccessor
        {
            private readonly HttpRequestInfo? _request;

            public FakeHttpRequestAccessor(HttpRequestInfo? request)
            {
                _request = request;
            }

            public HttpRequestInfo? GetCurrentRequest()
            {
                return _request;
            }
        }
    }
}
