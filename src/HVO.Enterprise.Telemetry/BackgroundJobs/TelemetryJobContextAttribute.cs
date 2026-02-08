using System;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Automatically restores telemetry context for background job methods.
    /// Apply to Hangfire, Quartz, or custom job methods.
    /// </summary>
    /// <remarks>
    /// This attribute is informational and can be used by frameworks to automatically
    /// restore context. The actual restoration logic must be implemented by the
    /// framework integration layer (e.g., Hangfire filters, Quartz listeners).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class TelemetryJobContextAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the parameter containing BackgroundJobContext.
        /// </summary>
        public string ContextParameterName { get; set; } = "context";
        
        /// <summary>
        /// Gets or sets whether to create a new Activity for the job.
        /// </summary>
        public bool CreateActivity { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to automatically restore correlation context.
        /// </summary>
        public bool RestoreCorrelation { get; set; } = true;
    }
}
