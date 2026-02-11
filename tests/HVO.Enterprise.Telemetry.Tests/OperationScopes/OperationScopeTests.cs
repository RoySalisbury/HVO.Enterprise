using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        [TestMethod]
        public void Fail_RecordExceptionThenFail_SameException_TagsAppearOnce()
        {
            var factory = CreateFactory();
            Activity? activity = null;

            using (var scope = factory.Begin("Test"))
            {
                activity = scope.Activity;
                var ex = new InvalidOperationException("boom");

                // Simulate the pattern that caused duplicate tags:
                // RecordException delegates to Fail, then user code calls Fail again.
                scope.RecordException(ex);
                scope.Fail(ex);
            }

            Assert.IsNotNull(activity);

            var exceptionTypeTags = activity!.TagObjects
                .Where(t => t.Key == "exception.type")
                .ToList();

            var fingerprintTags = activity.TagObjects
                .Where(t => t.Key == "exception.fingerprint")
                .ToList();

            Assert.AreEqual(1, exceptionTypeTags.Count,
                "exception.type tag should appear exactly once");
            Assert.AreEqual(1, fingerprintTags.Count,
                "exception.fingerprint tag should appear exactly once");
        }

        [TestMethod]
        public void Fail_CalledMultipleTimesWithSameException_TagsAppearOnce()
        {
            var factory = CreateFactory();
            Activity? activity = null;

            using (var scope = factory.Begin("Test"))
            {
                activity = scope.Activity;
                var ex = new InvalidOperationException("boom");

                scope.Fail(ex);
                scope.Fail(ex);
                scope.Fail(ex);
            }

            Assert.IsNotNull(activity);

            var exceptionTypeTags = activity!.TagObjects
                .Where(t => t.Key == "exception.type")
                .ToList();

            Assert.AreEqual(1, exceptionTypeTags.Count,
                "exception.type tag should appear exactly once after multiple Fail() calls");
        }

        [TestMethod]
        public void Fail_CalledWithDifferentExceptions_RecordsBoth()
        {
            var factory = CreateFactory();
            Activity? activity = null;

            using (var scope = factory.Begin("Test"))
            {
                activity = scope.Activity;

                scope.Fail(new InvalidOperationException("first"));
                scope.Fail(new ArgumentException("second"));
            }

            Assert.IsNotNull(activity);

            var exceptionTypeTags = activity!.TagObjects
                .Where(t => t.Key == "exception.type")
                .ToList();

            // Two different exceptions should each record their own tags.
            Assert.AreEqual(2, exceptionTypeTags.Count,
                "Each distinct exception should record its own exception.type tag");
        }

        [TestMethod]
        public void Fail_ConcurrentCallsWithSameException_TagsAppearOnce()
        {
            var factory = CreateFactory();
            Activity? activity = null;
            var ex = new InvalidOperationException("concurrent-boom");

            using (var scope = factory.Begin("Test"))
            {
                activity = scope.Activity;

                // Hammer Fail() from multiple threads with the same exception instance.
                var tasks = Enumerable.Range(0, 20).Select(_ =>
                    Task.Run(() => scope.Fail(ex))).ToArray();

                Task.WaitAll(tasks);
            }

            Assert.IsNotNull(activity);

            var exceptionTypeTags = activity!.TagObjects
                .Where(t => t.Key == "exception.type")
                .ToList();

            Assert.AreEqual(1, exceptionTypeTags.Count,
                "Concurrent Fail() calls with the same exception should record tags exactly once");
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
