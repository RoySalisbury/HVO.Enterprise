using System;

namespace HVO.Enterprise.Telemetry.Data.Common
{
    /// <summary>
    /// Detects the database system from connection strings and provider names.
    /// </summary>
    public static class DatabaseSystemDetector
    {
        /// <summary>
        /// Detects the database system based on connection string patterns and provider name.
        /// </summary>
        /// <param name="connectionString">The connection string to analyze.</param>
        /// <param name="providerName">Optional provider name (e.g., "System.Data.SqlClient").</param>
        /// <returns>A database system identifier from <see cref="DataActivityTags"/>.</returns>
        public static string DetectSystem(string? connectionString, string? providerName = null)
        {
            if (!string.IsNullOrWhiteSpace(providerName))
            {
                var provider = providerName!.ToLowerInvariant();

                // Check more specific names first to avoid false matches
                if (provider.Contains("mysql"))
                    return DataActivityTags.SystemMySql;
                if (provider.Contains("npgsql") || provider.Contains("postgres"))
                    return DataActivityTags.SystemPostgreSql;
                if (provider.Contains("oracle"))
                    return DataActivityTags.SystemOracle;
                if (provider.Contains("sqlite"))
                    return DataActivityTags.SystemSqlite;
                if (provider.Contains("sqlclient") || provider.Contains("mssql"))
                    return DataActivityTags.SystemMsSql;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                return DataActivityTags.SystemOther;

            var lower = connectionString!.ToLowerInvariant();

            // SQL Server patterns
            if ((lower.Contains("data source") || lower.Contains("server=")) &&
                (lower.Contains("initial catalog") || lower.Contains("database=")) &&
                !lower.Contains("host="))
            {
                // Disambiguate: "Server=" + "Database=" can be MySQL. Look for SQL Server markers.
                if (lower.Contains("data source") || lower.Contains("initial catalog") ||
                    lower.Contains("integrated security") || lower.Contains("trusted_connection"))
                    return DataActivityTags.SystemMsSql;
            }

            // PostgreSQL patterns
            if (lower.Contains("host=") && lower.Contains("database=") && !lower.Contains("server="))
                return DataActivityTags.SystemPostgreSql;

            // SQLite patterns
            if (lower.Contains("data source=") && (lower.Contains(".db") || lower.Contains(".sqlite") || lower.Contains("mode=")))
                return DataActivityTags.SystemSqlite;

            // MySQL pattern (Server=;Database= without host=)
            if (lower.Contains("server=") && lower.Contains("database=") && !lower.Contains("data source"))
                return DataActivityTags.SystemMySql;

            return DataActivityTags.SystemOther;
        }
    }
}
