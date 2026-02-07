using System;
using Xunit;

namespace HVO.Enterprise.Telemetry.Tests
{
    /// <summary>
    /// Tests to verify that HVO.Enterprise.Telemetry can be referenced and used from .NET 8 projects.
    /// </summary>
    public class CompatibilityTests
    {
        [Fact]
        public void ProjectReference_CanBeResolved()
        {
            // This test verifies that the project can be referenced successfully
            // If we can run this test, the project reference is working
            Assert.True(true);
        }

        [Fact]
        public void AssemblyVersion_IsCorrect()
        {
            // Verify the assembly can be loaded and has the expected version
            var assembly = typeof(HVO.Enterprise.Telemetry.Abstractions.ITelemetryService).Assembly;
            Assert.NotNull(assembly);
            Assert.Equal("1.0.0.0", assembly.GetName().Version?.ToString());
        }
    }
}
