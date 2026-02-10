using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Datadog.Tests
{
    [TestClass]
    public class DatadogOptionsTests
    {
        [TestMethod]
        public void Defaults_HaveExpectedValues()
        {
            var options = new DatadogOptions();

            Assert.AreEqual("localhost", options.AgentHost);
            Assert.AreEqual(8125, options.AgentPort);
            Assert.AreEqual(DatadogExportMode.Auto, options.Mode);
            Assert.IsNull(options.ServiceName);
            Assert.IsNull(options.Environment);
            Assert.IsNull(options.Version);
            Assert.IsNull(options.UnixDomainSocketPath);
            Assert.IsFalse(options.UseUnixDomainSocket);
            Assert.IsTrue(options.EnableMetricsExporter);
            Assert.IsTrue(options.EnableTraceExporter);
            Assert.IsNull(options.MetricPrefix);
            Assert.IsNotNull(options.GlobalTags);
            Assert.AreEqual(0, options.GlobalTags.Count);
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithDDService_SetsServiceName()
        {
            System.Environment.SetEnvironmentVariable("DD_SERVICE", "test-svc");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("test-svc", options.ServiceName);
                Assert.IsTrue(options.GlobalTags.ContainsKey("service"));
                Assert.AreEqual("test-svc", options.GlobalTags["service"]);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_SERVICE", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithDDEnv_SetsEnvironment()
        {
            System.Environment.SetEnvironmentVariable("DD_ENV", "staging");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("staging", options.Environment);
                Assert.AreEqual("staging", options.GlobalTags["env"]);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_ENV", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithDDVersion_SetsVersion()
        {
            System.Environment.SetEnvironmentVariable("DD_VERSION", "2.0.0");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("2.0.0", options.Version);
                Assert.AreEqual("2.0.0", options.GlobalTags["version"]);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_VERSION", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithDDAgentHost_SetsAgentHost()
        {
            System.Environment.SetEnvironmentVariable("DD_AGENT_HOST", "datadog-agent");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("datadog-agent", options.AgentHost);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_AGENT_HOST", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithDDDogstatsdPort_SetsPort()
        {
            System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", "9125");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual(9125, options.AgentPort);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithInvalidPort_KeepsDefault()
        {
            System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", "notanumber");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual(8125, options.AgentPort);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_WithDDDogstatsdSocket_SetsUds()
        {
            System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_SOCKET", "/var/run/datadog/dsd.socket");
            try
            {
                var options = new DatadogOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("/var/run/datadog/dsd.socket", options.UnixDomainSocketPath);
                Assert.IsTrue(options.UseUnixDomainSocket);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_SOCKET", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitValuesNotOverwritten()
        {
            System.Environment.SetEnvironmentVariable("DD_SERVICE", "env-service");
            try
            {
                var options = new DatadogOptions
                {
                    ServiceName = "explicit-service"
                };
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("explicit-service", options.ServiceName);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_SERVICE", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitAgentHostNotOverwritten()
        {
            System.Environment.SetEnvironmentVariable("DD_AGENT_HOST", "env-host");
            try
            {
                var options = new DatadogOptions
                {
                    AgentHost = "explicit-host"
                };
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("explicit-host", options.AgentHost);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_AGENT_HOST", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitPortNotOverwritten()
        {
            System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", "9999");
            try
            {
                var options = new DatadogOptions
                {
                    AgentPort = 7777
                };
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual(7777, options.AgentPort);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_AddsUnifiedServiceTags()
        {
            var options = new DatadogOptions
            {
                ServiceName = "my-svc",
                Environment = "prod",
                Version = "1.0.0"
            };

            options.ApplyEnvironmentDefaults();

            Assert.AreEqual("my-svc", options.GlobalTags["service"]);
            Assert.AreEqual("prod", options.GlobalTags["env"]);
            Assert.AreEqual("1.0.0", options.GlobalTags["version"]);
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_DoesNotOverwriteExistingGlobalTags()
        {
            var options = new DatadogOptions
            {
                ServiceName = "new-svc",
                GlobalTags = new Dictionary<string, string>
                {
                    ["service"] = "existing-svc"
                }
            };

            options.ApplyEnvironmentDefaults();

            Assert.AreEqual("existing-svc", options.GlobalTags["service"]);
        }

        [TestMethod]
        public void GetEffectiveServerName_DefaultReturnsAgentHost()
        {
            var options = new DatadogOptions { AgentHost = "my-agent.local" };

            Assert.AreEqual("my-agent.local", options.GetEffectiveServerName());
        }

        [TestMethod]
        public void GetEffectiveServerName_WithUds_ReturnsSocketPath()
        {
            var options = new DatadogOptions
            {
                UseUnixDomainSocket = true,
                UnixDomainSocketPath = "/var/run/datadog/dsd.socket"
            };

            var result = options.GetEffectiveServerName();
            Assert.AreEqual("unix:///var/run/datadog/dsd.socket", result);
        }

        [TestMethod]
        public void GetEffectiveServerName_WithUdsUnixPrefix_DoesNotDoublePrefix()
        {
            var options = new DatadogOptions
            {
                UseUnixDomainSocket = true,
                UnixDomainSocketPath = "unix:///var/run/datadog/dsd.socket"
            };

            var result = options.GetEffectiveServerName();
            Assert.AreEqual("unix:///var/run/datadog/dsd.socket", result);
        }

        [TestMethod]
        public void GetEffectiveServerName_UdsEnabledButNoPath_ReturnsAgentHost()
        {
            var options = new DatadogOptions
            {
                UseUnixDomainSocket = true,
                UnixDomainSocketPath = null,
                AgentHost = "fallback-host"
            };

            Assert.AreEqual("fallback-host", options.GetEffectiveServerName());
        }

        [TestMethod]
        public void DatadogExportMode_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)DatadogExportMode.Auto);
            Assert.AreEqual(1, (int)DatadogExportMode.OTLP);
            Assert.AreEqual(2, (int)DatadogExportMode.DogStatsD);
        }
    }
}
