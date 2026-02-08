using System;
using System.Collections.Generic;
using System.Linq;

namespace HVO.Common.Extensions;

/// <summary>
/// Extension methods for collection operations
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Determines whether a collection is null or empty
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to check</param>
    /// <returns>True if the collection is null or contains no elements; otherwise false</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    /// <summary>
    /// Safely iterates over a collection, returning an empty collection if the source is null
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to iterate</param>
    /// <returns>The original collection or an empty collection if source is null</returns>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    /// <summary>
    /// Executes an action for each element in a collection
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to iterate</param>
    /// <param name="action">The action to execute for each element</param>
    /// <exception cref="ArgumentNullException">Thrown when source or action is null</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Executes an action for each element in a collection with its index
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to iterate</param>
    /// <param name="action">The action to execute for each element (item, index)</param>
    /// <exception cref="ArgumentNullException">Thrown when source or action is null</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        int index = 0;
        foreach (var item in source)
        {
            action(item, index++);
        }
    }

    /// <summary>
    /// Returns the index of the first element that matches a predicate
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to search</param>
    /// <param name="predicate">The predicate to match</param>
    /// <returns>The index of the first matching element, or -1 if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or predicate is null</exception>
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        int index = 0;
        foreach (var item in source)
        {
            if (predicate(item))
                return index;
            index++;
        }

        return -1;
    }

    /// <summary>
    /// Randomly shuffles the elements of a sequence
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to shuffle</param>
    /// <returns>A new sequence with elements in random order</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null</exception>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var list = source.ToList();
        var rng = new Random();
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }

        return list;
    }
}
