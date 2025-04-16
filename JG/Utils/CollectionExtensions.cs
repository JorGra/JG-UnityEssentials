using System;
using System.Collections.Generic;

public static class CollectionExtensions
{
    private static readonly Random _random = new Random();

    /// <summary>
    /// Returns a random element from the collection.
    /// Throws ArgumentNullException if the source is null or InvalidOperationException if the collection is empty.
    /// </summary>
    public static T GetRandom<T>(this IEnumerable<T> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // If the source implements IList<T>, we can access elements by index
        if (source is IList<T> list)
        {
            if (list.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements.");

            int randomIndex = _random.Next(list.Count);
            return list[randomIndex];
        }
        else
        {
            // Use reservoir sampling for collections that do not support indexing.
            T selected = default;
            int count = 0;
            foreach (T element in source)
            {
                count++;
                if (_random.Next(count) == 0)
                    selected = element;
            }

            if (count == 0)
                throw new InvalidOperationException("Sequence contains no elements.");

            return selected;
        }
    }
}
