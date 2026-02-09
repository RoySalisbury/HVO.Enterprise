using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Instrumentation;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Tests
{
    [TestClass]
    public class RabbitMqHeaderPropagatorTests
    {
        [TestMethod]
        public void Inject_NullHeaders_CreatesNewDictionary()
        {
            // Act
            var result = RabbitMqHeaderPropagator.Inject(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object>));
        }

        [TestMethod]
        public void Inject_ExistingHeaders_AddsToExisting()
        {
            // Arrange
            var headers = new Dictionary<string, object> { ["custom-header"] = "value" };

            // Act
            var result = RabbitMqHeaderPropagator.Inject(headers);

            // Assert
            Assert.AreSame(headers, result);
            Assert.IsTrue(result.ContainsKey("custom-header"));
        }

        [TestMethod]
        public void Inject_WithActivity_InjectsTraceparent()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test.rabbitmq.inject");
            using var activity = source.StartActivity("TestPublish")!;

            var headers = new Dictionary<string, object>();

            // Act
            var result = RabbitMqHeaderPropagator.Inject(headers, activity);

            // Assert
            Assert.IsTrue(result.ContainsKey(RabbitMqHeaderPropagator.TraceparentHeader));
            var traceparent = Encoding.UTF8.GetString((byte[])result[RabbitMqHeaderPropagator.TraceparentHeader]);
            Assert.IsTrue(traceparent.StartsWith("00-"));
            Assert.IsTrue(traceparent.Contains(activity.TraceId.ToHexString()));
            Assert.IsTrue(traceparent.Contains(activity.SpanId.ToHexString()));
        }

        [TestMethod]
        public void Inject_WithActivityTraceState_InjectsTracestate()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test.rabbitmq.inject.ts");
            using var activity = source.StartActivity("TestPublish")!;
            activity.TraceStateString = "congo=t61rcWkgMzE";

            var headers = new Dictionary<string, object>();

            // Act
            var result = RabbitMqHeaderPropagator.Inject(headers, activity);

            // Assert
            Assert.IsTrue(result.ContainsKey(RabbitMqHeaderPropagator.TracestateHeader));
            var tracestate = Encoding.UTF8.GetString((byte[])result[RabbitMqHeaderPropagator.TracestateHeader]);
            Assert.AreEqual("congo=t61rcWkgMzE", tracestate);
        }

        [TestMethod]
        public void Inject_NoCurrentActivity_DoesNotAddHeaders()
        {
            // Arrange — ensure no Activity.Current
            Activity.Current = null;
            var headers = new Dictionary<string, object>();

            // Act
            var result = RabbitMqHeaderPropagator.Inject(headers, null);

            // Assert
            Assert.IsFalse(result.ContainsKey(RabbitMqHeaderPropagator.TraceparentHeader));
        }

        [TestMethod]
        public void Extract_NullHeaders_ReturnsDefault()
        {
            // Act
            var context = RabbitMqHeaderPropagator.Extract(null);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void Extract_EmptyHeaders_ReturnsDefault()
        {
            // Arrange
            var headers = new Dictionary<string, object>();

            // Act
            var context = RabbitMqHeaderPropagator.Extract(headers);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void Extract_ValidTraceparentBytes_ReturnsContext()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
            var headers = new Dictionary<string, object>
            {
                [RabbitMqHeaderPropagator.TraceparentHeader] = Encoding.UTF8.GetBytes(traceparent)
            };

            // Act
            var context = RabbitMqHeaderPropagator.Extract(headers);

            // Assert
            Assert.AreNotEqual(default(ActivityContext), context);
            Assert.AreEqual("0af7651916cd43dd8448eb211c80319c", context.TraceId.ToHexString());
            Assert.AreEqual("b7ad6b7169203331", context.SpanId.ToHexString());
            Assert.AreEqual(ActivityTraceFlags.Recorded, context.TraceFlags);
        }

        [TestMethod]
        public void Extract_ValidTraceparentString_ReturnsContext()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";
            var headers = new Dictionary<string, object>
            {
                [RabbitMqHeaderPropagator.TraceparentHeader] = traceparent
            };

            // Act
            var context = RabbitMqHeaderPropagator.Extract(headers);

            // Assert
            Assert.AreNotEqual(default(ActivityContext), context);
            Assert.AreEqual(ActivityTraceFlags.None, context.TraceFlags);
        }

        [TestMethod]
        public void Extract_InvalidTraceparent_ReturnsDefault()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                [RabbitMqHeaderPropagator.TraceparentHeader] = Encoding.UTF8.GetBytes("invalid-data")
            };

            // Act
            var context = RabbitMqHeaderPropagator.Extract(headers);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void Extract_WithTracestate_IncludesTracestate()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
            var headers = new Dictionary<string, object>
            {
                [RabbitMqHeaderPropagator.TraceparentHeader] = Encoding.UTF8.GetBytes(traceparent),
                [RabbitMqHeaderPropagator.TracestateHeader] = Encoding.UTF8.GetBytes("congo=t61rcWkgMzE")
            };

            // Act
            var context = RabbitMqHeaderPropagator.Extract(headers);

            // Assert
            Assert.AreEqual("congo=t61rcWkgMzE", context.TraceState);
        }

        [TestMethod]
        public void RoundTrip_InjectThenExtract_PreservesContext()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test.rabbitmq.roundtrip");
            using var activity = source.StartActivity("TestPublish")!;

            // Act — inject
            var headers = RabbitMqHeaderPropagator.Inject(null, activity);

            // Act — extract
            var context = RabbitMqHeaderPropagator.Extract(headers);

            // Assert
            Assert.AreEqual(activity.TraceId.ToHexString(), context.TraceId.ToHexString());
            Assert.AreEqual(activity.SpanId.ToHexString(), context.SpanId.ToHexString());
        }

        [TestMethod]
        public void TraceparentHeader_HasExpectedValue()
        {
            Assert.AreEqual("traceparent", RabbitMqHeaderPropagator.TraceparentHeader);
        }

        [TestMethod]
        public void TracestateHeader_HasExpectedValue()
        {
            Assert.AreEqual("tracestate", RabbitMqHeaderPropagator.TracestateHeader);
        }

        [TestMethod]
        public void ParseTraceparent_WrongVersion_ReturnsDefault()
        {
            // Act
            var context = RabbitMqHeaderPropagator.ParseTraceparent(
                "01-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", null);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ParseTraceparent_TooFewParts_ReturnsDefault()
        {
            // Act
            var context = RabbitMqHeaderPropagator.ParseTraceparent(
                "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331", null);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ParseTraceparent_WrongTraceIdLength_ReturnsDefault()
        {
            // Act
            var context = RabbitMqHeaderPropagator.ParseTraceparent(
                "00-0af765-b7ad6b7169203331-01", null);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ParseTraceparent_WrongSpanIdLength_ReturnsDefault()
        {
            // Act
            var context = RabbitMqHeaderPropagator.ParseTraceparent(
                "00-0af7651916cd43dd8448eb211c80319c-b7ad-01", null);

            // Assert
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ParseTraceparent_NullInput_ReturnsDefault()
        {
            var context = RabbitMqHeaderPropagator.ParseTraceparent(null, null);
            Assert.AreEqual(default(ActivityContext), context);
        }

        [TestMethod]
        public void ParseTraceparent_EmptyInput_ReturnsDefault()
        {
            var context = RabbitMqHeaderPropagator.ParseTraceparent(string.Empty, null);
            Assert.AreEqual(default(ActivityContext), context);
        }
    }
}
