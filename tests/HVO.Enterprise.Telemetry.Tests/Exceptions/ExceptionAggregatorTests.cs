using System;
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
            var clock = new TestClock(DateTimeOffset.UtcNow);
            var aggregator = new ExceptionAggregator(clock.GetNow);
            var exception = new InvalidOperationException("Test");

            var group = aggregator.RecordException(exception);
            clock.Advance(TimeSpan.FromSeconds(1));

            aggregator.RecordException(exception);
            aggregator.RecordException(exception);

            var errorRate = group.GetErrorRate();

            const double MinExpectedErrorRate = 60;
            const double MaxExpectedErrorRate = 240;

            Assert.IsTrue(
                errorRate >= MinExpectedErrorRate && errorRate <= MaxExpectedErrorRate,
                "Error rate should be within expected range.");
        }

        [TestMethod]
        public void GetGroup_ReturnsNullForEmptyFingerprint()
        {
            var aggregator = new ExceptionAggregator();

            Assert.IsNull(aggregator.GetGroup(string.Empty));
            Assert.IsNull(aggregator.GetGroup(null!));
        }

        [TestMethod]
        public void GetGlobalErrorRatePercentage_ReturnsZeroWhenNoOperations()
        {
            var aggregator = new ExceptionAggregator();

            var rate = aggregator.GetGlobalErrorRatePercentage(0);

            Assert.AreEqual(0, rate);
        }

        [TestMethod]
        public void Cleanup_RemovesExpiredGroups()
        {
            var clock = new TestClock(DateTimeOffset.UtcNow);
            var aggregator = new ExceptionAggregator(clock.GetNow, TimeSpan.FromSeconds(1));

            aggregator.RecordException(new InvalidOperationException("Test"));
            clock.Advance(TimeSpan.FromSeconds(2));

            var groups = aggregator.GetGroups();

            Assert.AreEqual(0, groups.Count);
        }

        private sealed class TestClock
        {
            private DateTimeOffset _current;

            public TestClock(DateTimeOffset start)
            {
                _current = start;
            }

            public DateTimeOffset GetNow()
            {
                return _current;
            }

            public void Advance(TimeSpan delta)
            {
                _current = _current.Add(delta);
            }
        }
    }
}
