using System;
using HVO.Enterprise.Telemetry.Wcf.Server;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class WcfTelemetryBehaviorAttributeTests
    {
        [TestMethod]
        public void Attribute_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var attribute = new WcfTelemetryBehaviorAttribute();

            // Assert
            Assert.IsTrue(attribute.PropagateTraceContextInReply);
            Assert.IsTrue(attribute.CaptureFaultDetails);
        }

        [TestMethod]
        public void Attribute_PropertiesAreSettable()
        {
            // Arrange & Act
            var attribute = new WcfTelemetryBehaviorAttribute
            {
                PropagateTraceContextInReply = false,
                CaptureFaultDetails = false
            };

            // Assert
            Assert.IsFalse(attribute.PropagateTraceContextInReply);
            Assert.IsFalse(attribute.CaptureFaultDetails);
        }

        [TestMethod]
        public void Attribute_IsAttributeType()
        {
            // Assert
            Assert.IsTrue(typeof(Attribute).IsAssignableFrom(typeof(WcfTelemetryBehaviorAttribute)));
        }

        [TestMethod]
        public void Attribute_HasCorrectUsage()
        {
            // Arrange
            var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
                typeof(WcfTelemetryBehaviorAttribute),
                typeof(AttributeUsageAttribute));

            // Assert
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Class, usage!.ValidOn);
            Assert.IsFalse(usage.AllowMultiple);
            Assert.IsTrue(usage.Inherited);
        }
    }
}
