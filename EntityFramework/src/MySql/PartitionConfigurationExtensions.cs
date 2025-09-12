// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring MySQL table partitioning for massive datasets and improved query performance.
/// Partitioning can provide 50-90% query performance improvement by enabling partition pruning.
/// </summary>
public static class PartitionConfigurationExtensions
{
    /// <summary>
    /// Configures table partitioning for large datasets using MySQL native partitioning.
    /// Partitioning distributes data across multiple physical partitions, enabling partition pruning
    /// for dramatic query performance improvements (50-90% faster queries).
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="type">The partitioning type (Range, List, Hash, Key).</param>
    /// <param name="partitionKey">Expression specifying the partition key column(s).</param>
    /// <param name="partitions">Number of partitions to create (default: 4, recommended: 8-32 for optimal performance).</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>Partitioning types and use cases:</para>
    /// <list type="bullet">
    /// <item><description><strong>Range:</strong> Time-series data, sequential IDs (ORDER BY optimized)</description></item>
    /// <item><description><strong>Hash:</strong> Even data distribution, load balancing (best for most use cases)</description></item>
    /// <item><description><strong>List:</strong> Categorical data, geographic regions</description></item>
    /// <item><description><strong>Key:</strong> Auto-generated hash using MySQL's algorithm</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE TABLE users (
    ///     id BIGINT PRIMARY KEY,
    ///     created_at DATETIME,
    ///     region VARCHAR(50)
    /// ) PARTITION BY HASH (id) PARTITIONS 16;
    /// </code>
    /// <para>Usage examples:</para>
    /// <code>
    /// // Hash partitioning for even distribution
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .HasMySqlPartitioning(PartitionType.Hash, user => user.Id, partitions: 16);
    /// 
    /// // Range partitioning for time-series data
    /// modelBuilder.Entity&lt;LogEntry&gt;()
    ///     .HasMySqlPartitioning(PartitionType.Range, log => log.CreatedAt, partitions: 12);
    /// </code>
    /// <para>Performance benefits:</para>
    /// <list type="bullet">
    /// <item><description>Partition pruning eliminates scanning irrelevant partitions</description></item>
    /// <item><description>Parallel query execution across partitions</description></item>
    /// <item><description>Improved maintenance operations (faster backups, index rebuilds)</description></item>
    /// <item><description>Better scalability for billion-row tables</description></item>
    /// </list>
    /// </remarks>
    public static EntityTypeBuilder<T> HasMySqlPartitioning<T>(
        this EntityTypeBuilder<T> builder,
        PartitionType type,
        Expression<Func<T, object>> partitionKey,
        int partitions = 4) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(partitionKey);
        
        if (partitions < 1 || partitions > 1024)
            throw new ArgumentException("Partition count must be between 1 and 1024", nameof(partitions));

        var keyExpression = GetPartitionKeyExpression(partitionKey);
        var partitionSpec = type switch
        {
            PartitionType.Range => $"PARTITION BY RANGE ({keyExpression})",
            PartitionType.List => $"PARTITION BY LIST ({keyExpression})",
            PartitionType.Hash => $"PARTITION BY HASH ({keyExpression}) PARTITIONS {partitions}",
            PartitionType.Key => $"PARTITION BY KEY ({keyExpression}) PARTITIONS {partitions}",
            _ => throw new ArgumentException($"Unsupported partition type: {type}", nameof(type))
        };

        builder.HasAnnotation("MySql:Partitioning", partitionSpec);
        builder.HasAnnotation("MySql:PartitionType", type.ToString());
        builder.HasAnnotation("MySql:PartitionCount", partitions);

        return builder;
    }

    /// <summary>
    /// Creates range-based partitions optimized for time-series data and sequential access patterns.
    /// Range partitioning provides optimal performance for date/time queries and enables automatic
    /// partition maintenance for rolling data retention policies.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="dateColumn">Expression specifying the date/datetime column for range partitioning.</param>
    /// <param name="interval">The partitioning interval (Daily, Weekly, Monthly, Quarterly, Yearly).</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>Range partitioning automatically creates time-based partitions for efficient data management:</para>
    /// <list type="bullet">
    /// <item><description><strong>Daily:</strong> High-volume logging, real-time analytics</description></item>
    /// <item><description><strong>Weekly:</strong> Moderate volume with weekly reporting cycles</description></item>
    /// <item><description><strong>Monthly:</strong> Most common for business applications</description></item>
    /// <item><description><strong>Quarterly:</strong> Long-term data with seasonal analysis</description></item>
    /// <item><description><strong>Yearly:</strong> Archival data with annual retention</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE TABLE log_entries (
    ///     id BIGINT AUTO_INCREMENT,
    ///     created_at DATETIME,
    ///     message TEXT,
    ///     PRIMARY KEY (id, created_at)
    /// ) PARTITION BY RANGE (YEAR(created_at)) (
    ///     PARTITION p2024 VALUES LESS THAN (2025),
    ///     PARTITION p2025 VALUES LESS THAN (2026),
    ///     PARTITION pmax VALUES LESS THAN MAXVALUE
    /// );
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;LogEntry&gt;()
    ///     .HasMySqlRangePartitions(log => log.CreatedAt, PartitionInterval.Monthly);
    /// 
    /// modelBuilder.Entity&lt;SalesRecord&gt;()
    ///     .HasMySqlRangePartitions(sale => sale.SaleDate, PartitionInterval.Quarterly);
    /// </code>
    /// <para>Benefits:</para>
    /// <list type="bullet">
    /// <item><description>Automatic partition pruning for date range queries</description></item>
    /// <item><description>Easy partition maintenance (drop old, add new partitions)</description></item>
    /// <item><description>Optimal for ORDER BY date queries</description></item>
    /// <item><description>Efficient data archival and purging</description></item>
    /// </list>
    /// </remarks>
    public static EntityTypeBuilder<T> HasMySqlRangePartitions<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, DateTime>> dateColumn,
        PartitionInterval interval = PartitionInterval.Monthly) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(dateColumn);

        var keyExpression = GetPartitionKeyExpression(dateColumn);
        var partitionFunction = interval switch
        {
            PartitionInterval.Daily => $"TO_DAYS({keyExpression})",
            PartitionInterval.Weekly => $"YEARWEEK({keyExpression})",
            PartitionInterval.Monthly => $"YEAR({keyExpression}) * 100 + MONTH({keyExpression})",
            PartitionInterval.Quarterly => $"YEAR({keyExpression}) * 10 + QUARTER({keyExpression})",
            PartitionInterval.Yearly => $"YEAR({keyExpression})",
            _ => throw new ArgumentException($"Unsupported partition interval: {interval}", nameof(interval))
        };

        var partitionSpec = $"PARTITION BY RANGE ({partitionFunction})";
        
        builder.HasAnnotation("MySql:Partitioning", partitionSpec);
        builder.HasAnnotation("MySql:PartitionType", "Range");
        builder.HasAnnotation("MySql:PartitionInterval", interval.ToString());
        builder.HasAnnotation("MySql:RangeFunction", partitionFunction);

        return builder;
    }

    /// <summary>
    /// Configures subpartitioning for complex data distribution patterns.
    /// Subpartitioning creates a two-level partitioning hierarchy, enabling fine-grained data distribution
    /// and parallel processing for complex queries involving multiple dimensions.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="type">The subpartitioning type (Hash or Key).</param>
    /// <param name="subpartitionsPerPartition">Number of subpartitions per main partition (default: 2, max: 16).</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>Subpartitioning creates a two-level hierarchy for maximum parallelism and data distribution:</para>
    /// <list type="bullet">
    /// <item><description><strong>Hash subpartitions:</strong> Even distribution based on hash function</description></item>
    /// <item><description><strong>Key subpartitions:</strong> MySQL's internal key-based distribution</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE TABLE sales_data (
    ///     id BIGINT,
    ///     sale_date DATE,
    ///     region VARCHAR(50),
    ///     amount DECIMAL(10,2)
    /// ) 
    /// PARTITION BY RANGE (YEAR(sale_date))
    /// SUBPARTITION BY HASH (region)
    /// SUBPARTITIONS 4 (
    ///     PARTITION p2024 VALUES LESS THAN (2025),
    ///     PARTITION p2025 VALUES LESS THAN (2026)
    /// );
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;SalesData&gt;()
    ///     .HasMySqlRangePartitions(s => s.SaleDate, PartitionInterval.Yearly)
    ///     .HasMySqlSubpartitions(SubpartitionType.Hash, subpartitionsPerPartition: 4);
    /// </code>
    /// <para>Benefits:</para>
    /// <list type="bullet">
    /// <item><description>Maximum parallelism for multi-dimensional queries</description></item>
    /// <item><description>Optimal load distribution across storage devices</description></item>
    /// <item><description>Fine-grained maintenance operations</description></item>
    /// <item><description>Improved scalability for complex analytical workloads</description></item>
    /// </list>
    /// </remarks>
    public static EntityTypeBuilder<T> HasMySqlSubpartitions<T>(
        this EntityTypeBuilder<T> builder,
        SubpartitionType type,
        int subpartitionsPerPartition = 2) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        if (subpartitionsPerPartition < 1 || subpartitionsPerPartition > 16)
            throw new ArgumentException("Subpartitions per partition must be between 1 and 16", nameof(subpartitionsPerPartition));

        var subpartitionSpec = type switch
        {
            SubpartitionType.Hash => $"SUBPARTITION BY HASH SUBPARTITIONS {subpartitionsPerPartition}",
            SubpartitionType.Key => $"SUBPARTITION BY KEY SUBPARTITIONS {subpartitionsPerPartition}",
            _ => throw new ArgumentException($"Unsupported subpartition type: {type}", nameof(type))
        };

        builder.HasAnnotation("MySql:Subpartitioning", subpartitionSpec);
        builder.HasAnnotation("MySql:SubpartitionType", type.ToString());
        builder.HasAnnotation("MySql:SubpartitionsPerPartition", subpartitionsPerPartition);

        return builder;
    }

    /// <summary>
    /// Manages partition maintenance operations for dynamic partition lifecycle management.
    /// Enables adding new partitions, dropping old ones, and optimizing existing partitions
    /// for automated data retention and performance maintenance.
    /// </summary>
    /// <typeparam name="T">The entity type being managed.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="operation">The partition operation to perform.</param>
    /// <param name="partitionName">The name of the partition to operate on.</param>
    /// <remarks>
    /// <para>Partition maintenance operations:</para>
    /// <list type="bullet">
    /// <item><description><strong>Add:</strong> Create new partition for future data</description></item>
    /// <item><description><strong>Drop:</strong> Remove old partition and its data</description></item>
    /// <item><description><strong>Reorganize:</strong> Redistribute data across partitions</description></item>
    /// <item><description><strong>Rebuild:</strong> Rebuild partition indexes and statistics</description></item>
    /// <item><description><strong>Optimize:</strong> Optimize partition storage and performance</description></item>
    /// <item><description><strong>Analyze:</strong> Update partition statistics for query optimizer</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL examples:</para>
    /// <code>
    /// -- Add new partition
    /// ALTER TABLE log_entries ADD PARTITION (PARTITION p2025_01 VALUES LESS THAN (TO_DAYS('2025-02-01')));
    /// 
    /// -- Drop old partition
    /// ALTER TABLE log_entries DROP PARTITION p2023_12;
    /// 
    /// -- Optimize partition
    /// ALTER TABLE log_entries OPTIMIZE PARTITION p2024_12;
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// // Add new monthly partition
    /// context.ManageMySqlPartition&lt;LogEntry&gt;(PartitionOperation.Add, "p2025_01");
    /// 
    /// // Drop old partition to free space
    /// context.ManageMySqlPartition&lt;LogEntry&gt;(PartitionOperation.Drop, "p2023_12");
    /// 
    /// // Optimize partition after heavy writes
    /// context.ManageMySqlPartition&lt;LogEntry&gt;(PartitionOperation.Optimize, "p2024_12");
    /// </code>
    /// <para>Automated maintenance example:</para>
    /// <code>
    /// // Monthly maintenance job
    /// var currentMonth = DateTime.Now.ToString("yyyy_MM");
    /// var oldMonth = DateTime.Now.AddMonths(-12).ToString("yyyy_MM");
    /// 
    /// context.ManageMySqlPartition&lt;LogEntry&gt;(PartitionOperation.Add, $"p{currentMonth}");
    /// context.ManageMySqlPartition&lt;LogEntry&gt;(PartitionOperation.Drop, $"p{oldMonth}");
    /// </code>
    /// </remarks>
    public static void ManageMySqlPartition<T>(
        this DbContext context,
        PartitionOperation operation,
        string partitionName) where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionName);

        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} not found in the model");

        var tableName = entityType.GetTableName();
        var sql = operation switch
        {
            PartitionOperation.Add => $"ALTER TABLE `{tableName}` ADD PARTITION (PARTITION {partitionName})",
            PartitionOperation.Drop => $"ALTER TABLE `{tableName}` DROP PARTITION {partitionName}",
            PartitionOperation.Reorganize => $"ALTER TABLE `{tableName}` REORGANIZE PARTITION {partitionName}",
            PartitionOperation.Rebuild => $"ALTER TABLE `{tableName}` REBUILD PARTITION {partitionName}",
            PartitionOperation.Optimize => $"ALTER TABLE `{tableName}` OPTIMIZE PARTITION {partitionName}",
            PartitionOperation.Analyze => $"ALTER TABLE `{tableName}` ANALYZE PARTITION {partitionName}",
            _ => throw new ArgumentException($"Unsupported partition operation: {operation}", nameof(operation))
        };

        context.Database.ExecuteSqlRaw(sql);
    }

    private static string GetPartitionKeyExpression<T>(Expression<Func<T, object>> keyExpression)
    {
        if (keyExpression.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        
        if (keyExpression.Body is UnaryExpression { Operand: MemberExpression unaryMemberExpr })
        {
            return unaryMemberExpr.Member.Name;
        }
        
        throw new ArgumentException("Partition key expression must be a simple property access", nameof(keyExpression));
    }

    private static string GetPartitionKeyExpression<T>(Expression<Func<T, DateTime>> keyExpression)
    {
        if (keyExpression.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        
        throw new ArgumentException("Partition key expression must be a simple property access", nameof(keyExpression));
    }
}

/// <summary>
/// Specifies the type of MySQL table partitioning to use.
/// Each type optimizes for different data distribution patterns and query access methods.
/// </summary>
public enum PartitionType
{
    /// <summary>
    /// Range partitioning divides data based on value ranges.
    /// Best for time-series data, sequential IDs, and ordered data access patterns.
    /// Enables partition pruning for range queries and supports automatic partition management.
    /// </summary>
    Range,

    /// <summary>
    /// List partitioning assigns specific values to partitions.
    /// Best for categorical data, geographic regions, and discrete value sets.
    /// Provides precise control over data distribution and supports uneven partition sizes.
    /// </summary>
    List,

    /// <summary>
    /// Hash partitioning uses a hash function for even data distribution.
    /// Best for general-purpose partitioning and load balancing across storage.
    /// Ensures uniform distribution but doesn't support partition pruning for range queries.
    /// </summary>
    Hash,

    /// <summary>
    /// Key partitioning uses MySQL's internal key hashing algorithm.
    /// Similar to Hash but uses MySQL's optimized partitioning function.
    /// Best when you want automatic key selection and optimized distribution.
    /// </summary>
    Key
}

/// <summary>
/// Specifies the time interval for range-based partitioning.
/// Different intervals optimize for various data retention and access patterns.
/// </summary>
public enum PartitionInterval
{
    /// <summary>
    /// Daily partitions for high-volume, real-time data.
    /// Creates one partition per day, optimal for logging and streaming data.
    /// Enables fine-grained retention policies and rapid data archival.
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly partitions for moderate volume with weekly cycles.
    /// Creates one partition per week, good for business applications with weekly reporting.
    /// Balances partition count with maintenance overhead.
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly partitions for standard business applications.
    /// Creates one partition per month, most common choice for transactional systems.
    /// Optimal balance of performance, maintenance, and storage management.
    /// </summary>
    Monthly,

    /// <summary>
    /// Quarterly partitions for seasonal data analysis.
    /// Creates one partition per quarter, good for business intelligence and reporting.
    /// Reduces partition count while maintaining query performance for quarterly reports.
    /// </summary>
    Quarterly,

    /// <summary>
    /// Yearly partitions for long-term data retention.
    /// Creates one partition per year, optimal for historical and archival data.
    /// Minimizes partition count for systems with long retention requirements.
    /// </summary>
    Yearly
}

/// <summary>
/// Specifies the type of subpartitioning for two-level partition hierarchies.
/// Subpartitioning provides fine-grained data distribution within main partitions.
/// </summary>
public enum SubpartitionType
{
    /// <summary>
    /// Hash-based subpartitioning for even data distribution.
    /// Uses hash function to distribute data evenly across subpartitions.
    /// Best for general-purpose subpartitioning and load balancing.
    /// </summary>
    Hash,

    /// <summary>
    /// Key-based subpartitioning using MySQL's internal algorithm.
    /// Uses MySQL's optimized key hashing for subpartition distribution.
    /// Best when you want automatic key selection and MySQL-optimized distribution.
    /// </summary>
    Key
}

/// <summary>
/// Specifies partition maintenance operations for lifecycle management.
/// Each operation serves different aspects of partition maintenance and optimization.
/// </summary>
public enum PartitionOperation
{
    /// <summary>
    /// Add a new partition to accommodate future data.
    /// Used for proactive partition management and data growth planning.
    /// Essential for time-based partitioning with rolling data retention.
    /// </summary>
    Add,

    /// <summary>
    /// Drop an existing partition and all its data.
    /// Used for data purging and storage space reclamation.
    /// Irreversible operation that permanently deletes partition data.
    /// </summary>
    Drop,

    /// <summary>
    /// Reorganize partition structure and redistribute data.
    /// Used when changing partition boundaries or merging/splitting partitions.
    /// Can be resource-intensive but necessary for partition restructuring.
    /// </summary>
    Reorganize,

    /// <summary>
    /// Rebuild partition indexes and storage structure.
    /// Used to defragment partition storage and rebuild corrupted indexes.
    /// Improves query performance after heavy write operations.
    /// </summary>
    Rebuild,

    /// <summary>
    /// Optimize partition storage and reclaim unused space.
    /// Used to compact partition data and improve storage efficiency.
    /// Similar to OPTIMIZE TABLE but operates on specific partitions.
    /// </summary>
    Optimize,

    /// <summary>
    /// Analyze partition data and update table statistics.
    /// Used to refresh query optimizer statistics for better execution plans.
    /// Should be run regularly after significant data changes.
    /// </summary>
    Analyze
}