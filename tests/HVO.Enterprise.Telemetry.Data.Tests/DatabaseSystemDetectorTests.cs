using System;
using HVO.Enterprise.Telemetry.Data.Common;

namespace HVO.Enterprise.Telemetry.Data.Tests
{
    [TestClass]
    public class DatabaseSystemDetectorTests
    {
        // ──────────────────────────────────────────────────────────────
        // Provider-based detection
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void DetectSystem_SqlClientProvider_ReturnsMsSql()
        {
            var result = DatabaseSystemDetector.DetectSystem(null, "System.Data.SqlClient");
            Assert.AreEqual(DataActivityTags.SystemMsSql, result);
        }

        [TestMethod]
        public void DetectSystem_NpgsqlProvider_ReturnsPostgreSql()
        {
            var result = DatabaseSystemDetector.DetectSystem(null, "Npgsql");
            Assert.AreEqual(DataActivityTags.SystemPostgreSql, result);
        }

        [TestMethod]
        public void DetectSystem_MySqlProvider_ReturnsMySql()
        {
            var result = DatabaseSystemDetector.DetectSystem(null, "MySql.Data.MySqlClient");
            Assert.AreEqual(DataActivityTags.SystemMySql, result);
        }

        [TestMethod]
        public void DetectSystem_OracleProvider_ReturnsOracle()
        {
            var result = DatabaseSystemDetector.DetectSystem(null, "Oracle.ManagedDataAccess");
            Assert.AreEqual(DataActivityTags.SystemOracle, result);
        }

        [TestMethod]
        public void DetectSystem_SqliteProvider_ReturnsSqlite()
        {
            var result = DatabaseSystemDetector.DetectSystem(null, "Microsoft.Data.Sqlite");
            Assert.AreEqual(DataActivityTags.SystemSqlite, result);
        }

        // ──────────────────────────────────────────────────────────────
        // Connection string-based detection
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void DetectSystem_SqlServerConnectionString_ReturnsMsSql()
        {
            var connStr = "Data Source=localhost;Initial Catalog=mydb;Integrated Security=true";
            var result = DatabaseSystemDetector.DetectSystem(connStr);
            Assert.AreEqual(DataActivityTags.SystemMsSql, result);
        }

        [TestMethod]
        public void DetectSystem_PostgreSqlConnectionString_ReturnsPostgreSql()
        {
            var connStr = "Host=localhost;Database=mydb;Username=admin;Password=secret";
            var result = DatabaseSystemDetector.DetectSystem(connStr);
            Assert.AreEqual(DataActivityTags.SystemPostgreSql, result);
        }

        [TestMethod]
        public void DetectSystem_SqliteConnectionString_ReturnsSqlite()
        {
            var connStr = "Data Source=test.db";
            var result = DatabaseSystemDetector.DetectSystem(connStr);
            Assert.AreEqual(DataActivityTags.SystemSqlite, result);
        }

        [TestMethod]
        public void DetectSystem_MySqlConnectionString_ReturnsMySql()
        {
            var connStr = "Server=localhost;Database=mydb;Uid=root;Pwd=secret";
            var result = DatabaseSystemDetector.DetectSystem(connStr);
            Assert.AreEqual(DataActivityTags.SystemMySql, result);
        }

        // ──────────────────────────────────────────────────────────────
        // Edge cases
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void DetectSystem_NullInputs_ReturnsOther()
        {
            var result = DatabaseSystemDetector.DetectSystem(null, null);
            Assert.AreEqual(DataActivityTags.SystemOther, result);
        }

        [TestMethod]
        public void DetectSystem_EmptyInputs_ReturnsOther()
        {
            var result = DatabaseSystemDetector.DetectSystem(string.Empty, string.Empty);
            Assert.AreEqual(DataActivityTags.SystemOther, result);
        }

        [TestMethod]
        public void DetectSystem_UnknownConnectionString_ReturnsOther()
        {
            var result = DatabaseSystemDetector.DetectSystem("SomeRandomString=value");
            Assert.AreEqual(DataActivityTags.SystemOther, result);
        }

        [TestMethod]
        public void DetectSystem_ProviderTakesPrecedence_WhenBothProvided()
        {
            // Provider says SQL Server, connection string looks like PostgreSQL
            var connStr = "Host=localhost;Database=mydb";
            var result = DatabaseSystemDetector.DetectSystem(connStr, "System.Data.SqlClient");
            Assert.AreEqual(DataActivityTags.SystemMsSql, result);
        }
    }
}
