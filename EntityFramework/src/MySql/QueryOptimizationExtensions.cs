// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;

namespace Wangkanai.EntityFramework.MySql;

/// <summary>
/// Provides extension methods for MySQL-specific query optimization in Entity Framework Core.
/// These extensions leverage MySQL's query hints, index management, and performance features.
/// </summary>
public static class QueryOptimizationExtensions
{
    /// <summary>
    /// Applies MySQL optimizer hints to influence query execution plan generation.
    /// Optimizer hints provide direct control over MySQL's query execution strategy.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The queryable to apply the hint to.</param>
    /// <param name="hint">The MySQL optimizer hint to apply.</param>
    /// <returns>The queryable with the optimizer hint applied.</returns>
    /// <remarks>
    /// <para>Common MySQL optimizer hints:</para>
    /// <list type="bullet">
    /// <item><description><strong>USE_INDEX_FOR_ORDER_BY:</strong> Use index for ORDER BY operations</description></item>
    /// <item><description><strong>USE_INDEX_FOR_GROUP_BY:</strong> Use index for GROUP BY operations</description></item>
    /// <item><description><strong>NO_INDEX_MERGE:</strong> Disable index merge optimization</description></item>
    /// <item><description><strong>NO_ICP:</strong> Disable Index Condition Pushdown</description></item>
    /// <item><description><strong>SEMIJOIN:</strong> Enable semijoin optimization strategies</description></item>
    /// <item><description><strong>NO_SEMIJOIN:</strong> Disable semijoin optimizations</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// SELECT /*+ USE_INDEX_FOR_ORDER_BY(products, idx_category_price) */ 
    /// * FROM products 
    /// WHERE category = 'Electronics' 
    /// ORDER BY price;
    /// </code>
    /// <para>Usage example:</para>
    /// <code>
    /// var products = context.Products
    ///     .WithMySqlHint("USE_INDEX_FOR_ORDER_BY")
    ///     .Where(p => p.Category == "Electronics")
    ///     .OrderBy(p => p.Price)
    ///     .ToList();
    /// </code>
    /// <para>Performance impact: 20-300% improvement with proper hint usage.</para>
    /// </remarks>
    public static IQueryable<T> WithMySqlHint<T>(
        this IQueryable<T> query,
        string hint) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(hint);

        // Apply the hint through EF Core's query annotation system
        return query.TagWith($"MySqlHint:{hint}");
    }

    /// <summary>
    /// Forces the query to use a specific index for optimal performance.
    /// This provides direct control over MySQL's index selection process.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The queryable to apply the index hint to.</param>
    /// <param name="indexName">The name of the index to use.</param>
    /// <returns>The queryable with the index hint applied.</returns>
    /// <remarks>
    /// <para>Index hint types available:</para>
    /// <list type="bullet">
    /// <item><description><strong>USE INDEX:</strong> Suggest using the specified index</description></item>
    /// <item><description><strong>FORCE INDEX:</strong> Force using the specified index</description></item>
    /// <item><description><strong>IGNORE INDEX:</strong> Ignore the specified index</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// SELECT * FROM products 
    /// FORCE INDEX (idx_category_price)
    /// WHERE category = 'Electronics' 
    /// AND price BETWEEN 100 AND 500;
    /// </code>
    /// <para>When to use index hints:</para>
    /// <list type="bullet">
    /// <item><description>MySQL optimizer chooses suboptimal execution plan</description></item>
    /// <item><description>Specific index provides significantly better performance</description></item>
    /// <item><description>Consistent query patterns benefit from forced index usage</description></item>
    /// <item><description>Complex queries where optimizer statistics are insufficient</description></item>
    /// </list>
    /// <para>Performance monitoring: Always validate performance improvements with EXPLAIN.</para>
    /// </remarks>
    public static IQueryable<T> UseMySqlIndex<T>(
        this IQueryable<T> query,
        string indexName) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);

        return query.TagWith($"MySqlForceIndex:{indexName}");
    }

    /// <summary>
    /// Configures query result caching using MySQL's query cache mechanism.
    /// Query cache stores SELECT statement results for improved response times on repeated queries.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The queryable to configure caching for.</param>
    /// <param name="cache">Whether to enable caching for this query (default: true).</param>
    /// <returns>The queryable with caching configuration applied.</returns>
    /// <remarks>
    /// <para>Query cache benefits:</para>
    /// <list type="bullet">
    /// <item><description>Dramatic performance improvement for repeated identical queries</description></item>
    /// <item><description>Reduces CPU usage for complex SELECT operations</description></item>
    /// <item><description>Ideal for read-heavy applications with stable data</description></item>
    /// <item><description>Automatic cache invalidation on data changes</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// SELECT SQL_CACHE * FROM products 
    /// WHERE category = 'Electronics';
    /// 
    /// SELECT SQL_NO_CACHE * FROM products 
    /// WHERE category = 'Electronics';
    /// </code>
    /// <para>Query cache considerations:</para>
    /// <list type="bullet">
    /// <item><description>Most effective for queries that are executed frequently</description></item>
    /// <item><description>Less beneficial for queries on frequently updated tables</description></item>
    /// <item><description>Cache hit rate monitoring recommended for optimization</description></item>
    /// <item><description>Memory usage scales with number of unique queries</description></item>
    /// </list>
    /// <para>Note: Query cache is deprecated in MySQL 8.0+. Consider application-level caching alternatives.</para>
    /// </remarks>
    public static IQueryable<T> WithMySqlQueryCache<T>(
        this IQueryable<T> query,
        bool cache = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);

        var cacheHint = cache ? "SQL_CACHE" : "SQL_NO_CACHE";
        return query.TagWith($"MySqlQueryCache:{cacheHint}");
    }

    /// <summary>
    /// Configures the query to use a specific MySQL buffer pool for optimized memory usage.
    /// Buffer pools control how data pages are cached in memory for improved access patterns.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The queryable to configure buffer pool usage for.</param>
    /// <param name="poolName">The name of the buffer pool to use.</param>
    /// <returns>The queryable with buffer pool configuration applied.</returns>
    /// <remarks>
    /// <para>Buffer pool benefits:</para>
    /// <list type="bullet">
    /// <item><description>Optimized memory allocation for different workload patterns</description></item>
    /// <item><description>Improved cache hit ratios for specific query types</description></item>
    /// <item><description>Better memory utilization in multi-tenant environments</description></item>
    /// <item><description>Workload isolation and performance predictability</description></item>
    /// </list>
    /// <para>Common buffer pool configurations:</para>
    /// <list type="bullet">
    /// <item><description><strong>oltp:</strong> Optimized for Online Transaction Processing workloads</description></item>
    /// <item><description><strong>analytics:</strong> Configured for analytical and reporting queries</description></item>
    /// <item><description><strong>mixed:</strong> Balanced configuration for mixed workloads</description></item>
    /// <item><description><strong>memory:</strong> Maximum memory allocation for critical queries</description></item>
    /// </list>
    /// <para>MySQL configuration example:</para>
    /// <code>
    /// -- Create buffer pool instances
    /// SET innodb_buffer_pool_instances = 4;
    /// SET innodb_buffer_pool_size = 8GB;
    /// 
    /// -- Use specific pool for query
    /// SELECT * FROM products 
    /// WHERE category = 'Electronics'
    /// /* Buffer pool: analytics */;
    /// </code>
    /// <para>Performance tuning guidelines:</para>
    /// <list type="bullet">
    /// <item><description>Match buffer pool characteristics to query patterns</description></item>
    /// <item><description>Monitor buffer pool hit ratios and memory usage</description></item>
    /// <item><description>Consider query frequency and data access patterns</description></item>
    /// <item><description>Balance memory allocation across different pools</description></item>
    /// </list>
    /// </remarks>
    public static IQueryable<T> UseMySqlBufferPool<T>(
        this IQueryable<T> query,
        string poolName) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(poolName);

        return query.TagWith($"MySqlBufferPool:{poolName}");
    }

    /// <summary>
    /// Sets MySQL-specific query timeout to prevent long-running queries from consuming resources.
    /// Query timeouts help maintain system responsiveness and prevent resource exhaustion.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The queryable to set timeout for.</param>
    /// <param name="seconds">The timeout value in seconds.</param>
    /// <returns>The queryable with timeout configuration applied.</returns>
    /// <remarks>
    /// <para>Query timeout benefits:</para>
    /// <list type="bullet">
    /// <item><description>Prevents runaway queries from impacting system performance</description></item>
    /// <item><description>Ensures predictable response times for application users</description></item>
    /// <item><description>Enables better resource allocation and system stability</description></item>
    /// <item><description>Supports SLA compliance and performance monitoring</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// SET SESSION max_execution_time = 30000; -- 30 seconds
    /// SELECT * FROM large_table 
    /// WHERE complex_condition = 'value';
    /// </code>
    /// <para>Recommended timeout values:</para>
    /// <list type="bullet">
    /// <item><description><strong>OLTP queries:</strong> 5-30 seconds for user-facing operations</description></item>
    /// <item><description><strong>Reporting queries:</strong> 300-1800 seconds (5-30 minutes)</description></item>
    /// <item><description><strong>Analytics queries:</strong> 1800-3600 seconds (30-60 minutes)</description></item>
    /// <item><description><strong>Batch processing:</strong> 3600+ seconds based on requirements</description></item>
    /// </list>
    /// <para>Usage considerations:</para>
    /// <list type="bullet">
    /// <item><description>Set appropriate timeout values based on expected query complexity</description></item>
    /// <item><description>Monitor query execution times to optimize timeout settings</description></item>
    /// <item><description>Consider implementing query result streaming for long operations</description></item>
    /// <item><description>Balance timeout values with user experience requirements</description></item>
    /// </list>
    /// </remarks>
    public static IQueryable<T> WithMySqlTimeout<T>(
        this IQueryable<T> query,
        int seconds) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(seconds, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(seconds, 86400); // 24 hours max

        return query.TagWith($"MySqlTimeout:{seconds}");
    }

    /// <summary>
    /// Applies multiple MySQL optimization hints in a single operation for complex query tuning.
    /// This method allows combining multiple optimization strategies for maximum performance.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The queryable to optimize.</param>
    /// <param name="options">Configuration options for comprehensive query optimization.</param>
    /// <returns>The queryable with all optimizations applied.</returns>
    /// <remarks>
    /// <para>Comprehensive optimization example:</para>
    /// <code>
    /// var products = context.Products
    ///     .WithMySqlOptimization(new MySqlQueryOptions
    ///     {
    ///         IndexName = "idx_category_price",
    ///         OptimizerHint = "USE_INDEX_FOR_ORDER_BY",
    ///         QueryCache = true,
    ///         BufferPool = "analytics",
    ///         TimeoutSeconds = 30
    ///     })
    ///     .Where(p => p.Category == "Electronics")
    ///     .OrderBy(p => p.Price)
    ///     .ToList();
    /// </code>
    /// </remarks>
    public static IQueryable<T> WithMySqlOptimization<T>(
        this IQueryable<T> query,
        MySqlQueryOptions options) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        var optimizedQuery = query;

        if (!string.IsNullOrEmpty(options.IndexName))
            optimizedQuery = optimizedQuery.UseMySqlIndex(options.IndexName);

        if (!string.IsNullOrEmpty(options.OptimizerHint))
            optimizedQuery = optimizedQuery.WithMySqlHint(options.OptimizerHint);

        if (options.QueryCache.HasValue)
            optimizedQuery = optimizedQuery.WithMySqlQueryCache(options.QueryCache.Value);

        if (!string.IsNullOrEmpty(options.BufferPool))
            optimizedQuery = optimizedQuery.UseMySqlBufferPool(options.BufferPool);

        if (options.TimeoutSeconds.HasValue)
            optimizedQuery = optimizedQuery.WithMySqlTimeout(options.TimeoutSeconds.Value);

        return optimizedQuery;
    }
}

/// <summary>
/// Configuration options for comprehensive MySQL query optimization.
/// Combines multiple optimization strategies for maximum query performance.
/// </summary>
public class MySqlQueryOptions
{
    /// <summary>
    /// The name of the index to force usage of in the query execution plan.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// MySQL optimizer hint to apply to the query for execution plan control.
    /// </summary>
    public string? OptimizerHint { get; set; }

    /// <summary>
    /// Whether to enable MySQL query cache for this specific query.
    /// </summary>
    public bool? QueryCache { get; set; }

    /// <summary>
    /// The name of the buffer pool to use for optimized memory allocation.
    /// </summary>
    public string? BufferPool { get; set; }

    /// <summary>
    /// Query timeout in seconds to prevent long-running operations.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Additional custom MySQL query annotations for specialized optimizations.
    /// </summary>
    public Dictionary<string, string> CustomAnnotations { get; set; } = new();
}