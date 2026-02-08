using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    [TestClass]
    public class OperationScopePerformanceTests
    {
        [TestMethod]
        public void OperationScope_CreateAndDispose_IsFast()
        {
            var factory = CreateFactory();
            const int iterations = 10000;

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                using (var scope = factory.Begin("Perf"))
                {
                    scope.Succeed();
                }
            }
            stopwatch.Stop();

            var perOperationMs = stopwatch.Elapsed.TotalMilliseconds / iterations;
            Assert.IsTrue(perOperationMs < 0.05, "Expected scope creation + disposal under 0.05ms per op.");
        }

        [TestMethod]
        public void OperationScope_TagAddition_IsFast()
        {
            var factory = CreateFactory();
            const int iterations = 10000;

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                using (var scope = factory.Begin("Perf"))
                {
                    scope.WithTag("tag.key", "value")
                        .WithTag("tag.number", i)
                        .Succeed();
                }
            }
            stopwatch.Stop();

            var perOperationMs = stopwatch.Elapsed.TotalMilliseconds / iterations;
            Assert.IsTrue(perOperationMs < 0.08, "Expected tag addition under 0.08ms per op.");
        }

        [TestMethod]
        public void OperationScope_DisposalWithLazyProperties_IsFast()
        {
            var factory = CreateFactory();
            const int iterations = 10000;

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                using (var scope = factory.Begin("Perf"))
                {
                    scope.WithProperty("lazy", () => i)
                        .Succeed();
                }
            }
            stopwatch.Stop();

            var perOperationMs = stopwatch.Elapsed.TotalMilliseconds / iterations;
            Assert.IsTrue(perOperationMs < 0.1, "Expected disposal with lazy properties under 0.1ms per op.");
        }

        private static OperationScopeFactory CreateFactory()
        {
            var sourceName = "HVO.Enterprise.Telemetry.Tests." + Guid.NewGuid().ToString("N");
            return new OperationScopeFactory(sourceName, "1.0.0");
        }
    }
}
