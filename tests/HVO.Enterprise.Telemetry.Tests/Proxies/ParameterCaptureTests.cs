using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    [TestClass]
    public class ParameterCaptureTests
    {
        private FakeOperationScopeFactory _scopeFactory = null!;
        private TelemetryProxyFactory _factory = null!;

        [TestInitialize]
        public void Setup()
        {
            _scopeFactory = new FakeOperationScopeFactory();
            _factory = new TelemetryProxyFactory(_scopeFactory);
        }

        // ─── SENSITIVE DATA ATTRIBUTE ───────────────────────────────────

        [TestMethod]
        public void SensitiveData_Mask_ReplacesWith3Stars()
        {
            var proxy = _factory.CreateProxy<ISensitiveService>(new SensitiveService());

            proxy.ProcessPayment(123, "4111111111111111", "user@test.com", "123-45-6789");

            var tags = _scopeFactory.LastScope!.Tags;

            // orderId should be captured normally.
            Assert.AreEqual(123, tags["param.orderId"]);

            // creditCard: [SensitiveData] default = Mask
            Assert.AreEqual("***", tags["param.creditCard"]);
        }

        [TestMethod]
        public void SensitiveData_Hash_ProducesConsistentHash()
        {
            var proxy = _factory.CreateProxy<ISensitiveService>(new SensitiveService());

            proxy.ProcessPayment(1, "x", "user@test.com", "ssn");

            var tags = _scopeFactory.LastScope!.Tags;

            // email: Hash strategy
            var hash1 = (string)tags["param.email"]!;
            Assert.IsNotNull(hash1);
            Assert.AreNotEqual("***", hash1);
            Assert.AreNotEqual("user@test.com", hash1);
            // Hash should be 8-char hex
            Assert.AreEqual(8, hash1.Length);
        }

        [TestMethod]
        public void SensitiveData_Hash_SameInput_SameOutput()
        {
            var proxy = _factory.CreateProxy<ISensitiveService>(new SensitiveService());

            proxy.ProcessPayment(1, "x", "hello@world.com", "ssn");
            var hash1 = (string)_scopeFactory.LastScope!.Tags["param.email"]!;

            proxy.ProcessPayment(1, "x", "hello@world.com", "ssn");
            var hash2 = (string)_scopeFactory.LastScope!.Tags["param.email"]!;

            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void SensitiveData_Remove_SetsNull()
        {
            var proxy = _factory.CreateProxy<ISensitiveService>(new SensitiveService());

            proxy.ProcessPayment(1, "cc", "email", "123-45-6789");

            var tags = _scopeFactory.LastScope!.Tags;

            // ssn: Remove strategy → null
            Assert.IsTrue(tags.ContainsKey("param.ssn"));
            Assert.IsNull(tags["param.ssn"]);
        }

        // ─── AUTO PII DETECTION ─────────────────────────────────────────

        [TestMethod]
        public void AutoPiiDetection_PasswordParam_Masked()
        {
            var proxy = _factory.CreateProxy<IPiiAutoDetectService>(new PiiAutoDetectService());

            proxy.Login("admin", "secret123", "tok_abc");

            var tags = _scopeFactory.LastScope!.Tags;

            // password and token auto-detected as PII
            Assert.AreEqual("admin", tags["param.username"]);
            Assert.AreEqual("***", tags["param.password"]);
            Assert.AreEqual("***", tags["param.token"]);
        }

        [TestMethod]
        public void AutoPiiDetection_Disabled_CapturesAll()
        {
            var options = new InstrumentationOptions { AutoDetectPii = false };
            var proxy = _factory.CreateProxy<IPiiAutoDetectService>(new PiiAutoDetectService(), options);

            proxy.Login("admin", "secret123", "tok_abc");

            var tags = _scopeFactory.LastScope!.Tags;
            Assert.AreEqual("admin", tags["param.username"]);
            Assert.AreEqual("secret123", tags["param.password"]);
            Assert.AreEqual("tok_abc", tags["param.token"]);
        }

        // ─── COMPLEX TYPE CAPTURE ───────────────────────────────────────

        [TestMethod]
        public void ComplexType_CapturesPublicProperties()
        {
            var proxy = _factory.CreateProxy<IComplexParamService>(new ComplexParamService());

            proxy.Process(new ComplexParam { Name = "Alice", Age = 30, Secret = "s3cret" });

            var tags = _scopeFactory.LastScope!.Tags;
            Assert.IsTrue(tags.ContainsKey("param.param"));

            // Complex type captured as dictionary.
            var dict = tags["param.param"] as Dictionary<string, object?>;
            Assert.IsNotNull(dict);
            Assert.AreEqual("Alice", dict!["Name"]);
            Assert.AreEqual(30, dict["Age"]);
            // Secret has [SensitiveData] on property → redacted with Mask strategy.
            Assert.IsTrue(dict.ContainsKey("Secret"));
            Assert.AreEqual("***", dict["Secret"]);
        }

        [TestMethod]
        public void ComplexType_CaptureDisabled_UsesToString()
        {
            var options = new InstrumentationOptions { CaptureComplexTypes = false };
            var proxy = _factory.CreateProxy<IComplexParamService>(new ComplexParamService(), options);

            proxy.Process(new ComplexParam { Name = "Bob" });

            var tags = _scopeFactory.LastScope!.Tags;
            var captured = tags["param.param"];

            // Should use ToString() result.
            Assert.IsInstanceOfType(captured, typeof(string));
        }

        // ─── COLLECTION CAPTURE ─────────────────────────────────────────

        [TestMethod]
        public void Collection_CapturesItemsUpToMax()
        {
            var options = new InstrumentationOptions { MaxCollectionItems = 3 };
            var proxy = _factory.CreateProxy<IComplexParamService>(new ComplexParamService(), options);

            proxy.ProcessList(new List<int> { 1, 2, 3, 4, 5 });

            var tags = _scopeFactory.LastScope!.Tags;
            var captured = tags["param.items"] as List<object?>;
            Assert.IsNotNull(captured);
            // 3 items + truncation marker
            Assert.AreEqual(4, captured!.Count);
            Assert.AreEqual(1, captured[0]);
            Assert.AreEqual(2, captured[1]);
            Assert.AreEqual(3, captured[2]);
            Assert.IsTrue(captured[3]!.ToString()!.Contains("truncated after"), $"Truncation marker was: {captured[3]}");
        }

        [TestMethod]
        public void Collection_SmallEnough_NoTruncation()
        {
            var proxy = _factory.CreateProxy<IComplexParamService>(new ComplexParamService());

            proxy.ProcessList(new List<int> { 10, 20 });

            var tags = _scopeFactory.LastScope!.Tags;
            var captured = tags["param.items"] as List<object?>;
            Assert.IsNotNull(captured);
            Assert.AreEqual(2, captured!.Count);
            Assert.AreEqual(10, captured[0]);
            Assert.AreEqual(20, captured[1]);
        }

        // ─── NULL PARAMETER ─────────────────────────────────────────────

        [TestMethod]
        public void NullParameter_CapturedAsNull()
        {
            var proxy = _factory.CreateProxy<IComplexParamService>(new ComplexParamService());

            proxy.Process(null!);

            var tags = _scopeFactory.LastScope!.Tags;
            Assert.IsTrue(tags.ContainsKey("param.param"));
            Assert.IsNull(tags["param.param"]);
        }

        // ─── DEPTH LIMIT ────────────────────────────────────────────────

        [TestMethod]
        public void DepthLimit_ExceededReturnsTypeName()
        {
            var options = new InstrumentationOptions { MaxCaptureDepth = 0, CaptureComplexTypes = true };
            var proxy = _factory.CreateProxy<IComplexParamService>(new ComplexParamService(), options);

            proxy.Process(new ComplexParam { Name = "test" });

            var tags = _scopeFactory.LastScope!.Tags;
            // At depth 0, complex types report max depth reached.
            Assert.IsTrue(tags["param.param"]!.ToString()!.Contains("Max depth"), $"Value was: {tags["param.param"]}");
        }
    }
}
