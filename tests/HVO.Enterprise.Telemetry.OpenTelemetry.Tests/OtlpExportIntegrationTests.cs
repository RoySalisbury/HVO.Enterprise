using System;
using System.Linq;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests
{
    [TestClass]
    public class OtlpExportIntegrationTests
    {
        [TestMethod]
        public void FullRegistration_WithTelemetry_ResolvesDependencies()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "integration-test";
                    options.EnableTraceExport = true;
                    options.EnableMetricsExport = true;
                });
            });

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual("integration-test", opts.ServiceName);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void OtlpExport_CoexistsWithOtherExtensions()
        {
            // Verify that OTel export can be registered alongside other extensions
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "dual-export-test";
                });
            });

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void FullRegistration_WithAllFeatures_Succeeds()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder
                    .WithOpenTelemetry(options =>
                    {
                        options.ServiceName = "full-feature-test";
                        options.Endpoint = "http://collector:4317";
                        options.Transport = OtlpTransport.HttpProtobuf;
                        options.EnableTraceExport = true;
                        options.EnableMetricsExport = true;
                        options.EnableLogExport = true;
                        options.EnablePrometheusEndpoint = true;
                        options.TemporalityPreference = MetricsTemporality.Delta;
                    });
            });

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual("full-feature-test", opts.ServiceName);
            Assert.AreEqual("http://collector:4317", opts.Endpoint);
            Assert.AreEqual(OtlpTransport.HttpProtobuf, opts.Transport);
            Assert.IsTrue(opts.EnableLogExport);
            Assert.IsTrue(opts.EnablePrometheusEndpoint);
            Assert.AreEqual(MetricsTemporality.Delta, opts.TemporalityPreference);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void FluentApi_ChainingWorks()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                var result = builder
                    .WithOpenTelemetry(o => o.ServiceName = "chain-test")
                    .WithPrometheusEndpoint()
                    .WithOtlpLogExport();

                Assert.AreSame(builder, result);
            });
        }
    }
}
