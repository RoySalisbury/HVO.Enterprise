using System;
using System.Diagnostics;
using System.Linq;
using Grpc.Core;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Grpc;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class GrpcMetadataHelperTests
    {
        [TestMethod]
        public void GetMetadataValue_ExistingKey_ReturnsValue()
        {
            var metadata = new Metadata
            {
                { "x-correlation-id", "test-123" }
            };

            var result = GrpcMetadataHelper.GetMetadataValue(metadata, "x-correlation-id");

            Assert.AreEqual("test-123", result);
        }

        [TestMethod]
        public void GetMetadataValue_CaseInsensitive_ReturnsValue()
        {
            var metadata = new Metadata
            {
                { "x-correlation-id", "test-123" }
            };

            var result = GrpcMetadataHelper.GetMetadataValue(metadata, "X-Correlation-ID");

            Assert.AreEqual("test-123", result);
        }

        [TestMethod]
        public void GetMetadataValue_MissingKey_ReturnsNull()
        {
            var metadata = new Metadata();

            var result = GrpcMetadataHelper.GetMetadataValue(metadata, "x-correlation-id");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetMetadataValue_NullHeaders_ReturnsNull()
        {
            var result = GrpcMetadataHelper.GetMetadataValue(null, "x-correlation-id");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExtractTraceContext_ValidTraceparent_ReturnsContext()
        {
            var metadata = new Metadata
            {
                { "traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" }
            };

            var context = GrpcMetadataHelper.ExtractTraceContext(metadata);

            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", context.TraceId.ToString());
            Assert.AreEqual("00f067aa0ba902b7", context.SpanId.ToString());
            Assert.AreEqual(ActivityTraceFlags.Recorded, context.TraceFlags);
        }

        [TestMethod]
        public void ExtractTraceContext_WithTracestate_IncludesIt()
        {
            var metadata = new Metadata
            {
                { "traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" },
                { "tracestate", "congo=t61rcWkgMzE" }
            };

            var context = GrpcMetadataHelper.ExtractTraceContext(metadata);

            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", context.TraceId.ToString());
            Assert.AreEqual("congo=t61rcWkgMzE", context.TraceState);
        }

        [TestMethod]
        public void ExtractTraceContext_NoTraceparent_ReturnsDefault()
        {
            var metadata = new Metadata();

            var context = GrpcMetadataHelper.ExtractTraceContext(metadata);

            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ExtractTraceContext_InvalidTraceparent_ReturnsDefault()
        {
            var metadata = new Metadata
            {
                { "traceparent", "invalid-trace-parent" }
            };

            var context = GrpcMetadataHelper.ExtractTraceContext(metadata);

            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ExtractTraceContext_NullHeaders_ReturnsDefault()
        {
            var context = GrpcMetadataHelper.ExtractTraceContext(null);

            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void InjectTraceContext_WithActivity_AddsTraceparent()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test");
            using var activity = source.StartActivity("test-op", ActivityKind.Client);
            Assert.IsNotNull(activity);

            var metadata = new Metadata();
            GrpcMetadataHelper.InjectTraceContext(activity, metadata);

            var traceparent = metadata.FirstOrDefault(e => e.Key == "traceparent");
            Assert.IsNotNull(traceparent);
            Assert.IsTrue(traceparent.Value.StartsWith("00-"));
            Assert.IsTrue(traceparent.Value.Contains(activity.TraceId.ToString()));
            Assert.IsTrue(traceparent.Value.Contains(activity.SpanId.ToString()));
        }

        [TestMethod]
        public void InjectTraceContext_NullActivity_DoesNothing()
        {
            var metadata = new Metadata();

            GrpcMetadataHelper.InjectTraceContext(null, metadata);

            Assert.AreEqual(0, metadata.Count);
        }

        [TestMethod]
        public void InjectCorrelation_WithExplicitCorrelationId_AddsHeader()
        {
            using var scope = CorrelationContext.BeginScope("test-correlation-123");

            var metadata = new Metadata();
            GrpcMetadataHelper.InjectCorrelation(metadata, "x-correlation-id");

            var entry = metadata.FirstOrDefault(e => e.Key == "x-correlation-id");
            Assert.IsNotNull(entry);
            Assert.AreEqual("test-correlation-123", entry.Value);
        }

        [TestMethod]
        public void InjectCorrelation_NoExplicitId_UsesCurrentFallback()
        {
            // Clear any previous state
            CorrelationContext.Current = null!;

            var metadata = new Metadata();
            GrpcMetadataHelper.InjectCorrelation(metadata, "x-correlation-id");

            // CorrelationContext.Current auto-generates, so there should be a value
            var entry = metadata.FirstOrDefault(e => e.Key == "x-correlation-id");
            Assert.IsNotNull(entry);
            Assert.IsFalse(string.IsNullOrEmpty(entry.Value));
        }

        [TestMethod]
        public void InjectCorrelation_CustomHeaderName_UsesCustomName()
        {
            using var scope = CorrelationContext.BeginScope("custom-id-456");

            var metadata = new Metadata();
            GrpcMetadataHelper.InjectCorrelation(metadata, "x-request-id");

            var entry = metadata.FirstOrDefault(e => e.Key == "x-request-id");
            Assert.IsNotNull(entry);
            Assert.AreEqual("custom-id-456", entry.Value);
        }
    }
}
