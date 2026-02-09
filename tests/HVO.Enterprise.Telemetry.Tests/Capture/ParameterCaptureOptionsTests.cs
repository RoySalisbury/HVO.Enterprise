using HVO.Enterprise.Telemetry.Capture;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Capture
{
    [TestClass]
    public class ParameterCaptureOptionsTests
    {
        // ─── Default values ─────────────────────────────────────────────

        [TestMethod]
        public void Default_HasExpectedValues()
        {
            var options = ParameterCaptureOptions.Default;

            Assert.AreEqual(CaptureLevel.Standard, options.Level);
            Assert.IsTrue(options.AutoDetectSensitiveData);
            Assert.AreEqual(RedactionStrategy.Mask, options.RedactionStrategy);
            Assert.AreEqual(2, options.MaxDepth);
            Assert.AreEqual(10, options.MaxCollectionItems);
            Assert.AreEqual(1000, options.MaxStringLength);
            Assert.IsTrue(options.UseCustomToString);
            Assert.IsTrue(options.CapturePropertyNames);
            Assert.IsNull(options.CustomSerializers);
        }

        // ─── InstrumentationOptions bridge ──────────────────────────────

        [TestMethod]
        public void InstrumentationOptions_ToParameterCaptureOptions_ComplexTypesEnabled()
        {
            var instrOptions = new InstrumentationOptions
            {
                CaptureComplexTypes = true,
                AutoDetectPii = true,
                MaxCaptureDepth = 3,
                MaxCollectionItems = 5
            };

            var captureOptions = instrOptions.ToParameterCaptureOptions();

            Assert.AreEqual(CaptureLevel.Verbose, captureOptions.Level);
            Assert.IsTrue(captureOptions.AutoDetectSensitiveData);
            Assert.AreEqual(3, captureOptions.MaxDepth);
            Assert.AreEqual(5, captureOptions.MaxCollectionItems);
            Assert.AreEqual(RedactionStrategy.Mask, captureOptions.RedactionStrategy);
        }

        [TestMethod]
        public void InstrumentationOptions_ToParameterCaptureOptions_ComplexTypesDisabled()
        {
            var instrOptions = new InstrumentationOptions
            {
                CaptureComplexTypes = false,
                AutoDetectPii = false,
                MaxCaptureDepth = 1,
                MaxCollectionItems = 20
            };

            var captureOptions = instrOptions.ToParameterCaptureOptions();

            Assert.AreEqual(CaptureLevel.Standard, captureOptions.Level);
            Assert.IsFalse(captureOptions.AutoDetectSensitiveData);
            Assert.AreEqual(1, captureOptions.MaxDepth);
            Assert.AreEqual(20, captureOptions.MaxCollectionItems);
        }

        // ─── Property setters ───────────────────────────────────────────

        [TestMethod]
        public void Options_AllPropertiesSettable()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                AutoDetectSensitiveData = false,
                RedactionStrategy = RedactionStrategy.Hash,
                MaxDepth = 5,
                MaxCollectionItems = 50,
                MaxStringLength = 500,
                UseCustomToString = false,
                CapturePropertyNames = false,
                CustomSerializers = new System.Collections.Generic.Dictionary<System.Type, System.Func<object, object?>>()
            };

            Assert.AreEqual(CaptureLevel.Verbose, options.Level);
            Assert.IsFalse(options.AutoDetectSensitiveData);
            Assert.AreEqual(RedactionStrategy.Hash, options.RedactionStrategy);
            Assert.AreEqual(5, options.MaxDepth);
            Assert.AreEqual(50, options.MaxCollectionItems);
            Assert.AreEqual(500, options.MaxStringLength);
            Assert.IsFalse(options.UseCustomToString);
            Assert.IsFalse(options.CapturePropertyNames);
            Assert.IsNotNull(options.CustomSerializers);
        }
    }
}
