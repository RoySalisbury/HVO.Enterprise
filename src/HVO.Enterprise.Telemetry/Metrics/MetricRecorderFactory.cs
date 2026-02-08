using System;
using System.Diagnostics.Metrics;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Factory for creating runtime-adaptive metric recorders.
    /// </summary>
    /// <remarks>
    /// The singleton instance is held in a static field for the process lifetime.
    /// If the concrete recorder implements <see cref="IDisposable"/> (e.g.,
    /// <see cref="MeterApiRecorder"/>), it will be cleaned up by the GC finalizer
    /// on process exit. For testing or explicit cleanup, cast the <see cref="Instance"/>
    /// to <see cref="IDisposable"/> and call <c>Dispose()</c> directly.
    /// </remarks>
    public static class MetricRecorderFactory
    {
        private static IMetricRecorder? _instance;
        private static readonly object Lock = new object();

        /// <summary>
        /// Gets the singleton metric recorder instance optimized for the current runtime.
        /// Unlike <see cref="Lazy{T}"/>, this factory retries creation on failure
        /// to avoid permanently caching transient initialization errors.
        /// </summary>
        public static IMetricRecorder Instance
        {
            get
            {
                var instance = Volatile.Read(ref _instance);
                if (instance != null)
                    return instance;

                lock (Lock)
                {
                    instance = Volatile.Read(ref _instance);
                    if (instance != null)
                        return instance;

                    instance = CreateRecorder();
                    Volatile.Write(ref _instance, instance);
                    return instance;
                }
            }
        }

        private static IMetricRecorder CreateRecorder()
        {
            if (IsMeterApiAvailable())
                return new MeterApiRecorder();

            return new EventCounterRecorder();
        }

        private static bool IsMeterApiAvailable()
        {
            if (Environment.Version.Major < 6)
                return false;

            try
            {
                using (new Meter(MeterApiRecorder.MeterName, MeterApiRecorder.MeterVersion))
                {
                }

                return true;
            }
            catch (TypeLoadException)
            {
                // Expected when running on a runtime that lacks the Meter API surface.
                // This is intentional runtime capability probing — not an error.
                return false;
            }
            catch (MissingMethodException)
            {
                // Expected when the API exists as a type but is missing specific methods
                // on the current runtime version.
                return false;
            }
            catch (NotSupportedException)
            {
                // Some runtimes (e.g., Blazor WASM) throw NotSupportedException for
                // unsupported diagnostic APIs.
                return false;
            }
        }
    }
}
