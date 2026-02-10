using System;
using HVO.Enterprise.Telemetry.Context;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    /// <summary>
    /// Tests for <see cref="EnrichmentOptions"/> defaults and <c>EnsureDefaults</c> behavior.
    /// </summary>
    [TestClass]
    public class EnrichmentOptionsTests
    {
        [TestMethod]
        public void DefaultValues_MaxLevel_IsStandard()
        {
            var options = new EnrichmentOptions();
            Assert.AreEqual(EnrichmentLevel.Standard, options.MaxLevel);
        }

        [TestMethod]
        public void DefaultValues_RedactPii_IsTrue()
        {
            var options = new EnrichmentOptions();
            Assert.IsTrue(options.RedactPii);
        }

        [TestMethod]
        public void DefaultValues_RedactionStrategy_IsMask()
        {
            var options = new EnrichmentOptions();
            Assert.AreEqual(PiiRedactionStrategy.Mask, options.RedactionStrategy);
        }

        [TestMethod]
        public void DefaultValues_ExcludedHeaders_ContainsKnownHeaders()
        {
            var options = new EnrichmentOptions();
            Assert.IsNotNull(options.ExcludedHeaders);
            Assert.IsTrue(options.ExcludedHeaders.Contains("Authorization"));
            Assert.IsTrue(options.ExcludedHeaders.Contains("Cookie"));
            Assert.IsTrue(options.ExcludedHeaders.Contains("X-API-Key"));
            Assert.IsTrue(options.ExcludedHeaders.Contains("X-Auth-Token"));
        }

        [TestMethod]
        public void DefaultValues_PiiProperties_ContainsKnownProperties()
        {
            var options = new EnrichmentOptions();
            Assert.IsNotNull(options.PiiProperties);
            Assert.IsTrue(options.PiiProperties.Contains("email"));
            Assert.IsTrue(options.PiiProperties.Contains("ssn"));
            Assert.IsTrue(options.PiiProperties.Contains("creditcard"));
            Assert.IsTrue(options.PiiProperties.Contains("password"));
            Assert.IsTrue(options.PiiProperties.Contains("phone"));
            Assert.IsTrue(options.PiiProperties.Contains("token"));
            Assert.IsTrue(options.PiiProperties.Contains("apikey"));
        }

        [TestMethod]
        public void DefaultValues_PiiProperties_CaseInsensitive()
        {
            var options = new EnrichmentOptions();
            Assert.IsTrue(options.PiiProperties.Contains("EMAIL"));
            Assert.IsTrue(options.PiiProperties.Contains("Password"));
        }

        [TestMethod]
        public void DefaultValues_ExcludedHeaders_CaseInsensitive()
        {
            var options = new EnrichmentOptions();
            Assert.IsTrue(options.ExcludedHeaders.Contains("authorization"));
            Assert.IsTrue(options.ExcludedHeaders.Contains("COOKIE"));
        }

        [TestMethod]
        public void DefaultValues_CustomEnvironmentTags_IsEmpty()
        {
            var options = new EnrichmentOptions();
            Assert.IsNotNull(options.CustomEnvironmentTags);
            Assert.AreEqual(0, options.CustomEnvironmentTags.Count);
        }

        [TestMethod]
        public void EnsureDefaults_NullCollections_InitializesToDefaults()
        {
            var options = new EnrichmentOptions
            {
                ExcludedHeaders = null!,
                PiiProperties = null!,
                CustomEnvironmentTags = null!
            };

            options.EnsureDefaults();

            Assert.IsNotNull(options.ExcludedHeaders);
            Assert.IsNotNull(options.PiiProperties);
            Assert.IsNotNull(options.CustomEnvironmentTags);
        }

        [TestMethod]
        public void EnsureDefaults_NonNullCollections_DoesNotOverwrite()
        {
            var headers = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Custom" };
            var options = new EnrichmentOptions
            {
                ExcludedHeaders = headers
            };

            options.EnsureDefaults();

            Assert.AreSame(headers, options.ExcludedHeaders);
            Assert.IsTrue(options.ExcludedHeaders.Contains("Custom"));
        }

        [TestMethod]
        public void MaxLevel_CanBeSet()
        {
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose };
            Assert.AreEqual(EnrichmentLevel.Verbose, options.MaxLevel);
        }

        [TestMethod]
        public void RedactPii_CanBeDisabled()
        {
            var options = new EnrichmentOptions { RedactPii = false };
            Assert.IsFalse(options.RedactPii);
        }

        [TestMethod]
        public void RedactionStrategy_CanBeChanged()
        {
            var options = new EnrichmentOptions { RedactionStrategy = PiiRedactionStrategy.Hash };
            Assert.AreEqual(PiiRedactionStrategy.Hash, options.RedactionStrategy);
        }
    }
}
