using System;
using System.Collections.Generic;
using System.Linq;

namespace HVO.Common.Utilities;

/// <summary>
/// Provides guard clauses for argument validation
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures that an argument is not null
    /// </summary>
    /// <typeparam name="T">The type of the argument</typeparam>
    /// <param name="value">The argument value</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The argument value if not null</returns>
    /// <exception cref="ArgumentNullException">Thrown when the value is null</exception>
    public static T AgainstNull<T>(T? value, string? parameterName = null) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(parameterName ?? nameof(value));
        
        return value;
    }

    /// <summary>
    /// Ensures that a string argument is not null, empty, or whitespace
    /// </summary>
    /// <param name="value">The string value</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The string value if valid</returns>
    /// <exception cref="ArgumentException">Thrown when the string is null, empty, or whitespace</exception>
    public static string AgainstNullOrWhiteSpace(string? value, string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace", parameterName ?? nameof(value));
        
        return value!;
    }

    /// <summary>
    /// Ensures that a string argument is not null or empty
    /// </summary>
    /// <param name="value">The string value</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The string value if valid</returns>
    /// <exception cref="ArgumentException">Thrown when the string is null or empty</exception>
    public static string AgainstNullOrEmpty(string? value, string? parameterName = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty", parameterName ?? nameof(value));
        
        return value!;
    }

    /// <summary>
    /// Ensures that a collection is not null or empty
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="value">The collection</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The collection if valid</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty</exception>
    public static IEnumerable<T> AgainstNullOrEmpty<T>(IEnumerable<T>? value, string? parameterName = null)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName ?? nameof(value));
        
        if (!value.Any())
            throw new ArgumentException("Collection cannot be empty", parameterName ?? nameof(value));
        
        return value;
    }

    /// <summary>
    /// Ensures that a value is within a specified range
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to check</param>
    /// <param name="min">The minimum value (inclusive)</param>
    /// <param name="max">The maximum value (inclusive)</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The value if within range</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the range</exception>
    public static T AgainstOutOfRange<T>(T value, T min, T max, string? parameterName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(parameterName ?? nameof(value), value, $"Value must be between {min} and {max}");
        
        return value;
    }

    /// <summary>
    /// Ensures that a value is greater than a specified minimum
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to check</param>
    /// <param name="min">The minimum value (exclusive)</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The value if greater than minimum</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not greater than minimum</exception>
    public static T AgainstNegativeOrZero<T>(T value, T min, string? parameterName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) <= 0)
            throw new ArgumentOutOfRangeException(parameterName ?? nameof(value), value, $"Value must be greater than {min}");
        
        return value;
    }

    /// <summary>
    /// Ensures that a condition is true
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="message">The exception message if condition is false</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when the condition is false</exception>
    public static void Against(bool condition, string message, string? parameterName = null)
    {
        if (condition)
            throw new ArgumentException(message, parameterName);
    }

    /// <summary>
    /// Ensures that an enum value is defined
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>The enum value if defined</returns>
    /// <exception cref="ArgumentException">Thrown when the enum value is not defined</exception>
    public static TEnum AgainstInvalidEnum<TEnum>(TEnum value, string? parameterName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new ArgumentException($"Value '{value}' is not a valid {typeof(TEnum).Name}", parameterName ?? nameof(value));
        
        return value;
    }
}
