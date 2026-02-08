using System;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Interface for background job framework integration.
    /// Implement this to provide automatic context propagation for specific job frameworks.
    /// </summary>
    public interface IBackgroundJobContextPropagator
    {
        /// <summary>
        /// Captures context and adds it to job data before enqueueing.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <param name="job">The job instance.</param>
        void PropagateContext<TJob>(TJob job) where TJob : class;

        /// <summary>
        /// Restores context from job data before execution.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <param name="job">The job instance.</param>
        /// <returns>
        /// An IDisposable that restores the previous context when disposed,
        /// or null if no context was found.
        /// </returns>
        IDisposable? RestoreContext<TJob>(TJob job) where TJob : class;
    }
}
