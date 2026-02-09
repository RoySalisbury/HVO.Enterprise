using System;
using System.Reflection;
using HVO.Enterprise.Telemetry.IIS;

namespace HVO.Enterprise.Telemetry.IIS.Tests
{
    /// <summary>
    /// Tests for <see cref="IisRegisteredObjectProxy"/> DispatchProxy behavior.
    /// </summary>
    [TestClass]
    public sealed class IisRegisteredObjectProxyTests
    {
        [TestMethod]
        public void Proxy_HasCorrectBaseType()
        {
            // IisRegisteredObjectProxy inherits from DispatchProxy
            Assert.IsTrue(typeof(DispatchProxy).IsAssignableFrom(typeof(IisRegisteredObjectProxy)));
        }

        [TestMethod]
        public void Factory_IsSystemWebAvailable_ReturnsFalse_OnNonFramework()
        {
            // On .NET 8 (our test runtime), System.Web is not available
            Assert.IsFalse(IisRegisteredObjectFactory.IsSystemWebAvailable);
        }

        [TestMethod]
        public void Factory_TryCreate_ReturnsFalse_WhenSystemWebUnavailable()
        {
            // Arrange
            var shutdownHandler = new IisShutdownHandler(null);

            // Act
            var result = IisRegisteredObjectFactory.TryCreate(
                shutdownHandler,
                TimeSpan.FromSeconds(25),
                out var proxy);

            // Assert - should fail gracefully since System.Web is not available
            Assert.IsFalse(result);
            Assert.IsNull(proxy);
        }

        [TestMethod]
        public void Factory_TryRegister_ReturnsFalse_WhenSystemWebUnavailable()
        {
            // Act & Assert
            Assert.IsFalse(IisRegisteredObjectFactory.TryRegister(new object()));
        }

        [TestMethod]
        public void Factory_TryUnregister_ReturnsFalse_WhenSystemWebUnavailable()
        {
            // Act & Assert
            Assert.IsFalse(IisRegisteredObjectFactory.TryUnregister(new object()));
        }

        [TestMethod]
        public void Proxy_CanBeCreatedDirectly_ForPropertyTesting()
        {
            // DispatchProxy.Create creates a derived type, but we can test property setting
            // by creating an instance via a different interface
            // For testing, we verify the proxy class itself has the expected properties

            var proxyType = typeof(IisRegisteredObjectProxy);

            // Verify internal properties exist
            var shutdownHandlerProp = proxyType.GetProperty("ShutdownHandler",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(shutdownHandlerProp, "ShutdownHandler property should exist");

            var shutdownTimeoutProp = proxyType.GetProperty("ShutdownTimeout",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(shutdownTimeoutProp, "ShutdownTimeout property should exist");

            var unregisterSelfProp = proxyType.GetProperty("UnregisterSelf",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(unregisterSelfProp, "UnregisterSelf property should exist");
        }

        [TestMethod]
        public void Proxy_InvokeMethod_IsOverridden()
        {
            // Verify the Invoke method is overridden from DispatchProxy
            var invokeMethod = typeof(IisRegisteredObjectProxy).GetMethod("Invoke",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(MethodInfo), typeof(object?[]) },
                null);

            Assert.IsNotNull(invokeMethod, "Invoke method should be overridden");
            Assert.AreEqual(typeof(IisRegisteredObjectProxy), invokeMethod!.DeclaringType,
                "Invoke should be declared on IisRegisteredObjectProxy, not the base DispatchProxy");
        }
    }
}
