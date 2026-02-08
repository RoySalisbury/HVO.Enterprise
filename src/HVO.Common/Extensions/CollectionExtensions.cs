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
    /// Returns distinct elements from a sequence according to a specified key selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="source">The collection to search</param>
    /// <param name="keySelector">Selector for the key used to determine uniqueness</param>
    /// <param name="comparer">Optional comparer for the key type</param>
    /// <returns>Distinct elements by key</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or keySelector is null</exception>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        var set = comparer == null
            ? new HashSet<TKey>()
            : new HashSet<TKey>(comparer);

        foreach (var item in source)
        {
            var key = keySelector(item);
            if (set.Add(key))
                yield return item;
        }
    }

    /// <summary>
    /// Splits a sequence into chunks of a specified size.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="source">The collection to chunk</param>
    /// <param name="size">The size of each chunk</param>
    /// <returns>Chunks of the requested size</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when size is less than or equal to zero</exception>
    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Chunk size must be positive");

        var buffer = new List<T>(size);

        foreach (var item in source)
        {
            buffer.Add(item);
            if (buffer.Count == size)
            {
                yield return buffer.ToArray();
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
            yield return buffer.ToArray();
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
