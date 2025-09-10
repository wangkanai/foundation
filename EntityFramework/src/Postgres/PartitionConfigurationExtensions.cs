// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring PostgreSQL-specific table partitioning strategies.
/// Supports declarative partitioning (range, list, hash) with automatic partition management,
/// partition pruning optimization, and performance tuning for large datasets.
/// </summary>
public static class PartitionConfigurationExtensions
{
    #region Range Partitioning

    /// <summary>
    /// Configures a table for PostgreSQL range partitioning based on a date/time column.
    /// Range partitioning divides data into ranges that do not overlap, ideal for time-series data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionColumn">The column name to partition on.</param>
    /// <param name="tablespace">Optional tablespace for the partitioned table.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when partitionColumn is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Configure entity for monthly partitioning by created date
    /// builder.Entity&lt;SaleRecord&gt;()
    ///        .HasRangePartition("created_at", "sales_data_tbs");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE sales_records (
    /// //   id SERIAL PRIMARY KEY,
    /// //   created_at TIMESTAMPTZ NOT NULL,
    /// //   amount DECIMAL(10,2)
    /// // ) PARTITION BY RANGE (created_at) TABLESPACE sales_data_tbs;
    /// 
    /// // Enables automatic partition pruning for queries like:
    /// // SELECT * FROM sales_records WHERE created_at >= '2023-01-01' AND created_at &lt; '2023-02-01';
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> HasRangePartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string partitionColumn,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(partitionColumn))
            throw new ArgumentException("Partition column cannot be null or whitespace.", nameof(partitionColumn));

        builder.HasAnnotation("Npgsql:PartitionStrategy", "RANGE");
        builder.HasAnnotation("Npgsql:PartitionColumns", new[] { partitionColumn });
        
        if (!string.IsNullOrWhiteSpace(tablespace))
            builder.HasAnnotation("Npgsql:Tablespace", tablespace);

        return builder;
    }

    /// <summary>
    /// Configures a table for PostgreSQL multi-column range partitioning.
    /// Supports compound partitioning keys for complex partitioning strategies.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionColumns">Array of column names to partition on.</param>
    /// <param name="tablespace">Optional tablespace for the partitioned table.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when partitionColumns is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Configure entity for multi-column range partitioning
    /// builder.Entity&lt;LogEntry&gt;()
    ///        .HasMultiColumnRangePartition(new[] { "log_date", "severity_level" });
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE log_entries (
    /// //   id SERIAL PRIMARY KEY,
    /// //   log_date DATE NOT NULL,
    /// //   severity_level INT NOT NULL,
    /// //   message TEXT
    /// // ) PARTITION BY RANGE (log_date, severity_level);
    /// 
    /// // Optimizes queries with compound conditions:
    /// // SELECT * FROM log_entries 
    /// // WHERE log_date >= '2023-01-01' AND log_date &lt; '2023-02-01' 
    /// // AND severity_level >= 3;
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> HasMultiColumnRangePartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string[] partitionColumns,
        string? tablespace = null)
        where TEntity : class
    {
        if (partitionColumns == null || partitionColumns.Length == 0)
            throw new ArgumentException("Partition columns cannot be null or empty.", nameof(partitionColumns));

        builder.HasAnnotation("Npgsql:PartitionStrategy", "RANGE");
        builder.HasAnnotation("Npgsql:PartitionColumns", partitionColumns);
        
        if (!string.IsNullOrWhiteSpace(tablespace))
            builder.HasAnnotation("Npgsql:Tablespace", tablespace);

        return builder;
    }

    /// <summary>
    /// Creates a range partition for a specific date/time range with automatic maintenance.
    /// Supports automatic partition creation with configurable intervals and cleanup policies.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionName">Name for the partition table.</param>
    /// <param name="startValue">Start value for the range partition.</param>
    /// <param name="endValue">End value for the range partition.</param>
    /// <param name="tablespace">Optional tablespace for the partition.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when partitionName is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Create monthly partitions with automatic management
    /// builder.Entity&lt;SaleRecord&gt;()
    ///        .HasRangePartition("created_at")
    ///        .CreateRangePartition("sales_2023_01", "'2023-01-01'", "'2023-02-01'", "fast_ssd_tbs")
    ///        .CreateRangePartition("sales_2023_02", "'2023-02-01'", "'2023-03-01'", "fast_ssd_tbs");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE sales_2023_01 PARTITION OF sales_records
    /// //   FOR VALUES FROM ('2023-01-01') TO ('2023-02-01') TABLESPACE fast_ssd_tbs;
    /// // CREATE TABLE sales_2023_02 PARTITION OF sales_records
    /// //   FOR VALUES FROM ('2023-02-01') TO ('2023-03-01') TABLESPACE fast_ssd_tbs;
    /// 
    /// // Enables partition pruning and parallel query execution
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> CreateRangePartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string partitionName,
        string startValue,
        string endValue,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(partitionName))
            throw new ArgumentException("Partition name cannot be null or whitespace.", nameof(partitionName));

        var partitions = builder.Metadata.GetAnnotations()
            .FirstOrDefault(a => a.Name == "Npgsql:RangePartitions")?.Value as List<object> ?? new List<object>();

        partitions.Add(new
        {
            Name = partitionName,
            StartValue = startValue,
            EndValue = endValue,
            Tablespace = tablespace
        });

        builder.HasAnnotation("Npgsql:RangePartitions", partitions);
        return builder;
    }

    #endregion

    #region List Partitioning

    /// <summary>
    /// Configures a table for PostgreSQL list partitioning based on discrete values.
    /// List partitioning divides data based on explicit lists of key values, ideal for categorical data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionColumn">The column name to partition on.</param>
    /// <param name="tablespace">Optional tablespace for the partitioned table.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when partitionColumn is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Configure entity for list partitioning by region
    /// builder.Entity&lt;UserAccount&gt;()
    ///        .HasListPartition("region");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE user_accounts (
    /// //   id SERIAL PRIMARY KEY,
    /// //   username VARCHAR(50) NOT NULL,
    /// //   region VARCHAR(20) NOT NULL,
    /// //   created_at TIMESTAMPTZ DEFAULT NOW()
    /// // ) PARTITION BY LIST (region);
    /// 
    /// // Enables partition pruning for region-specific queries:
    /// // SELECT * FROM user_accounts WHERE region = 'US_WEST';
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> HasListPartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string partitionColumn,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(partitionColumn))
            throw new ArgumentException("Partition column cannot be null or whitespace.", nameof(partitionColumn));

        builder.HasAnnotation("Npgsql:PartitionStrategy", "LIST");
        builder.HasAnnotation("Npgsql:PartitionColumns", new[] { partitionColumn });
        
        if (!string.IsNullOrWhiteSpace(tablespace))
            builder.HasAnnotation("Npgsql:Tablespace", tablespace);

        return builder;
    }

    /// <summary>
    /// Creates a list partition for specific categorical values.
    /// Supports automatic creation of partitions based on value lists with performance optimization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionName">Name for the partition table.</param>
    /// <param name="values">Array of values that belong to this partition.</param>
    /// <param name="tablespace">Optional tablespace for the partition.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when partitionName is null or values is empty.</exception>
    /// <example>
    /// <code>
    /// // Create regional partitions
    /// builder.Entity&lt;UserAccount&gt;()
    ///        .HasListPartition("region")
    ///        .CreateListPartition("users_us", new[] { "'US_WEST'", "'US_EAST'", "'US_CENTRAL'" }, "us_data_tbs")
    ///        .CreateListPartition("users_eu", new[] { "'EU_WEST'", "'EU_CENTRAL'", "'EU_NORTH'" }, "eu_data_tbs")
    ///        .CreateListPartition("users_asia", new[] { "'ASIA_PACIFIC'", "'ASIA_SOUTHEAST'" }, "asia_data_tbs");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE users_us PARTITION OF user_accounts
    /// //   FOR VALUES IN ('US_WEST', 'US_EAST', 'US_CENTRAL') TABLESPACE us_data_tbs;
    /// // CREATE TABLE users_eu PARTITION OF user_accounts
    /// //   FOR VALUES IN ('EU_WEST', 'EU_CENTRAL', 'EU_NORTH') TABLESPACE eu_data_tbs;
    /// // CREATE TABLE users_asia PARTITION OF user_accounts
    /// //   FOR VALUES IN ('ASIA_PACIFIC', 'ASIA_SOUTHEAST') TABLESPACE asia_data_tbs;
    /// 
    /// // Enables geographically distributed queries with automatic partition pruning
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> CreateListPartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string partitionName,
        string[] values,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(partitionName))
            throw new ArgumentException("Partition name cannot be null or whitespace.", nameof(partitionName));
        if (values == null || values.Length == 0)
            throw new ArgumentException("Values cannot be null or empty.", nameof(values));

        var partitions = builder.Metadata.GetAnnotations()
            .FirstOrDefault(a => a.Name == "Npgsql:ListPartitions")?.Value as List<object> ?? new List<object>();

        partitions.Add(new
        {
            Name = partitionName,
            Values = values,
            Tablespace = tablespace
        });

        builder.HasAnnotation("Npgsql:ListPartitions", partitions);
        return builder;
    }

    #endregion

    #region Hash Partitioning

    /// <summary>
    /// Configures a table for PostgreSQL hash partitioning for even data distribution.
    /// Hash partitioning distributes rows evenly across partitions using a hash function,
    /// ideal for load balancing and parallel processing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionColumn">The column name to partition on.</param>
    /// <param name="partitionCount">Number of hash partitions to create.</param>
    /// <param name="tablespace">Optional tablespace for the partitioned table.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when partitionColumn is null or partitionCount is less than 2.</exception>
    /// <example>
    /// <code>
    /// // Configure entity for hash partitioning across 4 partitions
    /// builder.Entity&lt;Transaction&gt;()
    ///        .HasHashPartition("user_id", 4, "transaction_tbs");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE transactions (
    /// //   id SERIAL PRIMARY KEY,
    /// //   user_id INT NOT NULL,
    /// //   amount DECIMAL(10,2),
    /// //   created_at TIMESTAMPTZ DEFAULT NOW()
    /// // ) PARTITION BY HASH (user_id) TABLESPACE transaction_tbs;
    /// 
    /// // Automatically creates 4 hash partitions:
    /// // CREATE TABLE transactions_p0 PARTITION OF transactions FOR VALUES WITH (modulus 4, remainder 0);
    /// // CREATE TABLE transactions_p1 PARTITION OF transactions FOR VALUES WITH (modulus 4, remainder 1);
    /// // CREATE TABLE transactions_p2 PARTITION OF transactions FOR VALUES WITH (modulus 4, remainder 2);
    /// // CREATE TABLE transactions_p3 PARTITION OF transactions FOR VALUES WITH (modulus 4, remainder 3);
    /// 
    /// // Enables parallel query execution and even load distribution
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> HasHashPartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string partitionColumn,
        int modulus,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(partitionColumn))
            throw new ArgumentException("Partition column cannot be null or whitespace.", nameof(partitionColumn));
        if (modulus <= 1)
            throw new ArgumentOutOfRangeException(nameof(modulus), "Hash partition modulus must be greater than 1.");

        builder.HasAnnotation("Npgsql:PartitionStrategy", "HASH");
        builder.HasAnnotation("Npgsql:PartitionColumns", new[] { partitionColumn });
        builder.HasAnnotation("Npgsql:HashPartitionCount", modulus);
        
        if (!string.IsNullOrWhiteSpace(tablespace))
            builder.HasAnnotation("Npgsql:Tablespace", tablespace);

        return builder;
    }

    /// <summary>
    /// Creates individual hash partitions with custom configuration.
    /// Allows fine-grained control over hash partition distribution and storage.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="partitionName">Name for the partition table.</param>
    /// <param name="modulus">Total number of partitions in the hash scheme.</param>
    /// <param name="remainder">Remainder value for this specific partition (0 to modulus-1).</param>
    /// <param name="tablespace">Optional tablespace for the partition.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    /// <example>
    /// <code>
    /// // Create custom hash partitions with different tablespaces
    /// builder.Entity&lt;Transaction&gt;()
    ///        .HasHashPartition("user_id", 4)
    ///        .CreateHashPartition("txn_ssd_fast", 4, 0, "fast_ssd_tbs")
    ///        .CreateHashPartition("txn_ssd_med", 4, 1, "medium_ssd_tbs")
    ///        .CreateHashPartition("txn_hdd_0", 4, 2, "large_hdd_tbs")
    ///        .CreateHashPartition("txn_hdd_1", 4, 3, "large_hdd_tbs");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE txn_ssd_fast PARTITION OF transactions 
    /// //   FOR VALUES WITH (modulus 4, remainder 0) TABLESPACE fast_ssd_tbs;
    /// // CREATE TABLE txn_ssd_med PARTITION OF transactions 
    /// //   FOR VALUES WITH (modulus 4, remainder 1) TABLESPACE medium_ssd_tbs;
    /// // CREATE TABLE txn_hdd_0 PARTITION OF transactions 
    /// //   FOR VALUES WITH (modulus 4, remainder 2) TABLESPACE large_hdd_tbs;
    /// // CREATE TABLE txn_hdd_1 PARTITION OF transactions 
    /// //   FOR VALUES WITH (modulus 4, remainder 3) TABLESPACE large_hdd_tbs;
    /// 
    /// // Enables tiered storage with performance-based partition distribution
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> CreateHashPartition<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string partitionName,
        int modulus,
        int remainder,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(partitionName))
            throw new ArgumentException("Partition name cannot be null or whitespace.", nameof(partitionName));
        if (modulus <= 1)
            throw new ArgumentOutOfRangeException(nameof(modulus), "Hash partition modulus must be greater than 1.");
        if (remainder < 0 || remainder >= modulus)
            throw new ArgumentException("Remainder must be between 0 and modulus-1.", nameof(remainder));

        var partitions = builder.Metadata.GetAnnotations()
            .FirstOrDefault(a => a.Name == "Npgsql:HashPartitions")?.Value as List<object> ?? new List<object>();

        partitions.Add(new
        {
            Name = partitionName,
            Modulus = modulus,
            Remainder = remainder,
            Tablespace = tablespace
        });

        builder.HasAnnotation("Npgsql:HashPartitions", partitions);
        return builder;
    }

    #endregion

    #region Table Inheritance (Legacy Support)

    /// <summary>
    /// Configures a table for PostgreSQL table inheritance, providing compatibility with legacy partitioning.
    /// Table inheritance is the legacy approach to partitioning, maintained for backward compatibility.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="checkConstraint">Check constraint SQL for the inherited table.</param>
    /// <param name="tablespace">Optional tablespace for the inherited table.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when checkConstraint is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Configure legacy table inheritance for date-based partitioning
    /// builder.Entity&lt;LegacyOrder&gt;()
    ///        .HasTableInheritance("order_date >= '2023-01-01' AND order_date &lt; '2023-02-01'", "legacy_tbs");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE legacy_order_2023_01 (
    /// //   CHECK (order_date >= '2023-01-01' AND order_date &lt; '2023-02-01')
    /// // ) INHERITS (legacy_orders) TABLESPACE legacy_tbs;
    /// 
    /// // Note: Consider migrating to declarative partitioning for better performance
    /// // and automatic partition pruning capabilities
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> HasTableInheritance<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string checkConstraint,
        string? tablespace = null)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(checkConstraint))
            throw new ArgumentException("Check constraint cannot be null or whitespace.", nameof(checkConstraint));

        builder.HasAnnotation("Npgsql:InheritanceStrategy", "TableInheritance");
        builder.HasAnnotation("Npgsql:CheckConstraint", checkConstraint);
        
        if (!string.IsNullOrWhiteSpace(tablespace))
            builder.HasAnnotation("Npgsql:Tablespace", tablespace);

        return builder;
    }

    #endregion

    #region Partition Maintenance

    /// <summary>
    /// Enables automatic partition pruning optimization for improved query performance.
    /// Configures the query planner to eliminate irrelevant partitions during query execution.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="enableConstraintExclusion">Enable constraint exclusion for partition pruning.</param>
    /// <param name="partitionWiseJoins">Enable partition-wise joins for better performance.</param>
    /// <param name="partitionWiseAggregates">Enable partition-wise aggregates for parallel processing.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure partition pruning optimization
    /// builder.Entity&lt;SaleRecord&gt;()
    ///        .HasRangePartition("sale_date")
    ///        .EnablePartitionPruning(
    ///            enableConstraintExclusion: true,
    ///            partitionWiseJoins: true,
    ///            partitionWiseAggregates: true);
    /// 
    /// // Enables the following PostgreSQL settings:
    /// // SET constraint_exclusion = partition;
    /// // SET enable_partition_wise_join = on;
    /// // SET enable_partition_wise_aggregate = on;
    /// 
    /// // Optimizes queries by eliminating unnecessary partition scans:
    /// // EXPLAIN (BUFFERS, ANALYZE) SELECT SUM(amount) FROM sale_records 
    /// // WHERE sale_date >= '2023-01-01' AND sale_date &lt; '2023-02-01';
    /// // -> Only scans January 2023 partition, not all partitions
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> EnablePartitionPruning<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        bool enableConstraintExclusion = true,
        bool partitionWiseJoins = true,
        bool partitionWiseAggregates = true)
        where TEntity : class
    {
        builder.HasAnnotation("Npgsql:ConstraintExclusion", enableConstraintExclusion);
        builder.HasAnnotation("Npgsql:PartitionWiseJoin", partitionWiseJoins);
        builder.HasAnnotation("Npgsql:PartitionWiseAggregate", partitionWiseAggregates);
        
        return builder;
    }

    /// <summary>
    /// Configures automatic partition management with creation and cleanup policies.
    /// Supports dynamic partition creation based on data patterns and automatic cleanup of old partitions.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="autoCreatePartitions">Enable automatic creation of new partitions.</param>
    /// <param name="retentionPeriod">Retention period for partition cleanup (in days).</param>
    /// <param name="maintenanceSchedule">Cron expression for partition maintenance schedule.</param>
    /// <param name="maxPartitions">Maximum number of partitions to maintain.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure automatic partition management
    /// builder.Entity&lt;LogEntry&gt;()
    ///        .HasRangePartition("log_timestamp")
    ///        .ConfigurePartitionMaintenance(
    ///            autoCreatePartitions: true,
    ///            retentionPeriod: 365, // Keep partitions for 1 year
    ///            maintenanceSchedule: "0 2 * * 0", // Run weekly on Sunday at 2 AM
    ///            maxPartitions: 52); // Keep maximum 52 weekly partitions
    /// 
    /// // Enables automatic stored procedures:
    /// // CREATE OR REPLACE FUNCTION manage_log_partitions()
    /// // RETURNS void AS $$
    /// // BEGIN
    /// //   -- Create new partitions as needed
    /// //   -- Drop old partitions beyond retention period
    /// //   -- Maintain statistics on partitioned tables
    /// // END;
    /// // $$ LANGUAGE plpgsql;
    /// 
    /// // SELECT cron.schedule('log_partition_maintenance', '0 2 * * 0', 'SELECT manage_log_partitions();');
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> ConfigurePartitionMaintenance<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        bool autoCreatePartitions = true,
        int retentionPeriod = 30,
        string maintenanceSchedule = "0 2 * * 0",
        int maxPartitions = 100)
        where TEntity : class
    {
        builder.HasAnnotation("Npgsql:AutoCreatePartitions", autoCreatePartitions);
        builder.HasAnnotation("Npgsql:PartitionRetentionDays", retentionPeriod);
        builder.HasAnnotation("Npgsql:MaintenanceSchedule", maintenanceSchedule);
        builder.HasAnnotation("Npgsql:MaxPartitions", maxPartitions);
        
        return builder;
    }

    /// <summary>
    /// Configures cross-partition query optimization for complex analytical queries.
    /// Enables parallel execution across partitions and optimization of aggregate operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <param name="maxWorkerProcesses">Maximum number of worker processes for parallel queries.</param>
    /// <param name="workMemory">Work memory per worker process in MB.</param>
    /// <param name="enableParallelAggregation">Enable parallel aggregation across partitions.</param>
    /// <param name="enableParallelSort">Enable parallel sorting across partitions.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure cross-partition optimization for analytics
    /// builder.Entity&lt;SalesMetric&gt;()
    ///        .HasRangePartition("metric_date")
    ///        .OptimizeCrossPartitionQueries(
    ///            maxWorkerProcesses: 8,
    ///            workMemory: 256, // 256 MB per worker
    ///            enableParallelAggregation: true,
    ///            enableParallelSort: true);
    /// 
    /// // Optimizes complex analytical queries like:
    /// // SELECT metric_date, region, 
    /// //        SUM(revenue) as total_revenue,
    /// //        AVG(customer_count) as avg_customers
    /// // FROM sales_metrics 
    /// // WHERE metric_date >= '2023-01-01' 
    /// // GROUP BY metric_date, region 
    /// // ORDER BY metric_date, total_revenue DESC;
    /// 
    /// // Enables parallel execution across date range partitions
    /// // with optimized memory usage and worker distribution
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> OptimizeCrossPartitionQueries<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        int maxWorkerProcesses = 4,
        int workMemory = 128,
        bool enableParallelAggregation = true,
        bool enableParallelSort = true)
        where TEntity : class
    {
        builder.HasAnnotation("Npgsql:MaxWorkerProcesses", maxWorkerProcesses);
        builder.HasAnnotation("Npgsql:WorkMemoryMB", workMemory);
        builder.HasAnnotation("Npgsql:ParallelAggregation", enableParallelAggregation);
        builder.HasAnnotation("Npgsql:ParallelSort", enableParallelSort);
        
        return builder;
    }

    #endregion
}

/// <summary>
/// Represents the supported PostgreSQL partitioning strategies.
/// </summary>
public enum PartitionStrategy
{
    /// <summary>Range partitioning based on value ranges.</summary>
    Range,
    
    /// <summary>List partitioning based on explicit value lists.</summary>
    List,
    
    /// <summary>Hash partitioning for even data distribution.</summary>
    Hash,
    
    /// <summary>Legacy table inheritance partitioning.</summary>
    Inheritance
}

/// <summary>
/// Configuration options for automatic partition maintenance.
/// </summary>
public class PartitionMaintenanceOptions
{
    /// <summary>Enable automatic creation of new partitions.</summary>
    public bool AutoCreatePartitions { get; set; } = true;
    
    /// <summary>Retention period for partitions in days.</summary>
    public int RetentionDays { get; set; } = 30;
    
    /// <summary>Cron expression for maintenance schedule.</summary>
    public string MaintenanceSchedule { get; set; } = "0 2 * * 0"; // Weekly on Sunday at 2 AM
    
    /// <summary>Maximum number of partitions to maintain.</summary>
    public int MaxPartitions { get; set; } = 100;
    
    /// <summary>Enable automatic statistics collection on partitions.</summary>
    public bool AutoAnalyze { get; set; } = true;
    
    /// <summary>Enable automatic vacuum on partitions.</summary>
    public bool AutoVacuum { get; set; } = true;
}