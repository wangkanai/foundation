// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;
using System.Reflection;

namespace Wangkanai.Foundation.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Groups elements by extracting values from a specified property and returns the grouped values.
    /// This extension method is useful for paginated lists and data aggregation scenarios.
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the collection.</typeparam>
    /// <typeparam name="TKey">The type of the property value used for grouping.</typeparam>
    /// <param name="source">The source collection to group.</param>
    /// <param name="propertySelector">An expression that selects the property to group by.</param>
    /// <returns>A grouped collection of property values.</returns>
    public static IEnumerable<IGrouping<TKey, TElement>> GroupPropertyValuesBy<TElement, TKey>(
        this IEnumerable<TElement> source,
        Expression<Func<TElement, TKey>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(propertySelector);

        var compiledSelector = propertySelector.Compile();
        return source.GroupBy(compiledSelector);
    }

    /// <summary>
    /// Groups elements by extracting values from a specified property using a string property name.
    /// This overload allows dynamic property selection at runtime.
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the collection.</typeparam>
    /// <param name="source">The source collection to group.</param>
    /// <param name="propertyName">The name of the property to group by.</param>
    /// <returns>A grouped collection of property values.</returns>
    public static IEnumerable<IGrouping<object?, TElement>> GroupPropertyValuesBy<TElement>(
        this IEnumerable<TElement> source,
        string propertyName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(propertyName);

        var type = typeof(TElement);
        var property = type.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
        {
            throw new ArgumentException(
                $"Property '{propertyName}' not found on type {type.Name}.",
                nameof(propertyName));
        }

        return source.GroupBy(element => property.GetValue(element));
    }

    /// <summary>
    /// Groups elements by extracting values from multiple properties and returns the grouped values.
    /// This is useful for creating composite grouping keys.
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the collection.</typeparam>
    /// <param name="source">The source collection to group.</param>
    /// <param name="propertyNames">The names of the properties to group by.</param>
    /// <returns>A grouped collection using composite keys.</returns>
    public static IEnumerable<IGrouping<string, TElement>> GroupPropertyValuesBy<TElement>(
        this IEnumerable<TElement> source,
        params string[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(propertyNames);

        if (propertyNames.Length == 0)
        {
            throw new ArgumentException("At least one property name must be specified.", nameof(propertyNames));
        }

        var type = typeof(TElement);
        var properties = new PropertyInfo[propertyNames.Length];

        for (int i = 0; i < propertyNames.Length; i++)
        {
            var property = type.GetProperty(propertyNames[i],
                BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                throw new ArgumentException(
                    $"Property '{propertyNames[i]}' not found on type {type.Name}.",
                    nameof(propertyNames));
            }

            properties[i] = property;
        }

        return source.GroupBy(element =>
        {
            var values = properties.Select(p => p.GetValue(element)?.ToString() ?? "null");
            return string.Join("|", values);
        });
    }

    /// <summary>
    /// Groups elements and applies a result selector to each group.
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the collection.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">The source collection to group.</param>
    /// <param name="keySelector">An expression that selects the grouping key.</param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <returns>A collection of results, one for each group.</returns>
    public static IEnumerable<TResult> GroupPropertyValuesBy<TElement, TKey, TResult>(
        this IEnumerable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var compiledSelector = keySelector.Compile();
        return source.GroupBy(compiledSelector, resultSelector);
    }
}