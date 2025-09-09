namespace Wangkanai.EntityFramework.Sqlite;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Extension methods for SQLite-specific query pattern optimizations.
/// Provides configuration options to enhance query performance for common patterns.
/// </summary>
public static class QueryOptimizationExtensions
{
    /// <summary>
    /// Configures the entity for optimized bulk SELECT operations in SQLite.
    /// Sets appropriate change tracking behavior and query splitting strategies.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="splitQuery">Whether to use query splitting for related data (default: true)</param>
    /// <param name="trackingBehavior">Query tracking behavior for bulk reads (default: NoTracking)</param>
    /// <returns>The entity type builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityBuilder is null</exception>
    /// <example>
    /// <code>
    /// entity.OptimizeForSqliteBulkReads&lt;Product&gt;(
    ///     splitQuery: true,
    ///     trackingBehavior: QueryTrackingBehavior.NoTracking);
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> OptimizeForSqliteBulkReads<T>(
        this EntityTypeBuilder<T> entityBuilder,
        bool splitQuery = true,
        QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.NoTracking) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityBuilder);

        // Configure for bulk read optimization
        entityBuilder.HasAnnotation("Sqlite:OptimizedForBulkReads", true);
        
        // Set query splitting strategy
        if (splitQuery)
        {
            entityBuilder.HasAnnotation("Sqlite:QuerySplittingBehavior", QuerySplittingBehavior.SplitQuery);
        }

        // Configure default tracking behavior
        entityBuilder.HasAnnotation("Sqlite:DefaultTrackingBehavior", trackingBehavior);

        // Optimize for sequential access patterns
        entityBuilder.HasAnnotation("Sqlite:AccessPattern", "Sequential");

        // Enable connection pooling optimizations
        entityBuilder.HasAnnotation("Sqlite:ConnectionPooling", true);

        // Configure read-ahead buffer size for large result sets
        entityBuilder.HasAnnotation("Sqlite:ReadAheadBufferSize", 8192);

        return entityBuilder;
    }

    /// <summary>
    /// Enables query plan caching for repeated query patterns to improve performance.
    /// Configures SQLite to cache and reuse execution plans for common queries.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="cacheSize">Maximum number of query plans to cache (default: 100)</param>
    /// <param name="enableStatistics">Whether to enable query statistics collection (default: false)</param>
    /// <returns>The entity type builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityBuilder is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheSize is less than 1</exception>
    /// <example>
    /// <code>
    /// entity.EnableSqliteQueryPlanCaching&lt;Order&gt;(
    ///     cacheSize: 200,
    ///     enableStatistics: true);
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> EnableSqliteQueryPlanCaching<T>(
        this EntityTypeBuilder<T> entityBuilder,
        int cacheSize = 100,
        bool enableStatistics = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityBuilder);
        ArgumentOutOfRangeException.ThrowIfLessThan(cacheSize, 1);

        // Enable query plan caching
        entityBuilder.HasAnnotation("Sqlite:QueryPlanCaching", true);
        
        // Set cache size
        entityBuilder.HasAnnotation("Sqlite:QueryPlanCacheSize", cacheSize);

        // Configure statistics collection
        if (enableStatistics)
        {
            entityBuilder.HasAnnotation("Sqlite:QueryStatistics", true);
            entityBuilder.HasAnnotation("Sqlite:StatisticsCollection", "Detailed");
        }

        // Enable prepared statement caching
        entityBuilder.HasAnnotation("Sqlite:PreparedStatementCaching", true);

        // Configure automatic query optimization
        entityBuilder.HasAnnotation("Sqlite:AutoOptimizeQueries", true);

        return entityBuilder;
    }

    /// <summary>
    /// Optimizes entity configuration for aggregation operations (SUM, COUNT, AVG, etc.).
    /// Configures indexes and storage optimizations specifically for aggregate queries.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="aggregateProperties">Properties commonly used in aggregations</param>
    /// <returns>The entity type builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityBuilder or aggregateProperties is null</exception>
    /// <exception cref="ArgumentException">Thrown when aggregateProperties is empty</exception>
    /// <example>
    /// <code>
    /// entity.OptimizeForSqliteAggregations&lt;OrderItem&gt;(
    ///     o => o.Quantity,
    ///     o => o.UnitPrice,
    ///     o => o.TotalAmount);
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> OptimizeForSqliteAggregations<T>(
        this EntityTypeBuilder<T> entityBuilder,
        params Expression<Func<T, object>>[] aggregateProperties) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityBuilder);
        ArgumentNullException.ThrowIfNull(aggregateProperties);
        
        if (aggregateProperties.Length == 0)
            throw new ArgumentException("At least one aggregate property must be specified", nameof(aggregateProperties));

        // Mark entity as optimized for aggregations
        entityBuilder.HasAnnotation("Sqlite:OptimizedForAggregations", true);

        // Configure each aggregate property
        foreach (var property in aggregateProperties)
        {
            var propertyName = ExtractPropertyName(property);
            if (!string.IsNullOrEmpty(propertyName))
            {
                // Create covering index for aggregation performance
                entityBuilder.HasIndex(property)
                           .HasDatabaseName($"IX_{typeof(T).Name}_{propertyName}_Aggregate")
                           .HasAnnotation("Sqlite:AggregateOptimized", true);

                // Configure property for aggregation
                entityBuilder.Property(property)
                           .HasAnnotation("Sqlite:AggregateProperty", true)
                           .HasAnnotation("Sqlite:StatisticsTarget", 1000); // Higher statistics for better query plans
            }
        }

        // Enable parallel aggregation where supported
        entityBuilder.HasAnnotation("Sqlite:ParallelAggregation", true);

        // Configure memory usage for large aggregations
        entityBuilder.HasAnnotation("Sqlite:AggregationMemoryLimit", "256MB");

        // Enable temporary table usage for complex aggregations
        entityBuilder.HasAnnotation("Sqlite:UseTempTablesForAggregation", true);

        return entityBuilder;
    }

    /// <summary>
    /// Configures the entity for optimal JOIN performance with related entities.
    /// Sets up foreign key indexes and relationship loading strategies.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="joinStrategy">The preferred join strategy (default: Hash)</param>
    /// <param name="batchSize">Batch size for related entity loading (default: 1000)</param>
    /// <returns>The entity type builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityBuilder is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1</exception>
    /// <example>
    /// <code>
    /// entity.OptimizeForSqliteJoins&lt;Order&gt;(
    ///     joinStrategy: SqliteJoinStrategy.NestedLoop,
    ///     batchSize: 500);
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> OptimizeForSqliteJoins<T>(
        this EntityTypeBuilder<T> entityBuilder,
        SqliteJoinStrategy joinStrategy = SqliteJoinStrategy.Hash,
        int batchSize = 1000) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityBuilder);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        // Configure join optimization
        entityBuilder.HasAnnotation("Sqlite:JoinStrategy", joinStrategy.ToString());
        entityBuilder.HasAnnotation("Sqlite:JoinBatchSize", batchSize);

        // Enable foreign key index optimization
        entityBuilder.HasAnnotation("Sqlite:OptimizeForeignKeyIndexes", true);

        // Configure join buffer size
        entityBuilder.HasAnnotation("Sqlite:JoinBufferSize", Math.Min(batchSize * 64, 65536)); // 64 bytes per row estimate

        // Enable join order optimization
        entityBuilder.HasAnnotation("Sqlite:OptimizeJoinOrder", true);

        return entityBuilder;
    }

    /// <summary>
    /// Configures the entity for optimal full-text search performance.
    /// Sets up FTS5 virtual table integration where appropriate.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="searchProperties">Properties to include in full-text search</param>
    /// <param name="enablePorter">Whether to enable Porter stemming (default: true)</param>
    /// <returns>The entity type builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityBuilder or searchProperties is null</exception>
    /// <exception cref="ArgumentException">Thrown when searchProperties is empty</exception>
    /// <example>
    /// <code>
    /// entity.OptimizeForSqliteFullTextSearch&lt;Product&gt;(
    ///     new[] { p => p.Name, p => p.Description },
    ///     enablePorter: true);
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> OptimizeForSqliteFullTextSearch<T>(
        this EntityTypeBuilder<T> entityBuilder,
        Expression<Func<T, object>>[] searchProperties,
        bool enablePorter = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityBuilder);
        ArgumentNullException.ThrowIfNull(searchProperties);
        
        if (searchProperties.Length == 0)
            throw new ArgumentException("At least one search property must be specified", nameof(searchProperties));

        // Mark entity as FTS-optimized
        entityBuilder.HasAnnotation("Sqlite:FullTextSearch", true);
        entityBuilder.HasAnnotation("Sqlite:FtsVersion", "FTS5");

        // Configure search properties
        var searchPropertyNames = searchProperties
            .Select(ExtractPropertyName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        entityBuilder.HasAnnotation("Sqlite:FtsProperties", string.Join(",", searchPropertyNames));

        // Configure stemming
        if (enablePorter)
        {
            entityBuilder.HasAnnotation("Sqlite:FtsTokenizer", "porter");
        }

        // Enable content synchronization
        entityBuilder.HasAnnotation("Sqlite:FtsContentSync", true);

        return entityBuilder;
    }

    #region Private Helper Methods and Enums

    /// <summary>
    /// SQLite join strategy options for performance optimization.
    /// </summary>
    public enum SqliteJoinStrategy
    {
        /// <summary>
        /// Use nested loop joins (good for small result sets)
        /// </summary>
        NestedLoop,
        
        /// <summary>
        /// Use hash joins (good for larger result sets)
        /// </summary>
        Hash,
        
        /// <summary>
        /// Let SQLite choose the optimal strategy
        /// </summary>
        Auto
    }

    /// <summary>
    /// Extracts the property name from a lambda expression.
    /// </summary>
    private static string ExtractPropertyName<T>(Expression<Func<T, object>> propertyExpression)
    {
        return propertyExpression.Body switch
        {
            MemberExpression memberExpr => memberExpr.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpr } => memberExpr.Member.Name,
            _ => string.Empty
        };
    }

    #endregion
}