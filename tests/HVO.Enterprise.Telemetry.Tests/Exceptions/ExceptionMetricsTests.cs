using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using HVO.Enterprise.Telemetry.Exceptions;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    [TestClass]
    public class ExceptionMetricsTests
    {
        [TestMethod]
        public void RecordException_UsesTypeTagOnly()
        {
            KeyValuePair<string, object?>[]? lastTags = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName &&
                    instrument.Name == "exceptions.total")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                lastTags = tags.ToArray();
            });

            listener.Start();

            ExceptionMetrics.RecordException("System.InvalidOperationException");

            Assert.IsNotNull(lastTags);
            Assert.IsTrue(lastTags!.Any(tag => tag.Key == "type"));
            Assert.IsFalse(lastTags!.Any(tag => tag.Key == "fingerprint"));
        }
    }
}
