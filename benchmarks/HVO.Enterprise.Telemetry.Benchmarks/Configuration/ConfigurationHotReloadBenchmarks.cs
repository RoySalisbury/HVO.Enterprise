using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Benchmarks.Configuration
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 1, iterationCount: 3)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [BenchmarkCategory("US-008")]
    public class ConfigurationHotReloadBenchmarks
    {
        private readonly Consumer _consumer = new Consumer();
        private string _configPath = string.Empty;
        private FileConfigurationReloader _reloader = null!;
        private ManualResetEventSlim _signal = null!;
        private TelemetryOptions _options = null!;
        private ConfigurationHttpEndpoint _endpoint = null!;
        private HttpClient _client = null!;
        private string _endpointUrl = string.Empty;
        private string _payload = string.Empty;

        [GlobalSetup]
        public void Setup()
        {
            _configPath = Path.Combine(Path.GetTempPath(), "telemetry-bench-" + Guid.NewGuid() + ".json");
            _options = new TelemetryOptions
            {
                DefaultSamplingRate = 0.1,
                Queue = new QueueOptions { Capacity = 1000, BatchSize = 100 },
                Metrics = new MetricsOptions { CollectionIntervalSeconds = 10 }
            };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(_options));

            _signal = new ManualResetEventSlim(false);
            _reloader = new FileConfigurationReloader(_configPath, NullLogger<FileConfigurationReloader>.Instance);
            _reloader.ConfigurationChanged += (_, __) => _signal.Set();

            var port = GetFreePort();
            _endpointUrl = "http://localhost:" + port + "/";
            _endpoint = new ConfigurationHttpEndpoint(_endpointUrl, _options, null, NullLogger<ConfigurationHttpEndpoint>.Instance);
            _endpoint.Start();

            _client = new HttpClient();
            _payload = JsonSerializer.Serialize(new TelemetryOptions { DefaultSamplingRate = 0.25 });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _endpoint.Dispose();
            _client.Dispose();
            _reloader.Dispose();
            _signal.Dispose();

            if (File.Exists(_configPath))
            {
                File.Delete(_configPath);
            }
        }

        [Benchmark]
        [BenchmarkCategory("FileReload")]
        public void FileConfigurationReloader_ChangePropagation()
        {
            _signal.Reset();
            _options.DefaultSamplingRate = _options.DefaultSamplingRate == 0.1 ? 0.2 : 0.1;
            File.WriteAllText(_configPath, JsonSerializer.Serialize(_options));
            _signal.Wait();
            _consumer.Consume(_reloader.CurrentOptions.DefaultSamplingRate);
        }

        [Benchmark]
        [BenchmarkCategory("HttpEndpoint")]
        public async Task ConfigurationHttpEndpoint_Update()
        {
            using var content = new StringContent(_payload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_endpointUrl + "telemetry/config", content).ConfigureAwait(false);
            _consumer.Consume(response.StatusCode == HttpStatusCode.OK);
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
