using System;
using System.Collections.Generic;
using System.Linq;

namespace HVO.Common.Results;

/// <summary>
/// LINQ-style extension methods for Result&lt;T&gt;
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Transforms a successful result using the provided mapper function
    /// </summary>
    /// <typeparam name="T">The source type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="result">The result to transform</param>
    /// <param name="mapper">The transformation function</param>
    /// <returns>A new result with the transformed value, or the original error</returns>
    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));
        
        return result.IsSuccessful
            ? Result<TResult>.Success(mapper(result.Value))
            : Result<TResult>.Failure(result.Error!);
    }

    /// <summary>
    /// Transforms a successful result using a function that returns a Result (monadic bind/flatMap)
    /// </summary>
    /// <typeparam name="T">The source type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="result">The result to transform</param>
    /// <param name="binder">The transformation function that returns a Result</param>
    /// <returns>The result of the binder function, or the original error</returns>
    public static Result<TResult> Bind<T, TResult>(this Result<T> result, Func<T, Result<TResult>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
        
        return result.IsSuccessful
            ? binder(result.Value)
            : Result<TResult>.Failure(result.Error!);
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute on success</param>
    /// <returns>The original result for chaining</returns>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        if (result.IsSuccessful)
        {
            action(result.Value);
        }
        
        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">The action to execute on failure</param>
    /// <returns>The original result for chaining</returns>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<Exception?> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        if (result.IsFailure)
        {
            action(result.Error);
        }
        
        return result;
    }

    /// <summary>
    /// Filters a sequence to only successful results and extracts their values
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="results">The sequence of results</param>
    /// <returns>A sequence of successful values</returns>
    public static IEnumerable<T> WhereSuccess<T>(this IEnumerable<Result<T>> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        
        return results.Where(r => r.IsSuccessful).Select(r => r.Value);
    }

    /// <summary>
    /// Filters a sequence to only failed results and extracts their errors
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="results">The sequence of results</param>
    /// <returns>A sequence of errors</returns>
    public static IEnumerable<Exception> WhereFailure<T>(this IEnumerable<Result<T>> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        
        return results.Where(r => r.IsFailure).Select(r => r.Error!);
    }

    /// <summary>
    /// Gets the value if successful, or a default value if failed
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="defaultValue">The default value to return on failure</param>
    /// <returns>The result value or the default value</returns>
    public static T GetValueOrDefault<T>(this Result<T> result, T defaultValue)
    {
        return result.IsSuccessful ? result.Value : defaultValue;
    }

    /// <summary>
    /// Gets the value if successful, or computes a default value if failed
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="defaultFactory">Factory function to compute the default value</param>
    /// <returns>The result value or the computed default value</returns>
    public static T GetValueOrDefault<T>(this Result<T> result, Func<T> defaultFactory)
    {
        if (defaultFactory == null) throw new ArgumentNullException(nameof(defaultFactory));
        
        return result.IsSuccessful ? result.Value : defaultFactory();
    }
}
