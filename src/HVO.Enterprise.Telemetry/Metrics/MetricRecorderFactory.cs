using System;
using System.Diagnostics.Metrics;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Factory for creating runtime-adaptive metric recorders.
    /// </summary>
    public static class MetricRecorderFactory
    {
        private static readonly Lazy<IMetricRecorder> InstanceLazy =
            new Lazy<IMetricRecorder>(CreateRecorder, true);

        /// <summary>
        /// Gets the singleton metric recorder instance optimized for the current runtime.
        /// </summary>
        public static IMetricRecorder Instance => InstanceLazy.Value;

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
                using (var meter = new Meter(MeterApiRecorder.MeterName, MeterApiRecorder.MeterVersion))
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
