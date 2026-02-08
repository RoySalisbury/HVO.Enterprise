using System;
using System.Diagnostics;
using System.Threading;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    [TestClass]
    public class OperationScopeTests
    {
        [TestMethod]
        public void OperationScope_CapturesTiming()
        {
            var factory = CreateFactory();

            using (var scope = factory.Begin("Test"))
            {
                Thread.Sleep(5);
                Assert.IsTrue(scope.Elapsed > TimeSpan.Zero);
            }
        }

        [TestMethod]
        public void OperationScope_RedactsPiiTags()
        {
            var factory = CreateFactory();
            var options = new OperationScopeOptions
            {
                PiiOptions = new EnrichmentOptions
                {
                    RedactPii = true,
                    RedactionStrategy = PiiRedactionStrategy.Mask
                }
            };

            using (var scope = factory.Begin("Test", options))
            {
                scope.WithTag("password", "secret");

                Assert.IsNotNull(scope.Activity);
                Assert.AreEqual("***", scope.Activity?.GetTagItem("password"));
            }
        }

        [TestMethod]
        public void OperationScope_EvaluatesLazyPropertiesOnDispose()
        {
            var factory = CreateFactory();
            var evaluations = 0;

            using (factory.Begin("Test")
                .WithProperty("lazy", () =>
                {
                    evaluations++;
                    return "value";
                }))
            {
                Assert.AreEqual(0, evaluations);
            }

            Assert.AreEqual(1, evaluations);
        }

        [TestMethod]
        public void OperationScope_FailSetsActivityStatus()
        {
            var factory = CreateFactory();
            var activity = default(Activity);

            using (var scope = factory.Begin("Test"))
            {
                activity = scope.Activity;
                scope.Fail(new InvalidOperationException("boom"));
            }

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityStatusCode.Error, activity?.Status);
        }

        [TestMethod]
        public void OperationScope_CreateChildUsesParentActivity()
        {
            var factory = CreateFactory();

            using (var parent = factory.Begin("Parent"))
            using (var child = parent.CreateChild("Child"))
            {
                Assert.IsNotNull(parent.Activity);
                Assert.IsNotNull(child.Activity);
                Assert.AreEqual(parent.Activity?.TraceId, child.Activity?.TraceId);
                Assert.AreEqual(parent.Activity?.SpanId, child.Activity?.ParentSpanId);
            }
        }

        [TestMethod]
        public void OperationScope_UsesCustomSerializerForComplexTypes()
        {
            var factory = CreateFactory();
            var options = new OperationScopeOptions
            {
                ComplexTypeSerializer = _ => "serialized"
            };

            using (var scope = factory.Begin("Test", options))
            {
                scope.WithTag("payload", new TestPayload { Value = "data" });

                Assert.AreEqual("serialized", scope.Activity?.GetTagItem("payload"));
            }
        }

        private static OperationScopeFactory CreateFactory()
        {
            var sourceName = "HVO.Enterprise.Telemetry.Tests." + Guid.NewGuid().ToString("N");
            return new OperationScopeFactory(sourceName, "1.0.0");
        }

        private sealed class TestPayload
        {
            public string? Value { get; set; }
        }
    }
}
