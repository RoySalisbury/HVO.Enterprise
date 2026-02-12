using System;
using System.Linq;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddOpenTelemetryExport_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddOpenTelemetryExport(null!));
        }

        [TestMethod]
        public void AddOpenTelemetryExport_RegistersMarker()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void AddOpenTelemetryExport_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.ServiceName = "test-service";
                options.Endpoint = "http://collector:4317";
                options.EnableLogExport = true;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual("test-service", options.ServiceName);
            Assert.AreEqual("http://collector:4317", options.Endpoint);
            Assert.IsTrue(options.EnableLogExport);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_IsIdempotent()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport();
            services.AddOpenTelemetryExport();

            var markerCount = services.Count(s => s.ServiceType == typeof(OtlpExportMarker));
            Assert.AreEqual(1, markerCount);
        }

        [TestMethod]
        public void AddOpenTelemetryExport_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();
            var result = services.AddOpenTelemetryExport();

            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithNullConfigure_Succeeds()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(configure: null);

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void AddOpenTelemetryExportFromEnvironment_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddOpenTelemetryExportFromEnvironment(null!));
        }

        [TestMethod]
        public void AddOpenTelemetryExportFromEnvironment_RegistersMarker()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExportFromEnvironment();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void AddOpenTelemetryExport_RegistersActivitySourceRegistrar()
        {
            var services = new ServiceCollection();
            services.AddOptions<Configuration.TelemetryOptions>();
            services.AddOpenTelemetryExport();

            var provider = services.BuildServiceProvider();
            var registrar = provider.GetService<HvoActivitySourceRegistrar>();

            Assert.IsNotNull(registrar);
            (provider as IDisposable)?.Dispose();
        }
    }
}
