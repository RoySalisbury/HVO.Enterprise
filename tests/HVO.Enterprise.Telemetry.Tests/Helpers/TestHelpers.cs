using System;
using System.Diagnostics;
using System.Threading;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.Tests.Helpers
{
    /// <summary>
    /// Common test helper methods to reduce boilerplate across test classes.
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a fresh <see cref="ActivityListener"/> that enables all activities and
        /// returns it. The caller is responsible for disposing it.
        /// </summary>
        public static ActivityListener CreateGlobalListener()
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }

        /// <summary>
        /// Runs an action with a clean correlation context, restoring the original value afterward.
        /// </summary>
        public static void WithCleanCorrelation(Action action)
        {
            var original = CorrelationContext.GetRawValue();
            try
            {
                CorrelationContext.Clear();
                action();
            }
            finally
            {
                CorrelationContext.SetRawValue(original);
            }
        }

        /// <summary>
        /// Configures a minimal <see cref="IServiceCollection"/> with telemetry registered
        /// and returns a built <see cref="ServiceProvider"/>.
        /// </summary>
        public static ServiceProvider CreateTelemetryServiceProvider(
            Action<TelemetryOptions>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(options =>
            {
                options.ServiceName = "test-service";
                configure?.Invoke(options);
            });
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Runs an action on multiple threads concurrently and waits for all to complete.
        /// Useful for thread-safety tests.
        /// </summary>
        /// <param name="threadCount">Number of concurrent threads.</param>
        /// <param name="action">Action each thread executes. Receives the thread index.</param>
        public static void RunConcurrently(int threadCount, Action<int> action)
        {
            using (var barrier = new ManualResetEventSlim(false))
            {
                var threads = new Thread[threadCount];
                Exception? firstException = null;

                for (int i = 0; i < threadCount; i++)
                {
                    int index = i;
                    threads[i] = new Thread(() =>
                    {
                        barrier.Wait();
                        try
                        {
                            action(index);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                        }
                    });
                    threads[i].IsBackground = true;
                    threads[i].Start();
                }

                // Release all threads simultaneously
                barrier.Set();

                foreach (var thread in threads)
                {
                    thread.Join(TimeSpan.FromSeconds(30));
                }

                if (firstException != null)
                {
                    throw new AggregateException("Concurrent execution failed.", firstException);
                }
            }
        }

        /// <summary>
        /// Safely resets the static <see cref="Telemetry"/> class for test isolation.
        /// </summary>
        public static void ResetStaticTelemetry()
        {
            try { Telemetry.Shutdown(); } catch { /* ignore */ }
            Telemetry.ClearInstance();
        }
    }
}
