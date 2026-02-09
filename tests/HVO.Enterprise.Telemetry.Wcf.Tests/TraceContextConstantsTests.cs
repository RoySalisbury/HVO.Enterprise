using System;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class TraceContextConstantsTests
    {
        [TestMethod]
        public void TraceParentHeaderName_HasCorrectValue()
        {
            Assert.AreEqual("traceparent", TraceContextConstants.TraceParentHeaderName);
        }

        [TestMethod]
        public void TraceStateHeaderName_HasCorrectValue()
        {
            Assert.AreEqual("tracestate", TraceContextConstants.TraceStateHeaderName);
        }

        [TestMethod]
        public void SoapNamespace_HasCorrectValue()
        {
            Assert.AreEqual("http://hvo.enterprise/telemetry", TraceContextConstants.SoapNamespace);
        }

        [TestMethod]
        public void ActivitySourceName_HasCorrectValue()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Wcf", TraceContextConstants.ActivitySourceName);
        }

        [TestMethod]
        public void TraceParentVersion_IsZeroZero()
        {
            Assert.AreEqual("00", TraceContextConstants.TraceParentVersion);
        }
    }
}
