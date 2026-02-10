using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.ThreadSafety
{
    /// <summary>
    /// Validates that <see cref="OperationScopeFactory"/> and <see cref="IOperationScope"/>
    /// are safe to use from multiple threads concurrently.
    /// </summary>
    [TestClass]
    public class ConcurrentOperationScopeTests
    {
        [TestMethod]
        public void OperationScopeFactory_ConcurrentBegin_CreatesSeparateScopes()
        {
            // Arrange
            using var testSource = new TestActivitySource("concurrent-scope-test");
            var factory = new OperationScopeFactory(testSource.Source);
            const int threadCount = 30;
            var scopeNames = new ConcurrentBag<string>();

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                using var scope = factory.Begin($"operation-{index}");
                scopeNames.Add(scope.Name);
                Thread.SpinWait(100);
            });

            // Assert
            Assert.AreEqual(threadCount, scopeNames.Count);
            for (int i = 0; i < threadCount; i++)
            {
                Assert.IsTrue(scopeNames.Contains($"operation-{i}"),
                    $"Scope 'operation-{i}' was not created");
            }
        }

        [TestMethod]
        public void OperationScope_ConcurrentWithTag_NoExceptions()
        {
            // Arrange
            using var testSource = new TestActivitySource("concurrent-tag-test");
            var factory = new OperationScopeFactory(testSource.Source);
            const int threadCount = 20;

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                using var scope = factory.Begin($"tagged-op-{index}");
                for (int i = 0; i < 50; i++)
                {
                    scope.WithTag($"key-{i}", $"value-{index}-{i}");
                }
                scope.Succeed();
            });

            // Assert - no exceptions = thread-safe
        }

        [TestMethod]
        public void OperationScope_ConcurrentFailAndSucceed_NoExceptions()
        {
            // Arrange
            using var testSource = new TestActivitySource("concurrent-result-test");
            var factory = new OperationScopeFactory(testSource.Source);
            const int threadCount = 20;

            // Act - some succeed, some fail
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                using var scope = factory.Begin($"result-op-{index}");
                if (index % 2 == 0)
                {
                    scope.Succeed();
                }
                else
                {
                    scope.Fail(new InvalidOperationException($"Failure {index}"));
                }
            });

            // Assert - no exceptions
        }

        [TestMethod]
        public void OperationScope_ConcurrentChildScope_ParentChildIsolation()
        {
            // Arrange
            using var testSource = new TestActivitySource("concurrent-child-test");
            var factory = new OperationScopeFactory(testSource.Source);
            const int threadCount = 10;
            var childNames = new ConcurrentBag<string>();

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                using var parent = factory.Begin($"parent-{index}");
                using var child = parent.CreateChild($"child-{index}");
                childNames.Add(child.Name);
                child.WithTag("parent-index", index.ToString());
                child.Succeed();
                parent.Succeed();
            });

            // Assert
            Assert.AreEqual(threadCount, childNames.Count);
        }

        [TestMethod]
        public void OperationScope_ConcurrentRecordException_NoExceptions()
        {
            // Arrange
            using var testSource = new TestActivitySource("concurrent-exception-test");
            var factory = new OperationScopeFactory(testSource.Source);
            const int threadCount = 20;

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                using var scope = factory.Begin($"exception-op-{index}");
                scope.RecordException(new Exception($"Test error {index}"));
            });

            // Assert - no exceptions
        }
    }
}
