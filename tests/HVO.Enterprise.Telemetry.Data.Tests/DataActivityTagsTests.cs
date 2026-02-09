using System;
using HVO.Enterprise.Telemetry.Data.Common;

namespace HVO.Enterprise.Telemetry.Data.Tests
{
    [TestClass]
    public class DataActivityTagsTests
    {
        [TestMethod]
        public void DbSystem_HasExpectedValue()
        {
            Assert.AreEqual("db.system", DataActivityTags.DbSystem);
        }

        [TestMethod]
        public void DbName_HasExpectedValue()
        {
            Assert.AreEqual("db.name", DataActivityTags.DbName);
        }

        [TestMethod]
        public void DbStatement_HasExpectedValue()
        {
            Assert.AreEqual("db.statement", DataActivityTags.DbStatement);
        }

        [TestMethod]
        public void DbOperation_HasExpectedValue()
        {
            Assert.AreEqual("db.operation", DataActivityTags.DbOperation);
        }

        [TestMethod]
        public void ServerAddress_HasExpectedValue()
        {
            Assert.AreEqual("server.address", DataActivityTags.ServerAddress);
        }

        [TestMethod]
        public void ServerPort_HasExpectedValue()
        {
            Assert.AreEqual("server.port", DataActivityTags.ServerPort);
        }

        [TestMethod]
        public void DbRowsAffected_HasExpectedValue()
        {
            Assert.AreEqual("db.rows_affected", DataActivityTags.DbRowsAffected);
        }

        [TestMethod]
        public void MessagingSystem_HasExpectedValue()
        {
            Assert.AreEqual("messaging.system", DataActivityTags.MessagingSystem);
        }

        [TestMethod]
        public void MessagingDestinationName_HasExpectedValue()
        {
            Assert.AreEqual("messaging.destination.name", DataActivityTags.MessagingDestinationName);
        }

        [TestMethod]
        public void MessagingOperation_HasExpectedValue()
        {
            Assert.AreEqual("messaging.operation", DataActivityTags.MessagingOperation);
        }

        [TestMethod]
        public void MessagingRabbitMqRoutingKey_HasExpectedValue()
        {
            Assert.AreEqual("messaging.rabbitmq.destination.routing_key", DataActivityTags.MessagingRabbitMqRoutingKey);
        }

        [TestMethod]
        public void MessagingMessageBodySize_HasExpectedValue()
        {
            Assert.AreEqual("messaging.message.body.size", DataActivityTags.MessagingMessageBodySize);
        }

        [TestMethod]
        public void SystemMsSql_HasExpectedValue()
        {
            Assert.AreEqual("mssql", DataActivityTags.SystemMsSql);
        }

        [TestMethod]
        public void SystemPostgreSql_HasExpectedValue()
        {
            Assert.AreEqual("postgresql", DataActivityTags.SystemPostgreSql);
        }

        [TestMethod]
        public void SystemRedis_HasExpectedValue()
        {
            Assert.AreEqual("redis", DataActivityTags.SystemRedis);
        }

        [TestMethod]
        public void SystemRabbitMq_HasExpectedValue()
        {
            Assert.AreEqual("rabbitmq", DataActivityTags.SystemRabbitMq);
        }

        [TestMethod]
        public void SystemSqlite_HasExpectedValue()
        {
            Assert.AreEqual("sqlite", DataActivityTags.SystemSqlite);
        }

        [TestMethod]
        public void SystemOther_HasExpectedValue()
        {
            Assert.AreEqual("other_sql", DataActivityTags.SystemOther);
        }
    }
}
