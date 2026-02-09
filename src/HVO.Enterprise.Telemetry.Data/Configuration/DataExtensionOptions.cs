using System;
using System.ComponentModel.DataAnnotations;

namespace HVO.Enterprise.Telemetry.Data.Configuration
{
    /// <summary>
    /// Base configuration options shared by all data extension packages.
    /// </summary>
    public class DataExtensionOptions
    {
        /// <summary>
        /// Whether to record SQL/command statements in telemetry.
        /// Default: <c>true</c>.
        /// </summary>
        public bool RecordStatements { get; set; } = true;

        /// <summary>
        /// Maximum statement length to record. Statements exceeding this length are truncated.
        /// Default: 2000 characters.
        /// </summary>
        [Range(100, 50000)]
        public int MaxStatementLength { get; set; } = 2000;

        /// <summary>
        /// Whether to record parameter values in telemetry.
        /// WARNING: May contain PII. Default: <c>false</c>.
        /// </summary>
        public bool RecordParameters { get; set; }

        /// <summary>
        /// Maximum number of parameters to record per operation.
        /// Default: 10.
        /// </summary>
        [Range(0, 100)]
        public int MaxParameters { get; set; } = 10;

        /// <summary>
        /// Filter predicate to control which operations are traced.
        /// Return <c>true</c> to trace the operation, <c>false</c> to skip it.
        /// Default: <c>null</c> (trace all operations).
        /// </summary>
        public Func<string, bool>? OperationFilter { get; set; }
    }
}
