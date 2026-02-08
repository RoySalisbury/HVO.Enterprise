using System;
using System.Diagnostics.Metrics;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Factory for creating runtime-adaptive metric recorders.
    /// </summary>
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
                return false;
            }
            catch (MissingMethodException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }
}
