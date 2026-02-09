using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;

namespace HVO.Enterprise.Telemetry.Benchmarks.Context
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-011")]
    public class ContextProviderBenchmarks
    {
        private const int OperationsPerInvoke = 1000;
        private readonly Consumer _consumer = new Consumer();
        private readonly EnrichmentOptions _options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose, RedactPii = false };
        private EnvironmentContextProvider _environmentProvider = null!;
        private UserContextProvider _userProvider = null!;
        private HttpRequestContextProvider _requestProvider = null!;
        private Activity _activity = null!;
        private Dictionary<string, object> _properties = null!;

        [GlobalSetup]
        public void Setup()
        {
            _environmentProvider = new EnvironmentContextProvider();
            _userProvider = new UserContextProvider(new FakeUserContextAccessor());
            _requestProvider = new HttpRequestContextProvider(new FakeHttpRequestAccessor());
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _activity = new Activity("bench");
            _activity.Start();
            _properties = new Dictionary<string, object>();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _activity.Stop();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Provider")]
        public void EnvironmentProvider_EnrichActivity()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _environmentProvider.EnrichActivity(_activity, _options);
            }
            _consumer.Consume(_activity.GetTagItem("service.name")!);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Provider")]
        public void UserProvider_EnrichActivity()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _userProvider.EnrichActivity(_activity, _options);
            }
            _consumer.Consume(_activity.GetTagItem("user.id")!);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Provider")]
        public void RequestProvider_EnrichActivity()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _requestProvider.EnrichActivity(_activity, _options);
            }
            _consumer.Consume(_activity.GetTagItem("http.method")!);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Provider")]
        public void EnvironmentProvider_EnrichProperties()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _environmentProvider.EnrichProperties(_properties, _options);
            }
            _consumer.Consume(_properties.Count);
        }

        private sealed class FakeUserContextAccessor : IUserContextAccessor
        {
            private readonly UserContext _context = new UserContext
            {
                UserId = "bench-user",
                Username = "bench",
                Roles = new List<string> { "role" }
            };

            public UserContext? GetUserContext()
            {
                return _context;
            }
        }

        private sealed class FakeHttpRequestAccessor : IHttpRequestAccessor
        {
            private readonly HttpRequestInfo _request = new HttpRequestInfo
            {
                Method = "GET",
                Url = "https://example.test/bench",
                Path = "/bench",
                QueryString = "a=1",
                Headers = new Dictionary<string, string> { ["x-test"] = "value" },
                UserAgent = "bench",
                ClientIp = "127.0.0.1"
            };

            public HttpRequestInfo? GetCurrentRequest()
            {
                return _request;
            }
        }
    }
}
