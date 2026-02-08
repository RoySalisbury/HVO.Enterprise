using System;
using System.Reflection;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfigurationProviderTests
    {
        [TestMethod]
        public void ConfigurationProvider_CallOverridesMethod()
        {
            var provider = new ConfigurationProvider();
            var method = typeof(SampleService).GetMethod(nameof(SampleService.DoWork));

            provider.SetMethodConfiguration(method!, new OperationConfiguration { SamplingRate = 0.3 });

            var callConfig = new OperationConfiguration { SamplingRate = 0.8 };
            var effective = provider.GetEffectiveConfiguration(typeof(SampleService), method, callConfig);

            Assert.AreEqual(0.8, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationProvider_MethodOverridesType()
        {
            var provider = new ConfigurationProvider();
            var method = typeof(SampleService).GetMethod(nameof(SampleService.DoWork));

            provider.SetTypeConfiguration(typeof(SampleService), new OperationConfiguration { SamplingRate = 0.2 });
            provider.SetMethodConfiguration(method!, new OperationConfiguration { SamplingRate = 0.6 });

            var effective = provider.GetEffectiveConfiguration(typeof(SampleService), method);

            Assert.AreEqual(0.6, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationProvider_TypeOverridesNamespace()
        {
            var provider = new ConfigurationProvider();

            provider.SetNamespaceConfiguration("HVO.Enterprise.Telemetry.Tests.*", new OperationConfiguration { SamplingRate = 0.1 });
            provider.SetTypeConfiguration(typeof(SampleService), new OperationConfiguration { SamplingRate = 0.9 });

            var effective = provider.GetEffectiveConfiguration(typeof(SampleService));

            Assert.AreEqual(0.9, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationProvider_SourcePrecedence_RuntimeOverridesFileAndCode()
        {
            var provider = new ConfigurationProvider();
            var method = typeof(SampleService).GetMethod(nameof(SampleService.DoWork));

            provider.SetMethodConfiguration(method!, new OperationConfiguration { SamplingRate = 0.2 }, ConfigurationSourceKind.Code);
            provider.SetMethodConfiguration(method!, new OperationConfiguration { SamplingRate = 0.5 }, ConfigurationSourceKind.File);
            provider.SetMethodConfiguration(method!, new OperationConfiguration { SamplingRate = 0.8 }, ConfigurationSourceKind.Runtime);

            var effective = provider.GetEffectiveConfiguration(typeof(SampleService), method);

            Assert.AreEqual(0.8, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationProvider_NamespaceExactMatch_WinsOverWildcard()
        {
            var provider = new ConfigurationProvider();
            var namespaceValue = typeof(SampleService).Namespace;

            provider.SetNamespaceConfiguration("HVO.Enterprise.Telemetry.Tests.*", new OperationConfiguration { SamplingRate = 0.2 });
            provider.SetNamespaceConfiguration(namespaceValue!, new OperationConfiguration { SamplingRate = 0.6 });

            var effective = provider.GetEffectiveConfiguration(typeof(SampleService));

            Assert.AreEqual(0.6, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationProvider_NamespaceLongestPrefix_Wins()
        {
            var provider = new ConfigurationProvider();
            var namespaceValue = typeof(SampleService).Namespace;

            provider.SetNamespaceConfiguration("HVO.Enterprise.*", new OperationConfiguration { SamplingRate = 0.1 });
            provider.SetNamespaceConfiguration("HVO.Enterprise.Telemetry.*", new OperationConfiguration { SamplingRate = 0.4 });
            provider.SetNamespaceConfiguration(namespaceValue! + ".*", new OperationConfiguration { SamplingRate = 0.7 });

            var effective = provider.GetEffectiveConfiguration(typeof(SampleService));

            Assert.AreEqual(0.7, effective.SamplingRate);
        }

        private sealed class SampleService
        {
            public void DoWork()
            {
            }
        }
    }
}
