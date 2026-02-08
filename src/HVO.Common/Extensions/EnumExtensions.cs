using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HVO.Common.Extensions;

/// <summary>
/// Extension methods for Enum types
/// </summary>
public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> DescriptionCache
        = new ConcurrentDictionary<Enum, string>();

    /// <summary>
    /// Gets the description from the Description attribute, or the enum value name if not present.
    /// Results are cached to avoid repeated reflection lookups.
    /// </summary>
    /// <param name="value">The enum value</param>
    /// <returns>The description or enum value name</returns>
    public static string GetDescription(this Enum value)
    {
        if (value == null)
            return string.Empty;

        return DescriptionCache.GetOrAdd(value, static v =>
        {
            var fieldInfo = v.GetType().GetField(v.ToString());
            if (fieldInfo == null)
                return v.ToString();

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : v.ToString();
        });
    }
}
