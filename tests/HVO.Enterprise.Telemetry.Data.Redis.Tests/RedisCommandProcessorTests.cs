using System;
using HVO.Enterprise.Telemetry.Data.Redis;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using HVO.Enterprise.Telemetry.Data.Redis.Profiling;

namespace HVO.Enterprise.Telemetry.Data.Redis.Tests
{
    [TestClass]
    public class RedisCommandProcessorTests
    {
        [TestMethod]
        public void Constructor_Default_DoesNotThrow()
        {
            // Act
            var processor = new RedisCommandProcessor();

            // Assert
            Assert.IsNotNull(processor);
        }

        [TestMethod]
        public void Constructor_WithOptions_DoesNotThrow()
        {
            // Arrange
            var options = new RedisTelemetryOptions { RecordCommands = false };

            // Act
            var processor = new RedisCommandProcessor(options);

            // Assert
            Assert.IsNotNull(processor);
        }

        [TestMethod]
        public void ProcessCommand_NullInput_DoesNotThrow()
        {
            // Arrange
            var processor = new RedisCommandProcessor();

            // Act — should not throw
            processor.ProcessCommand(null!);
        }
    }
}
