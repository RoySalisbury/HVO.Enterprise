using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.ThreadSafety
{
    /// <summary>
    /// Validates that <see cref="CorrelationContext"/> is thread-safe and properly
    /// isolated across threads and async contexts.
    /// </summary>
    [TestClass]
    public class ConcurrentCorrelationTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void CorrelationContext_ConcurrentSetAndGet_ThreadIsolation()
        {
            // Arrange
            const int threadCount = 50;
            var results = new ConcurrentDictionary<int, string>();

            // Act - each thread sets its own correlation ID and reads it back
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                var expected = $"corr-{index}";
                CorrelationContext.Current = expected;

                // Yield to increase interleaving
                Thread.SpinWait(100);

                var actual = CorrelationContext.Current;
                results[index] = actual;
            });

            // Assert - each thread should have read its own value
            foreach (var kvp in results)
            {
                Assert.AreEqual($"corr-{kvp.Key}", kvp.Value,
                    $"Thread {kvp.Key} read wrong correlation ID");
            }
        }

        [TestMethod]
        public async Task CorrelationContext_AsyncFlowAcrossTasks_PreservesValue()
        {
            // Arrange
            var expected = Guid.NewGuid().ToString();
            CorrelationContext.Current = expected;

            // Act - flow across multiple async hops
            var result = await Task.Run(async () =>
            {
                var hop1 = CorrelationContext.Current;
                await Task.Yield();
                var hop2 = CorrelationContext.Current;
                await Task.Delay(1);
                var hop3 = CorrelationContext.Current;
                return (hop1, hop2, hop3);
            });

            // Assert
            Assert.AreEqual(expected, result.hop1);
            Assert.AreEqual(expected, result.hop2);
            Assert.AreEqual(expected, result.hop3);
        }

        [TestMethod]
        public async Task CorrelationContext_ParallelTasks_DoNotInterfere()
        {
            // Arrange
            const int taskCount = 100;
            var results = new ConcurrentDictionary<int, string>();

            // Act
            var tasks = Enumerable.Range(0, taskCount).Select(async i =>
            {
                var expected = $"task-{i}";
                CorrelationContext.Current = expected;
                await Task.Yield();
                results[i] = CorrelationContext.Current;
            });

            await Task.WhenAll(tasks);

            // Assert
            foreach (var kvp in results)
            {
                Assert.AreEqual($"task-{kvp.Key}", kvp.Value,
                    $"Task {kvp.Key} read wrong correlation ID");
            }
        }

        [TestMethod]
        public void CorrelationContext_ScopeOnMultipleThreads_EachScopeIsIsolated()
        {
            // Arrange
            const int threadCount = 20;
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                var outerValue = $"outer-{index}";
                CorrelationContext.Current = outerValue;

                using (CorrelationContext.BeginScope($"scope-{index}"))
                {
                    Assert.AreEqual($"scope-{index}", CorrelationContext.Current);
                    Thread.SpinWait(100);
                    Assert.AreEqual($"scope-{index}", CorrelationContext.Current);
                }

                // Validate restore after scope
                Assert.AreEqual(outerValue, CorrelationContext.Current);
            });
        }

        [TestMethod]
        public async Task CorrelationContext_NestedScopesAcrossAsyncBoundaries_RestoresCorrectly()
        {
            // Arrange
            var rootId = "root-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            CorrelationContext.Current = rootId;

            // Act
            using (CorrelationContext.BeginScope("level1"))
            {
                Assert.AreEqual("level1", CorrelationContext.Current);

                await Task.Yield();

                using (CorrelationContext.BeginScope("level2"))
                {
                    Assert.AreEqual("level2", CorrelationContext.Current);
                    await Task.Delay(1);
                    Assert.AreEqual("level2", CorrelationContext.Current);
                }

                Assert.AreEqual("level1", CorrelationContext.Current);
            }

            // Assert
            Assert.AreEqual(rootId, CorrelationContext.Current);
        }
    }
}
