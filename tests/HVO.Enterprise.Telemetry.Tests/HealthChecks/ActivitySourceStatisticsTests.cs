using HVO.Enterprise.Telemetry.HealthChecks;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class ActivitySourceStatisticsTests
    {
        [TestMethod]
        public void DefaultValues_AreZero()
        {
            var stats = new ActivitySourceStatistics();

            Assert.AreEqual(string.Empty, stats.SourceName);
            Assert.AreEqual(0, stats.ActivitiesCreated);
            Assert.AreEqual(0, stats.ActivitiesCompleted);
            Assert.AreEqual(0.0, stats.AverageDurationMs);
        }

        [TestMethod]
        public void Properties_CanBeSet()
        {
            var stats = new ActivitySourceStatistics
            {
                SourceName = "MySource",
                ActivitiesCreated = 100,
                ActivitiesCompleted = 95,
                AverageDurationMs = 42.5
            };

            Assert.AreEqual("MySource", stats.SourceName);
            Assert.AreEqual(100, stats.ActivitiesCreated);
            Assert.AreEqual(95, stats.ActivitiesCompleted);
            Assert.AreEqual(42.5, stats.AverageDurationMs);
        }
    }
}
