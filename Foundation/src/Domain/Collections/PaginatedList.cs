// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Wangkanai.Foundation.Extensions;

namespace Wangkanai.Foundation.Collections;

/// <summary>
/// Represents a paginated list of items with metadata about the pagination.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PaginatedList<T> : List<T>
{
    /// <summary>
    /// Gets the current page index (1-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// Gets the first item number on the current page (1-based).
    /// </summary>
    public int FirstItemIndex => (PageIndex - 1) * PageSize + 1;

    /// <summary>
    /// Gets the last item number on the current page (1-based).
    /// </summary>
    public int LastItemIndex => Math.Min(PageIndex * PageSize, TotalCount);

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="count">The total count of items across all pages.</param>
    /// <param name="pageIndex">The current page index (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);

        AddRange(items);
    }

    /// <summary>
    /// Creates a paginated list from a queryable source.
    /// </summary>
    /// <param name="source">The source queryable.</param>
    /// <param name="pageIndex">The page index (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paginated list containing the items for the specified page.</returns>
    public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageIndex < 1)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be 1 or greater.");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

        var count = source.Count();
        var items = source.Skip((pageIndex - 1) * pageSize)
                          .Take(pageSize)
                          .ToList();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    /// <summary>
    /// Creates a paginated list from an enumerable source.
    /// </summary>
    /// <param name="source">The source enumerable.</param>
    /// <param name="pageIndex">The page index (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paginated list containing the items for the specified page.</returns>
    public static PaginatedList<T> Create(IEnumerable<T> source, int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageIndex < 1)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be 1 or greater.");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

        // Convert to list to avoid multiple enumeration
        var sourceList = source as IList<T> ?? source.ToList();
        var count = sourceList.Count;
        var items = sourceList.Skip((pageIndex - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    /// <summary>
    /// Groups the items in the paginated list by a property value.
    /// </summary>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>Grouped items from the current page.</returns>
    public IEnumerable<IGrouping<TKey, T>> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        return this.GroupPropertyValuesBy(keySelector);
    }

    /// <summary>
    /// Groups the items in the paginated list by a property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to group by.</param>
    /// <returns>Grouped items from the current page.</returns>
    public IEnumerable<IGrouping<object?, T>> GroupBy(string propertyName)
    {
        return this.GroupPropertyValuesBy(propertyName);
    }

    /// <summary>
    /// Maps the items in the paginated list to a different type while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <param name="selector">The mapping function.</param>
    /// <returns>A new paginated list with mapped items.</returns>
    public PaginatedList<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var mappedItems = this.Select(selector).ToList();
        return new PaginatedList<TResult>(mappedItems, TotalCount, PageIndex, PageSize);
    }

    /// <summary>
    /// Creates a paginated list asynchronously from a queryable source.
    /// </summary>
    /// <param name="source">The source queryable.</param>
    /// <param name="pageIndex">The page index (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list containing the items for the specified page.</returns>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageIndex < 1)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be 1 or greater.");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageIndex - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    /// <summary>
    /// Gets metadata about the pagination as a dictionary.
    /// </summary>
    /// <returns>A dictionary containing pagination metadata.</returns>
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["PageIndex"] = PageIndex,
            ["PageSize"] = PageSize,
            ["TotalPages"] = TotalPages,
            ["TotalCount"] = TotalCount,
            ["HasPreviousPage"] = HasPreviousPage,
            ["HasNextPage"] = HasNextPage,
            ["FirstItemIndex"] = FirstItemIndex,
            ["LastItemIndex"] = LastItemIndex
        };
    }
}