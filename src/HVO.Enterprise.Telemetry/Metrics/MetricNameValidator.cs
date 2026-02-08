using System;

namespace HVO.Enterprise.Telemetry.Metrics
{
    internal static class MetricNameValidator
    {
        public static void ValidateName(string name, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Metric name must be non-empty.", parameterName);
        }
    }
}
