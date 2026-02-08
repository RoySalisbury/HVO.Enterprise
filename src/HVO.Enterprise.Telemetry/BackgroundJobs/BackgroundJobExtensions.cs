using System;
using System.Threading;
using System.Threading.Tasks;

namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Extension methods for background job correlation.
    /// </summary>
    public static class BackgroundJobExtensions
    {
        /// <summary>
        /// Enqueues a background job with current correlation context using ThreadPool.
        /// </summary>
        /// <param name="correlationId">The correlation ID (typically from CorrelationContext.Current).</param>
        /// <param name="action">The action to execute in the background.</param>
        /// <remarks>
        /// This is a simple implementation using ThreadPool. For production scenarios,
        /// consider using a dedicated job framework like Hangfire or Quartz.NET.
        /// </remarks>
        public static void EnqueueWithContext(this string correlationId, Action action)
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
                    catch (Exception)
                    {
                        // Exception should be handled by the action itself or logged
                        throw;
                    }
                }
            });
        }
        
        /// <summary>
        /// Enqueues an async background job with current correlation context.
        /// </summary>
        /// <param name="correlationId">The correlation ID (typically from CorrelationContext.Current).</param>
        /// <param name="action">The async action to execute in the background.</param>
        /// <returns>A Task representing the background operation.</returns>
        /// <remarks>
        /// This is a simple implementation using Task.Run. For production scenarios,
        /// consider using a dedicated job framework like Hangfire or Quartz.NET.
        /// </remarks>
        public static Task EnqueueWithContextAsync(this string correlationId, Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            var context = BackgroundJobContext.Capture();
            
            return Task.Run(async () =>
            {
                using (context.Restore())
                {
                    await action();
                }
            });
        }
        
        /// <summary>
        /// Enqueues an async background job with current correlation context and returns a result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="correlationId">The correlation ID (typically from CorrelationContext.Current).</param>
        /// <param name="func">The async function to execute in the background.</param>
        /// <returns>A Task representing the background operation with its result.</returns>
        /// <remarks>
        /// This is a simple implementation using Task.Run. For production scenarios,
        /// consider using a dedicated job framework like Hangfire or Quartz.NET.
        /// </remarks>
        public static Task<T> EnqueueWithContextAsync<T>(this string correlationId, Func<Task<T>> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            
            var context = BackgroundJobContext.Capture();
            
            return Task.Run(async () =>
            {
                using (context.Restore())
                {
                    return await func();
                }
            });
        }
    }
}
