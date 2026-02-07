using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HVO.Common.Utilities;

/// <summary>
/// Provides runtime assertions for ensuring program state
/// </summary>
public static class Ensure
{
    /// <summary>
    /// Ensures that a condition is true
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="message">The exception message if condition is false</param>
    /// <param name="callerMemberName">The calling member name (automatically populated)</param>
    /// <param name="callerFilePath">The calling file path (automatically populated)</param>
    /// <param name="callerLineNumber">The calling line number (automatically populated)</param>
    /// <exception cref="InvalidOperationException">Thrown when the condition is false</exception>
    [DebuggerStepThrough]
    public static void That(
        bool condition,
        string message,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!condition)
        {
            var details = $"{message} [at {callerMemberName} in {callerFilePath}:{callerLineNumber}]";
            throw new InvalidOperationException(details);
        }
    }

    /// <summary>
    /// Ensures that a value is not null
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to check</param>
    /// <param name="message">The exception message if value is null</param>
    /// <param name="callerMemberName">The calling member name (automatically populated)</param>
    /// <param name="callerFilePath">The calling file path (automatically populated)</param>
    /// <param name="callerLineNumber">The calling line number (automatically populated)</param>
    /// <returns>The value if not null</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is null</exception>
    [DebuggerStepThrough]
    public static T NotNull<T>(
        T? value,
        string? message = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (value == null)
        {
            message = message ?? "Value cannot be null";
            var details = $"{message} [at {callerMemberName} in {callerFilePath}:{callerLineNumber}]";
            throw new InvalidOperationException(details);
        }
        
        return value!;
    }

    /// <summary>
    /// Ensures that a string is not null, empty, or whitespace
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <param name="message">The exception message if value is invalid</param>
    /// <param name="callerMemberName">The calling member name (automatically populated)</param>
    /// <param name="callerFilePath">The calling file path (automatically populated)</param>
    /// <param name="callerLineNumber">The calling line number (automatically populated)</param>
    /// <returns>The string if valid</returns>
    /// <exception cref="InvalidOperationException">Thrown when the string is null, empty, or whitespace</exception>
    [DebuggerStepThrough]
    public static string NotNullOrWhiteSpace(
        string? value,
        string? message = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            message = message ?? "String cannot be null, empty, or whitespace";
            var details = $"{message} [at {callerMemberName} in {callerFilePath}:{callerLineNumber}]";
            throw new InvalidOperationException(details);
        }
        
        return value!;
    }

    /// <summary>
    /// Ensures that a value is within a specified range
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to check</param>
    /// <param name="min">The minimum value (inclusive)</param>
    /// <param name="max">The maximum value (inclusive)</param>
    /// <param name="valueName">The name of the value</param>
    /// <param name="callerMemberName">The calling member name (automatically populated)</param>
    /// <param name="callerFilePath">The calling file path (automatically populated)</param>
    /// <param name="callerLineNumber">The calling line number (automatically populated)</param>
    /// <returns>The value if within range</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is outside the range</exception>
    [DebuggerStepThrough]
    public static T InRange<T>(
        T value,
        T min,
        T max,
        string? valueName = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            valueName = valueName ?? "Value";
            var message = $"{valueName} must be between {min} and {max}, but was {value}";
            var details = $"{message} [at {callerMemberName} in {callerFilePath}:{callerLineNumber}]";
            throw new InvalidOperationException(details);
        }
        
        return value;
    }

    /// <summary>
    /// Marks a code path as unreachable. If this method is called, it throws an exception.
    /// </summary>
    /// <param name="message">Optional message describing why this code should be unreachable</param>
    /// <param name="callerMemberName">The calling member name (automatically populated)</param>
    /// <param name="callerFilePath">The calling file path (automatically populated)</param>
    /// <param name="callerLineNumber">The calling line number (automatically populated)</param>
    /// <exception cref="InvalidOperationException">Always thrown</exception>
    [DebuggerStepThrough]
    public static void Unreachable(
        string? message = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        message = message ?? "Unreachable code was reached";
        var details = $"{message} [at {callerMemberName} in {callerFilePath}:{callerLineNumber}]";
        throw new InvalidOperationException(details);
    }
}
