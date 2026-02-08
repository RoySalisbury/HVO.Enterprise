using System;
using System.Reflection;
using System.Text.Json;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfigurationFileProviderTests
    {
        [TestMethod]
        public void ConfigurationFileProvider_AppliesGlobalAndNamespaceOverrides()
        {
            var provider = new ConfigurationProvider();

            var payload = new HierarchicalConfigurationFile
            {
                Global = new OperationConfiguration { SamplingRate = 0.2 },
                Namespaces =
                {
                    ["HVO.Enterprise.Telemetry.Tests.*"] = new OperationConfiguration { Enabled = false }
                }
            };

            ConfigurationFileProvider.ApplyTo(provider, payload, ConfigurationSourceKind.File);

            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService));

            Assert.AreEqual(0.2, effective.SamplingRate);
            Assert.AreEqual(false, effective.Enabled);
        }

        [TestMethod]
        public void ConfigurationFileProvider_AppliesTypeAndMethodOverrides()
        {
            var provider = new ConfigurationProvider();

            var payload = new HierarchicalConfigurationFile
            {
                Types =
                {
                    [typeof(FileConfiguredService).AssemblyQualifiedName!] = new OperationConfiguration { SamplingRate = 0.4 }
                },
                Methods =
                {
                    [typeof(FileConfiguredService).AssemblyQualifiedName + "::Run"] = new OperationConfiguration { SamplingRate = 0.9 }
                }
            };

            ConfigurationFileProvider.ApplyTo(
                provider,
                payload,
                ConfigurationSourceKind.File,
                ResolveType,
                ResolveMethod);

            var method = typeof(FileConfiguredService).GetMethod(nameof(FileConfiguredService.Run));
            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService), method);

            Assert.AreEqual(0.9, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationFileProvider_LoadFromJson_ValidatesPayload()
        {
            var payload = new HierarchicalConfigurationFile
            {
                Global = new OperationConfiguration { SamplingRate = 0.5 }
            };

            var json = JsonSerializer.Serialize(payload);
            var parsed = ConfigurationFileProvider.LoadFromJson(json);

            Assert.IsNotNull(parsed);
            Assert.IsNotNull(parsed.Global);
            Assert.AreEqual(0.5, parsed.Global!.SamplingRate);
        }

        private static Type? ResolveType(string typeName)
        {
            return typeof(FileConfiguredService).AssemblyQualifiedName == typeName
                ? typeof(FileConfiguredService)
                : null;
        }

        private static MethodInfo? ResolveMethod(Type type, string methodName)
        {
            return type == typeof(FileConfiguredService)
                ? type.GetMethod(methodName)
                : null;
        }

        private sealed class FileConfiguredService
        {
            public void Run()
            {
            }
        }
    }
}
