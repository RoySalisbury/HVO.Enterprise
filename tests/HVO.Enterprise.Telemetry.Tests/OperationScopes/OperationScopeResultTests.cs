using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    [TestClass]
    public class OperationScopeResultTests
    {
        [TestMethod]
        public void OperationScope_WithResultAddsTagOnDispose()
        {
            var factory = CreateFactory();
            var activity = default(Activity);

            using (var scope = factory.Begin("Test"))
            {
                scope.WithResult("ok");
                activity = scope.Activity;
            }

            Assert.IsNotNull(activity);
            Assert.AreEqual("ok", activity?.GetTagItem("operation.result"));
        }

        private static OperationScopeFactory CreateFactory()
        {
            var sourceName = "HVO.Enterprise.Telemetry.Tests." + Guid.NewGuid().ToString("N");
            return new OperationScopeFactory(sourceName, "1.0.0");
        }
    }
}
