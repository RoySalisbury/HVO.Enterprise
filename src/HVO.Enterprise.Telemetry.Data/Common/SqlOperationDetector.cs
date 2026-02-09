using System;

namespace HVO.Enterprise.Telemetry.Data.Common
{
    /// <summary>
    /// Detects the SQL operation type from a command text string.
    /// Shared utility used by EF Core and ADO.NET instrumentation packages
    /// to avoid duplication of SQL parsing logic.
    /// </summary>
    public static class SqlOperationDetector
    {
        /// <summary>
        /// Detects the SQL operation type from the beginning of a command text.
        /// Recognizes INSERT, UPDATE, DELETE, SELECT, MERGE, CREATE, ALTER, DROP, and EXEC/EXECUTE.
        /// </summary>
        /// <param name="commandText">The SQL command text to analyze.</param>
        /// <returns>
        /// The detected operation name in uppercase (e.g., "INSERT", "SELECT").
        /// Returns "EXECUTE" for unrecognized or null/empty input.
        /// </returns>
        public static string DetectOperation(string? commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return "EXECUTE";

            var trimmed = commandText!.TrimStart();
            if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                return "INSERT";
            if (trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                return "UPDATE";
            if (trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                return "DELETE";
            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "SELECT";
            if (trimmed.StartsWith("MERGE", StringComparison.OrdinalIgnoreCase))
                return "MERGE";
            if (trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
                return "CREATE";
            if (trimmed.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase))
                return "ALTER";
            if (trimmed.StartsWith("DROP", StringComparison.OrdinalIgnoreCase))
                return "DROP";
            if (trimmed.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
                return "EXECUTE";

            return "EXECUTE";
        }
    }
}
