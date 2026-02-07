using System;
using System.Collections.Generic;
using System.Linq;

namespace HVO.Common.Options;

/// <summary>
/// LINQ-style extension methods for Option&lt;T&gt;
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// Transforms an option that has a value using the provided mapper function
    /// </summary>
    /// <typeparam name="T">The source type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="option">The option to transform</param>
    /// <param name="mapper">The transformation function</param>
    /// <returns>A new option with the transformed value, or None if the original had no value</returns>
    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> mapper)
        where T : notnull
        where TResult : notnull
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));
        
        return option.HasValue && option.Value != null
            ? new Option<TResult>(mapper(option.Value))
            : Option<TResult>.None();
    }

    /// <summary>
    /// Transforms an option using a function that returns an Option (monadic bind/flatMap)
    /// </summary>
    /// <typeparam name="T">The source type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="option">The option to transform</param>
    /// <param name="binder">The transformation function that returns an Option</param>
    /// <returns>The result of the binder function, or None if the original had no value</returns>
    public static Option<TResult> Bind<T, TResult>(this Option<T> option, Func<T, Option<TResult>> binder)
        where T : notnull
        where TResult : notnull
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
        
        return (option.HasValue && option.Value != null) ? binder(option.Value) : Option<TResult>.None();
    }

    /// <summary>
    /// Executes an action if the option has a value
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="option">The option</param>
    /// <param name="action">The action to execute if a value exists</param>
    /// <returns>The original option for chaining</returns>
    public static Option<T> OnSome<T>(this Option<T> option, Action<T> action) where T : notnull
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        if (option.HasValue && option.Value != null)
        {
            action(option.Value);
        }
        
        return option;
    }

    /// <summary>
    /// Executes an action if the option has no value
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="option">The option</param>
    /// <param name="action">The action to execute if no value exists</param>
    /// <returns>The original option for chaining</returns>
    public static Option<T> OnNone<T>(this Option<T> option, Action action) where T : notnull
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        if (!option.HasValue)
        {
            action();
        }
        
        return option;
    }

    /// <summary>
    /// Filters options to only those with values and extracts the values
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="options">The sequence of options</param>
    /// <returns>A sequence of values from options that have values</returns>
    public static IEnumerable<T> WhereSome<T>(this IEnumerable<Option<T>> options) where T : notnull
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        return options.Where(o => o.HasValue && o.Value != null).Select(o => o.Value!);
    }

    /// <summary>
    /// Gets the value if present, or a default value if not
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="option">The option</param>
    /// <param name="defaultValue">The default value to return when no value is present</param>
    /// <returns>The option value or the default value</returns>
    public static T GetValueOrDefault<T>(this Option<T> option, T defaultValue) where T : notnull
    {
        return (option.HasValue && option.Value != null) ? option.Value : defaultValue;
    }

    /// <summary>
    /// Gets the value if present, or computes a default value if not
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="option">The option</param>
    /// <param name="defaultFactory">Factory function to compute the default value</param>
    /// <returns>The option value or the computed default value</returns>
    public static T GetValueOrDefault<T>(this Option<T> option, Func<T> defaultFactory) where T : notnull
    {
        if (defaultFactory == null) throw new ArgumentNullException(nameof(defaultFactory));
        
        return (option.HasValue && option.Value != null) ? option.Value : defaultFactory();
    }

    /// <summary>
    /// Converts an Option to a nullable value
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="option">The option</param>
    /// <returns>The value or null</returns>
    public static T? ToNullable<T>(this Option<T> option) where T : notnull
    {
        return option.HasValue ? option.Value : default;
    }

    /// <summary>
    /// Converts a nullable value to an Option
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="value">The nullable value</param>
    /// <returns>An Option containing the value if not null, otherwise None</returns>
    public static Option<T> ToOption<T>(this T? value) where T : notnull
    {
        return value != null ? new Option<T>(value) : Option<T>.None();
    }
}
