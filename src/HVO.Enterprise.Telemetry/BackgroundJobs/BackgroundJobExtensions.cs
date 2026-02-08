using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Helper methods for background job correlation.
    /// Captures the current correlation context and restores it inside the background work item.
    /// </summary>
    public static class BackgroundJobExtensions
    {
        /// <summary>
        /// Enqueues a background job with current correlation context using ThreadPool.
        /// </summary>
        /// <param name="action">The action to execute in the background.</param>
        /// <remarks>
        /// This is a simple implementation using ThreadPool. For production scenarios,
        /// consider using a dedicated job framework like Hangfire or Quartz.NET.
        /// </remarks>
        public static void EnqueueWithContext(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var context = BackgroundJobContext.Capture();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (context.Restore())
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Telemetry.RecordException(ex);
                    }
                }
            });
        }

        /// <summary>
        /// Enqueues an async background job with current correlation context.
        /// </summary>
        /// <param name="action">The async action to execute in the background.</param>
        /// <returns>A Task representing the background operation.</returns>
        /// <remarks>
        /// This is a simple implementation using Task.Run. For production scenarios,
        /// consider using a dedicated job framework like Hangfire or Quartz.NET.
        /// </remarks>
        public static Task EnqueueWithContextAsync(Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var context = BackgroundJobContext.Capture();

            return Task.Run(async () =>
            {
                using (context.Restore())
                {
                    await action().ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Enqueues an async background job with current correlation context and returns a result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="func">The async function to execute in the background.</param>
        /// <returns>A Task representing the background operation with its result.</returns>
        /// <remarks>
        /// This is a simple implementation using Task.Run. For production scenarios,
        /// consider using a dedicated job framework like Hangfire or Quartz.NET.
        /// </remarks>
        public static Task<T> EnqueueWithContextAsync<T>(Func<Task<T>> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var context = BackgroundJobContext.Capture();

            return Task.Run(async () =>
            {
                using (context.Restore())
                {
                    return await func().ConfigureAwait(false);
                }
            });
        }
    }
}
