using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Internal
{
    /// <summary>
    /// Tests for <see cref="NoOpOperationScope"/>.
    /// </summary>
    [TestClass]
    public class NoOpOperationScopeTests
    {
        [TestMethod]
        public void Constructor_WithName_SetsName()
        {
            var scope = new NoOpOperationScope("test-op");
            Assert.AreEqual("test-op", scope.Name);
        }

        [TestMethod]
        public void Constructor_WithNullName_SetsEmptyString()
        {
            var scope = new NoOpOperationScope(null!);
            Assert.AreEqual(string.Empty, scope.Name);
        }

        [TestMethod]
        public void CorrelationId_IsEmpty()
        {
            var scope = new NoOpOperationScope("op");
            Assert.AreEqual(string.Empty, scope.CorrelationId);
        }

        [TestMethod]
        public void Activity_ReturnsNull()
        {
            var scope = new NoOpOperationScope("op");
            Assert.IsNull(scope.Activity);
        }

        [TestMethod]
        public void Elapsed_ReturnsNonNegative()
        {
            var scope = new NoOpOperationScope("op");
            System.Threading.Thread.Sleep(1);
            Assert.IsTrue(scope.Elapsed >= TimeSpan.Zero);
        }

        [TestMethod]
        public void WithTag_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithTag("key", "value");
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithTag_NullKey_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithTag(null!, null);
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithTags_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var tags = new Dictionary<string, object?> { ["a"] = 1, ["b"] = "two" };
            var result = scope.WithTags(tags);
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithTags_NullCollection_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithTags(null!);
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithProperty_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithProperty("key", () => "value");
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithProperty_NullKeyAndFactory_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithProperty(null!, null!);
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void Fail_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.Fail(new Exception("boom"));
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void Fail_NullException_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.Fail(null!);
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void Succeed_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.Succeed();
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithResult_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithResult("some-result");
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void WithResult_Null_ReturnsSelf()
        {
            var scope = new NoOpOperationScope("op");
            var result = scope.WithResult(null);
            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void CreateChild_ReturnsNewNoOpScope()
        {
            var scope = new NoOpOperationScope("parent");
            var child = scope.CreateChild("child-op");
            Assert.IsNotNull(child);
            Assert.IsInstanceOfType(child, typeof(NoOpOperationScope));
            Assert.AreEqual("child-op", child.Name);
            Assert.AreNotSame(scope, child);
        }

        [TestMethod]
        public void RecordException_DoesNotThrow()
        {
            var scope = new NoOpOperationScope("op");
            scope.RecordException(new InvalidOperationException("test"));
        }

        [TestMethod]
        public void RecordException_NullException_DoesNotThrow()
        {
            var scope = new NoOpOperationScope("op");
            scope.RecordException(null!);
        }

        [TestMethod]
        public void Dispose_DoesNotThrow()
        {
            var scope = new NoOpOperationScope("op");
            scope.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var scope = new NoOpOperationScope("op");
            scope.Dispose();
            scope.Dispose();
        }

        [TestMethod]
        public void FluentChaining_AllMethodsChainable()
        {
            var scope = new NoOpOperationScope("chained");
            var result = scope
                .WithTag("key", "value")
                .WithTags(new Dictionary<string, object?> { ["a"] = 1 })
                .WithProperty("p", () => "v")
                .Succeed()
                .WithResult("done");
            Assert.AreSame(scope, result);
        }
    }
}
