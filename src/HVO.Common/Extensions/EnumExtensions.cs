using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HVO.Common.Extensions;

/// <summary>
/// Extension methods for Enum types
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the description from the Description attribute, or the enum value name if not present
    /// </summary>
    /// <param name="value">The enum value</param>
    /// <returns>The description or enum value name</returns>
    public static string GetDescription(this Enum value)
    {
        if (value == null)
            return string.Empty;

        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null)
            return value.ToString();

        var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }
}
