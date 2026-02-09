using System;
using HVO.Enterprise.Telemetry.Capture;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Capture
{
    [TestClass]
    public class RedactionStrategyTests
    {
        // ─── Enum values ────────────────────────────────────────────────

        [TestMethod]
        public void RedactionStrategy_HasAllExpectedValues()
        {
            Assert.AreEqual(0, (int)RedactionStrategy.Remove);
            Assert.AreEqual(1, (int)RedactionStrategy.Mask);
            Assert.AreEqual(2, (int)RedactionStrategy.Hash);
            Assert.AreEqual(3, (int)RedactionStrategy.Partial);
            Assert.AreEqual(4, (int)RedactionStrategy.TypeName);
        }

        // ─── Mask strategy ──────────────────────────────────────────────

        [TestMethod]
        public void Redact_Mask_ReturnsTripleAsterisk()
        {
            var result = ParameterCapture.RedactValue("secret123", RedactionStrategy.Mask);

            Assert.AreEqual("***", result);
        }

        [TestMethod]
        public void Redact_Mask_NullValue_ReturnsTripleAsterisk()
        {
            var result = ParameterCapture.RedactValue(null, RedactionStrategy.Mask);

            Assert.AreEqual("***", result);
        }

        // ─── Remove strategy ────────────────────────────────────────────

        [TestMethod]
        public void Redact_Remove_ReturnsNull()
        {
            var result = ParameterCapture.RedactValue("secret123", RedactionStrategy.Remove);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Redact_Remove_NullValue_ReturnsNull()
        {
            var result = ParameterCapture.RedactValue(null, RedactionStrategy.Remove);

            Assert.IsNull(result);
        }

        // ─── Hash strategy ──────────────────────────────────────────────

        [TestMethod]
        public void Redact_Hash_ReturnsNonReversibleHash()
        {
            var result = (string)ParameterCapture.RedactValue("secret123", RedactionStrategy.Hash)!;

            Assert.IsNotNull(result);
            Assert.AreNotEqual("secret123", result);
            Assert.AreEqual(8, result.Length); // 4 bytes × 2 hex chars each
        }

        [TestMethod]
        public void Redact_Hash_SameInput_SameOutput()
        {
            var result1 = ParameterCapture.RedactValue("same-input", RedactionStrategy.Hash);
            var result2 = ParameterCapture.RedactValue("same-input", RedactionStrategy.Hash);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void Redact_Hash_DifferentInput_DifferentOutput()
        {
            var result1 = ParameterCapture.RedactValue("input-a", RedactionStrategy.Hash);
            var result2 = ParameterCapture.RedactValue("input-b", RedactionStrategy.Hash);

            Assert.AreNotEqual(result1, result2);
        }

        [TestMethod]
        public void Redact_Hash_NullValue_ReturnsMask()
        {
            var result = ParameterCapture.RedactValue(null, RedactionStrategy.Hash);

            Assert.AreEqual("***", result);
        }

        // ─── Partial strategy ───────────────────────────────────────────

        [TestMethod]
        public void Redact_Partial_ShowsFirstLastChars()
        {
            var result = (string)ParameterCapture.RedactValue("user@example.com", RedactionStrategy.Partial)!;

            // "us***om"
            Assert.IsTrue(result.StartsWith("us"), $"Got: {result}");
            Assert.IsTrue(result.EndsWith("om"), $"Got: {result}");
            Assert.IsTrue(result.Contains("***"), $"Got: {result}");
        }

        [TestMethod]
        public void Redact_Partial_ShortValue_ReturnsMask()
        {
            var result = ParameterCapture.RedactValue("ab", RedactionStrategy.Partial);

            Assert.AreEqual("***", result); // <= 4 chars → full mask
        }

        [TestMethod]
        public void Redact_Partial_ExactlyFourChars_ReturnsMask()
        {
            var result = ParameterCapture.RedactValue("abcd", RedactionStrategy.Partial);

            Assert.AreEqual("***", result); // <= 4 chars → full mask
        }

        [TestMethod]
        public void Redact_Partial_FiveChars_ShowsPartial()
        {
            var result = (string)ParameterCapture.RedactValue("abcde", RedactionStrategy.Partial)!;

            Assert.AreEqual("ab***de", result);
        }

        [TestMethod]
        public void Redact_Partial_NullValue_ReturnsMask()
        {
            var result = ParameterCapture.RedactValue(null, RedactionStrategy.Partial);

            Assert.AreEqual("***", result);
        }

        [TestMethod]
        public void Redact_Partial_EmptyString_ReturnsMask()
        {
            var result = ParameterCapture.RedactValue("", RedactionStrategy.Partial);

            Assert.AreEqual("***", result);
        }

        // ─── TypeName strategy ──────────────────────────────────────────

        [TestMethod]
        public void Redact_TypeName_ReturnsTypeName()
        {
            var result = ParameterCapture.RedactValue("secret123", RedactionStrategy.TypeName);

            Assert.AreEqual("String", result);
        }

        [TestMethod]
        public void Redact_TypeName_IntValue_ReturnsInt32()
        {
            var result = ParameterCapture.RedactValue(42, RedactionStrategy.TypeName);

            Assert.AreEqual("Int32", result);
        }

        [TestMethod]
        public void Redact_TypeName_ComplexType_ReturnsClassName()
        {
            var result = ParameterCapture.RedactValue(
                new PersonForTest { Name = "Alice" },
                RedactionStrategy.TypeName);

            Assert.AreEqual("PersonForTest", result);
        }

        [TestMethod]
        public void Redact_TypeName_NullValue_ReturnsNullString()
        {
            var result = ParameterCapture.RedactValue(null, RedactionStrategy.TypeName);

            Assert.AreEqual("null", result);
        }

        // ─── Integration with CaptureParameter ──────────────────────────

        [TestMethod]
        public void CaptureParameter_PasswordWithMaskRedaction_ReturnsMask()
        {
            var capture = new ParameterCapture();
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                AutoDetectSensitiveData = true,
                // password pattern defaults to Mask
            };

            var result = capture.CaptureParameter("password", "my-pw", typeof(string), options);

            Assert.AreEqual("***", result);
        }

        [TestMethod]
        public void CaptureParameter_CreditCardWithHashRedaction_ReturnsHash()
        {
            var capture = new ParameterCapture();
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                AutoDetectSensitiveData = true,
            };

            var result = capture.CaptureParameter("creditCard", "4111111111111111", typeof(string), options);

            // creditcard pattern has Hash strategy
            var str = (string)result!;
            Assert.AreNotEqual("***", str);
            Assert.AreNotEqual("4111111111111111", str);
            Assert.AreEqual(8, str.Length);
        }

        // ─── Helper types ───────────────────────────────────────────────

        private class PersonForTest
        {
            public string? Name { get; set; }
        }
    }
}
