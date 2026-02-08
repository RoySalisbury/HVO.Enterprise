using System;
using System.Globalization;
using System.Text;

namespace HVO.Enterprise.Telemetry.Metrics
{
    internal static class MetricTagKeyBuilder
    {
        public static string BuildTaggedName(string name, in MetricTag tag1)
        {
            return string.Concat(name, ".", tag1.Key, "=", FormatValue(tag1.Value));
        }

        public static string BuildTaggedName(string name, in MetricTag tag1, in MetricTag tag2)
        {
            return string.Concat(
                name,
                ".", tag1.Key, "=", FormatValue(tag1.Value),
                ".", tag2.Key, "=", FormatValue(tag2.Value));
        }

        public static string BuildTaggedName(string name, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3)
        {
            return string.Concat(
                name,
                ".", tag1.Key, "=", FormatValue(tag1.Value),
                ".", tag2.Key, "=", FormatValue(tag2.Value),
                ".", tag3.Key, "=", FormatValue(tag3.Value));
        }

        public static string BuildTaggedName(string name, MetricTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return name;

            var builder = new StringBuilder(name.Length + (tags.Length * 16));
            builder.Append(name);

            for (int i = 0; i < tags.Length; i++)
            {
                builder.Append('.');
                builder.Append(tags[i].Key);
                builder.Append('=');
                builder.Append(FormatValue(tags[i].Value));
            }

            return builder.ToString();
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
                return "null";

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString() ?? string.Empty;
        }
    }
}
