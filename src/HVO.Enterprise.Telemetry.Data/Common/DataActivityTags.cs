namespace HVO.Enterprise.Telemetry.Data.Common
{
    /// <summary>
    /// OpenTelemetry semantic conventions for database and messaging operations.
    /// See: https://opentelemetry.io/docs/specs/semconv/database/
    /// See: https://opentelemetry.io/docs/specs/semconv/messaging/
    /// </summary>
    public static class DataActivityTags
    {
        // ──────────────────────────────────────────────────────────────
        // Database semantic conventions
        // ──────────────────────────────────────────────────────────────

        /// <summary>Database system identifier (e.g., "mssql", "postgresql", "redis").</summary>
        public const string DbSystem = "db.system";

        /// <summary>Database name being accessed.</summary>
        public const string DbName = "db.name";

        /// <summary>Database statement (SQL query, Redis command, etc.).</summary>
        public const string DbStatement = "db.statement";

        /// <summary>Database operation type (SELECT, INSERT, GET, SET, etc.).</summary>
        public const string DbOperation = "db.operation";

        /// <summary>Server hostname or IP address.</summary>
        public const string ServerAddress = "server.address";

        /// <summary>Server port number.</summary>
        public const string ServerPort = "server.port";

        /// <summary>SQL table name.</summary>
        public const string DbSqlTable = "db.sql.table";

        /// <summary>Number of rows affected by the operation.</summary>
        public const string DbRowsAffected = "db.rows_affected";

        /// <summary>Duration of the database operation in milliseconds.</summary>
        public const string DbOperationDuration = "db.operation.duration";

        /// <summary>Redis database index.</summary>
        public const string DbRedisDatabaseIndex = "db.redis.database_index";

        // ──────────────────────────────────────────────────────────────
        // Messaging semantic conventions (RabbitMQ, etc.)
        // ──────────────────────────────────────────────────────────────

        /// <summary>Messaging system identifier (e.g., "rabbitmq").</summary>
        public const string MessagingSystem = "messaging.system";

        /// <summary>Messaging destination name (queue/exchange).</summary>
        public const string MessagingDestinationName = "messaging.destination.name";

        /// <summary>Messaging operation type (publish, receive, process).</summary>
        public const string MessagingOperation = "messaging.operation";

        /// <summary>Message ID.</summary>
        public const string MessagingMessageId = "messaging.message.id";

        /// <summary>Message conversation/correlation ID.</summary>
        public const string MessagingMessageConversationId = "messaging.message.conversation_id";

        /// <summary>RabbitMQ routing key.</summary>
        public const string MessagingRabbitMqRoutingKey = "messaging.rabbitmq.destination.routing_key";

        /// <summary>Message body size in bytes.</summary>
        public const string MessagingMessageBodySize = "messaging.message.body.size";

        // ──────────────────────────────────────────────────────────────
        // Database system values
        // ──────────────────────────────────────────────────────────────

        /// <summary>Microsoft SQL Server.</summary>
        public const string SystemMsSql = "mssql";

        /// <summary>PostgreSQL.</summary>
        public const string SystemPostgreSql = "postgresql";

        /// <summary>MySQL.</summary>
        public const string SystemMySql = "mysql";

        /// <summary>Oracle Database.</summary>
        public const string SystemOracle = "oracle";

        /// <summary>Redis.</summary>
        public const string SystemRedis = "redis";

        /// <summary>SQLite.</summary>
        public const string SystemSqlite = "sqlite";

        /// <summary>Unknown or other SQL system.</summary>
        public const string SystemOther = "other_sql";

        /// <summary>RabbitMQ messaging system.</summary>
        public const string SystemRabbitMq = "rabbitmq";
    }
}
