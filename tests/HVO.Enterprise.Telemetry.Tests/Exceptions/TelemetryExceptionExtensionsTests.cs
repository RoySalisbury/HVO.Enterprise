using System;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    [TestClass]
    public class TelemetryExceptionExtensionsTests
    {
        [TestMethod]
        public void RecordException_AddsToActivity()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestOp", ActivityKind.Internal);

            Assert.IsNotNull(activity);

            var exception = new InvalidOperationException("Test error");
            exception.RecordException();

            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.IsTrue(activity.Tags.Any(tag => tag.Key == "exception.type"));
            Assert.IsTrue(activity.Tags.Any(tag => tag.Key == "exception.fingerprint"));
        }
    }
}
