using System;
using HVO.Enterprise.Telemetry.Data.AdoNet;
using HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Tests
{
    [TestClass]
    public class InstrumentedDbCommandTests
    {
        [DataTestMethod]
        [DataRow(null, "EXECUTE")]
        [DataRow("", "EXECUTE")]
        [DataRow("   ", "EXECUTE")]
        [DataRow("INSERT INTO Users VALUES (@p0)", "INSERT")]
        [DataRow("UPDATE Users SET Name = @p0", "UPDATE")]
        [DataRow("DELETE FROM Users WHERE Id = @p0", "DELETE")]
        [DataRow("SELECT * FROM Users", "SELECT")]
        [DataRow("EXEC sp_GetUsers", "EXECUTE")]
        [DataRow("EXECUTE sp_GetUsers", "EXECUTE")]
        [DataRow("  SELECT * FROM Users", "SELECT")]
        public void DetectOperation_VariousCommands_ReturnsExpected(string? commandText, string expected)
        {
            // Act
            var result = InstrumentedDbCommand.DetectOperation(commandText);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullCommand_ThrowsArgumentNullException()
        {
            new InstrumentedDbCommand(null!);
        }

        [TestMethod]
        public void AdoNetActivitySource_HasExpectedName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.AdoNet", AdoNetActivitySource.Name);
        }

        [TestMethod]
        public void AdoNetActivitySource_SourceNotNull()
        {
            Assert.IsNotNull(AdoNetActivitySource.Source);
        }
    }
}
