using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    [TestClass]
    public class AttributeTests
    {
        // ─── InstrumentMethodAttribute ──────────────────────────────────

        [TestMethod]
        public void InstrumentMethodAttribute_DefaultValues_AreCorrect()
        {
            var attr = new InstrumentMethodAttribute();

            Assert.IsNull(attr.OperationName);
            Assert.AreEqual(ActivityKind.Internal, attr.ActivityKind);
            Assert.IsTrue(attr.CaptureParameters);
            Assert.IsFalse(attr.CaptureReturnValue);
            Assert.IsTrue(attr.LogEvents);
            Assert.AreEqual(LogLevel.Debug, attr.LogLevel);
        }

        [TestMethod]
        public void InstrumentMethodAttribute_CustomValues_AreRetained()
        {
            var attr = new InstrumentMethodAttribute
            {
                OperationName = "MyOp",
                ActivityKind = ActivityKind.Client,
                CaptureParameters = false,
                CaptureReturnValue = true,
                LogEvents = false,
                LogLevel = LogLevel.Warning
            };

            Assert.AreEqual("MyOp", attr.OperationName);
            Assert.AreEqual(ActivityKind.Client, attr.ActivityKind);
            Assert.IsFalse(attr.CaptureParameters);
            Assert.IsTrue(attr.CaptureReturnValue);
            Assert.IsFalse(attr.LogEvents);
            Assert.AreEqual(LogLevel.Warning, attr.LogLevel);
        }

        // ─── InstrumentClassAttribute ───────────────────────────────────

        [TestMethod]
        public void InstrumentClassAttribute_DefaultValues_AreCorrect()
        {
            var attr = new InstrumentClassAttribute();

            Assert.IsNull(attr.OperationPrefix);
            Assert.AreEqual(ActivityKind.Internal, attr.ActivityKind);
            Assert.IsTrue(attr.CaptureParameters);
            Assert.IsTrue(attr.LogEvents);
        }

        [TestMethod]
        public void InstrumentClassAttribute_CustomValues_AreRetained()
        {
            var attr = new InstrumentClassAttribute
            {
                OperationPrefix = "Svc",
                ActivityKind = ActivityKind.Server,
                CaptureParameters = false,
                LogEvents = false
            };

            Assert.AreEqual("Svc", attr.OperationPrefix);
            Assert.AreEqual(ActivityKind.Server, attr.ActivityKind);
            Assert.IsFalse(attr.CaptureParameters);
            Assert.IsFalse(attr.LogEvents);
        }

        // ─── SensitiveDataAttribute ─────────────────────────────────────

        [TestMethod]
        public void SensitiveDataAttribute_DefaultStrategy_IsMask()
        {
            var attr = new SensitiveDataAttribute();
            Assert.AreEqual(RedactionStrategy.Mask, attr.Strategy);
        }

        [TestMethod]
        public void SensitiveDataAttribute_CanSetHash()
        {
            var attr = new SensitiveDataAttribute { Strategy = RedactionStrategy.Hash };
            Assert.AreEqual(RedactionStrategy.Hash, attr.Strategy);
        }

        [TestMethod]
        public void SensitiveDataAttribute_CanSetRemove()
        {
            var attr = new SensitiveDataAttribute { Strategy = RedactionStrategy.Remove };
            Assert.AreEqual(RedactionStrategy.Remove, attr.Strategy);
        }

        // ─── NoTelemetryAttribute ───────────────────────────────────────

        [TestMethod]
        public void NoTelemetryAttribute_CanBeCreated()
        {
            var attr = new NoTelemetryAttribute();
            Assert.IsNotNull(attr);
        }

        // ─── InstrumentationOptions ─────────────────────────────────────

        [TestMethod]
        public void InstrumentationOptions_DefaultValues_AreCorrect()
        {
            var options = new InstrumentationOptions();

            Assert.AreEqual(2, options.MaxCaptureDepth);
            Assert.AreEqual(10, options.MaxCollectionItems);
            Assert.IsTrue(options.CaptureComplexTypes);
            Assert.IsTrue(options.AutoDetectPii);
        }

        [TestMethod]
        public void InstrumentationOptions_CustomValues_AreRetained()
        {
            var options = new InstrumentationOptions
            {
                MaxCaptureDepth = 5,
                MaxCollectionItems = 3,
                CaptureComplexTypes = false,
                AutoDetectPii = false
            };

            Assert.AreEqual(5, options.MaxCaptureDepth);
            Assert.AreEqual(3, options.MaxCollectionItems);
            Assert.IsFalse(options.CaptureComplexTypes);
            Assert.IsFalse(options.AutoDetectPii);
        }

        // ─── RedactionStrategy enum ─────────────────────────────────────

        [TestMethod]
        public void RedactionStrategy_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)RedactionStrategy.Remove);
            Assert.AreEqual(1, (int)RedactionStrategy.Mask);
            Assert.AreEqual(2, (int)RedactionStrategy.Hash);
        }
    }
}
