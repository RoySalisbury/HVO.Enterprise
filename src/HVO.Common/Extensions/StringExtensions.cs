using System;
using System.Globalization;
using System.Linq;

namespace HVO.Common.Extensions;

/// <summary>
/// Extension methods for string operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Determines whether a string is null, empty, or consists only of white-space characters
    /// </summary>
    /// <param name="value">The string to test</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise false</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Determines whether a string is null or empty
    /// </summary>
    /// <param name="value">The string to test</param>
    /// <returns>True if the string is null or empty; otherwise false</returns>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Truncates a string to a maximum length
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">The maximum length</param>
    /// <returns>The truncated string</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength is negative</exception>
    public static string Truncate(this string value, int maxLength)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length cannot be negative");

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    /// <summary>
    /// Truncates a string to a maximum length and appends a suffix if truncated
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">The maximum length (including suffix)</param>
    /// <param name="suffix">The suffix to append if truncated (default: "...")</param>
    /// <returns>The truncated string with suffix if applicable</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength is negative or less than suffix length</exception>
    public static string TruncateWithSuffix(this string value, int maxLength, string suffix = "...")
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (suffix == null) throw new ArgumentNullException(nameof(suffix));
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length cannot be negative");
        if (maxLength < suffix.Length) throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be at least as long as the suffix");

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Converts a string to Title Case using the specified culture
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <param name="culture">The culture to use (null for current culture)</param>
    /// <returns>The string in Title Case</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToTitleCase(this string value, CultureInfo? culture = null)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        culture = culture ?? CultureInfo.CurrentCulture;
        return culture.TextInfo.ToTitleCase(value.ToLower());
    }

    /// <summary>
    /// Converts a string to an enum value
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The string value to parse</param>
    /// <param name="ignoreCase">Whether to ignore case when parsing</param>
    /// <returns>The parsed enum value</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <exception cref="ArgumentException">Thrown when the string cannot be parsed to the enum type</exception>
    public static TEnum ToEnum<TEnum>(this string value, bool ignoreCase = true) where TEnum : struct, Enum
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        return (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
    }

    /// <summary>
    /// Tries to convert a string to an enum value
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The string value to parse</param>
    /// <param name="result">The parsed enum value if successful</param>
    /// <param name="ignoreCase">Whether to ignore case when parsing</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryToEnum<TEnum>(this string? value, out TEnum result, bool ignoreCase = true) where TEnum : struct, Enum
    {
        if (value == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Reverses a string, correctly handling Unicode surrogate pairs.
    /// </summary>
    /// <param name="value">The string to reverse</param>
    /// <returns>The reversed string</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string Reverse(this string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (value.Length <= 1) return value;

        char[] charArray = value.ToCharArray();
        Array.Reverse(charArray);

        // Fix surrogate pairs that were swapped by the reversal.
        // After reversing, a high-low surrogate pair becomes low-high which is invalid.
        // We need to swap each such pair back to high-low order.
        for (int i = 0; i < charArray.Length - 1; i++)
        {
            if (char.IsLowSurrogate(charArray[i]) && char.IsHighSurrogate(charArray[i + 1]))
            {
                var temp = charArray[i];
                charArray[i] = charArray[i + 1];
                charArray[i + 1] = temp;
                i++; // skip the next char since we already processed it
            }
        }

        return new string(charArray);
    }

    /// <summary>
    /// Removes all whitespace from a string
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <returns>The string with all whitespace removed</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string RemoveWhitespace(this string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        return string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
    }

    /// <summary>
    /// Determines if a string contains any of the specified values
    /// </summary>
    /// <param name="value">The string to search</param>
    /// <param name="values">The values to search for</param>
    /// <returns>True if any value is found, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when value or values is null</exception>
    public static bool ContainsAny(this string value, params string[] values)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (values == null) throw new ArgumentNullException(nameof(values));

        return values.Any(v => value.Contains(v));
    }

    /// <summary>
    /// Determines if a string equals any of the specified values
    /// </summary>
    /// <param name="value">The string to compare</param>
    /// <param name="comparison">The string comparison type</param>
    /// <param name="values">The values to compare against</param>
    /// <returns>True if the string equals any value, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when value or values is null</exception>
    public static bool EqualsAny(this string value, StringComparison comparison, params string[] values)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (values == null) throw new ArgumentNullException(nameof(values));

        return values.Any(v => value.Equals(v, comparison));
    }
}
