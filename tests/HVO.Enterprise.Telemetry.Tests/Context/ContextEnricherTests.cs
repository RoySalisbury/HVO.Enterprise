using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class ContextEnricherTests
    {
        [TestMethod]
        public void ContextEnricher_InvokesProviders()
        {
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard };
            var enricher = new ContextEnricher(options);
            var provider = new TestProvider();
            
            using (var activity = new Activity("test"))
            {
                enricher.RegisterProvider(provider);
                enricher.EnrichActivity(activity);

                Assert.IsTrue(provider.WasInvoked);
            }
        }

        [TestMethod]
        public void ContextEnricher_SwallowsProviderExceptions()
        {
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard };
            var enricher = new ContextEnricher(options);
            var provider = new ThrowingProvider();
            
            using (var activity = new Activity("test"))
            {
                enricher.RegisterProvider(provider);
                enricher.EnrichActivity(activity);
            }
        }

        private sealed class TestProvider : IContextProvider
        {
            public string Name => "Test";

            public EnrichmentLevel Level => EnrichmentLevel.Standard;

            public bool WasInvoked { get; private set; }

            public void EnrichActivity(Activity activity, EnrichmentOptions options)
            {
                WasInvoked = true;
            }

            public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
            {
                WasInvoked = true;
            }
        }

        private sealed class ThrowingProvider : IContextProvider
        {
            public string Name => "Throwing";

            public EnrichmentLevel Level => EnrichmentLevel.Standard;

            public void EnrichActivity(Activity activity, EnrichmentOptions options)
            {
                throw new InvalidOperationException("boom");
            }

            public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
            {
                throw new InvalidOperationException("boom");
            }
        }
    }
}
