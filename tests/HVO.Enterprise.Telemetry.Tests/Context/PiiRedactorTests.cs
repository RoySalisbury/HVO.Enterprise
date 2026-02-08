using HVO.Enterprise.Telemetry.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class PiiRedactorTests
    {
        [TestMethod]
        public void PiiRedactor_MasksEmail_WhenEnabled()
        {
            var redactor = new PiiRedactor();
            var options = new EnrichmentOptions { RedactPii = true, RedactionStrategy = PiiRedactionStrategy.Mask };

            var result = redactor.TryRedact("user.name", "test@example.com", options, out var redacted);

            Assert.IsTrue(result);
            Assert.AreEqual("***", redacted);
        }

        [TestMethod]
        public void PiiRedactor_Removes_WhenStrategyRemove()
        {
            var redactor = new PiiRedactor();
            var options = new EnrichmentOptions { RedactPii = true, RedactionStrategy = PiiRedactionStrategy.Remove };

            var result = redactor.TryRedact("user.name", "test@example.com", options, out var redacted);

            Assert.IsTrue(result);
            Assert.IsNull(redacted);
        }

        [TestMethod]
        public void PiiRedactor_Hashes_WhenStrategyHash()
        {
            var redactor = new PiiRedactor();
            var options = new EnrichmentOptions { RedactPii = true, RedactionStrategy = PiiRedactionStrategy.Hash };

            var result = redactor.TryRedact("user.name", "test@example.com", options, out var redacted);

            Assert.IsTrue(result);
            Assert.IsFalse(string.IsNullOrEmpty(redacted));
            Assert.AreNotEqual("test@example.com", redacted);
        }

        [TestMethod]
        public void PiiRedactor_PartialMasks_WhenStrategyPartial()
        {
            var redactor = new PiiRedactor();
            var options = new EnrichmentOptions { RedactPii = true, RedactionStrategy = PiiRedactionStrategy.Partial };

            var result = redactor.TryRedact("user.name", "test@example.com", options, out var redacted);

            Assert.IsTrue(result);
            Assert.AreEqual("te***om", redacted);
        }

        [TestMethod]
        public void PiiRedactor_UsesPropertyList()
        {
            var redactor = new PiiRedactor();
            var options = new EnrichmentOptions { RedactPii = true, RedactionStrategy = PiiRedactionStrategy.Mask };

            var result = redactor.TryRedact("password", "not-a-pattern", options, out var redacted);

            Assert.IsTrue(result);
            Assert.AreEqual("***", redacted);
        }

        [TestMethod]
        public void PiiRedactor_ReturnsOriginal_WhenRedactionDisabled()
        {
            var redactor = new PiiRedactor();
            var options = new EnrichmentOptions { RedactPii = false };

            var result = redactor.TryRedact("user.name", "test@example.com", options, out var redacted);

            Assert.IsTrue(result);
            Assert.AreEqual("test@example.com", redacted);
        }
    }
}
