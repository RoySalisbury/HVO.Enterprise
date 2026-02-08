using System;
using System.Threading;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    [TestClass]
    public class ExceptionAggregatorTests
    {
        [TestMethod]
        public void ExceptionAggregator_GroupsSimilarExceptions()
        {
            var aggregator = new ExceptionAggregator();

            var ex1 = new InvalidOperationException("User 123 not found");
            var ex2 = new InvalidOperationException("User 456 not found");

            var group1 = aggregator.RecordException(ex1);
            var group2 = aggregator.RecordException(ex2);

            Assert.AreEqual(group1.Fingerprint, group2.Fingerprint);
            Assert.AreEqual(2, group1.Count);
        }

        [TestMethod]
        public void ExceptionGroup_TracksOccurrences()
        {
            var aggregator = new ExceptionAggregator();
            var exception = new InvalidOperationException("Test error");

            var group = aggregator.RecordException(exception);
            Assert.AreEqual(1, group.Count);

            aggregator.RecordException(exception);
            Assert.AreEqual(2, group.Count);

            aggregator.RecordException(exception);
            Assert.AreEqual(3, group.Count);
        }

        [TestMethod]
        public void ExceptionGroup_CalculatesErrorRate()
        {
            var aggregator = new ExceptionAggregator();
            var exception = new InvalidOperationException("Test");

            var group = aggregator.RecordException(exception);
            Thread.Sleep(1000);

            aggregator.RecordException(exception);
            aggregator.RecordException(exception);

            var errorRate = group.GetErrorRate();

            Assert.IsTrue(errorRate >= 60 && errorRate <= 240, "Error rate should be within expected range.");
        }
    }
}
