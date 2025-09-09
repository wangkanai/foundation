// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;

namespace Wangkanai.EntityFramework.SqlServer;

/// <summary>
/// Provides extension methods for configuring SQL Server table partitioning for large-scale data management.
/// Partitioning enables efficient management of very large tables by distributing data across multiple filegroups,
/// providing parallel query execution, efficient maintenance operations, and fast archival capabilities.
/// </summary>
public static class PartitionConfigurationExtensions
{
    /// <summary>
    /// Configures table partitioning for large-scale data management.
    /// Enables parallel query execution and maintenance operations by distributing data across multiple filegroups.
    /// </summary>
    /// <typeparam name="T">The entity type to be partitioned.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="partitionScheme">The name of the partition scheme to use for data distribution.</param>
    /// <param name="partitionKey">Expression identifying the column to use as the partition key.</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Table partitioning provides several key benefits for large-scale data management:
    /// - **Parallel Processing**: Queries can execute in parallel across partitions
    /// - **Maintenance Efficiency**: Index maintenance and statistics updates can be performed per partition
    /// - **Query Performance**: Partition elimination improves query performance by scanning only relevant partitions
    /// - **Archival Operations**: Fast data archival through partition switching
    /// </para>
    /// <para>
    /// Before using this method, you must:
    /// 1. Create partition function with CreateSqlServerPartitionFunction()
    /// 2. Create partition scheme mapping function to filegroups
    /// 3. Ensure filegroups exist and are properly configured
    /// </para>
    /// <para>
    /// Partitioning is most effective for:
    /// - Tables larger than 2GB with predictable access patterns
    /// - Time-series data with date-based queries
    /// - Historical data requiring archival strategies
    /// - High-volume OLTP systems with range-based queries
    /// </para>
    /// <example>
    /// Example T-SQL partition setup:
    /// <code>
    /// -- Create partition function for monthly partitioning
    /// CREATE PARTITION FUNCTION pf_OrderDate (datetime2)
    /// AS RANGE RIGHT FOR VALUES 
    /// ('2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01');
    /// 
    /// -- Create partition scheme
    /// CREATE PARTITION SCHEME ps_Orders
    /// AS PARTITION pf_OrderDate
    /// TO (fg_2023, fg_2024_01, fg_2024_02, fg_2024_03, fg_2024_04);
    /// </code>
    /// 
    /// Example EF Core configuration:
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasSqlServerPartitionScheme("ps_Orders", o => o.OrderDate);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="partitionScheme"/>, or <paramref name="partitionKey"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="partitionScheme"/> is empty or whitespace.</exception>
    public static EntityTypeBuilder<T> HasSqlServerPartitionScheme<T>(
        this EntityTypeBuilder<T> builder,
        string partitionScheme,
        Expression<Func<T, object>> partitionKey) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (partitionScheme == null)
            throw new ArgumentNullException(nameof(partitionScheme));
        if (string.IsNullOrWhiteSpace(partitionScheme))
            throw new ArgumentException("Partition scheme name cannot be empty or whitespace.", nameof(partitionScheme));
        if (partitionKey == null)
            throw new ArgumentNullException(nameof(partitionKey));

        // Configure partition scheme annotation
        builder.HasAnnotation("SqlServer:PartitionScheme", partitionScheme);
        
        // Configure partition key column annotation
        var propertyName = GetPropertyName(partitionKey);
        builder.HasAnnotation("SqlServer:PartitionKey", propertyName);
        
        // Configure the partition key property for optimal indexing
        var keyProperty = builder.Property(partitionKey);
        keyProperty.HasAnnotation("SqlServer:IsPartitionKey", true);
        
        return builder;
    }

    /// <summary>
    /// Creates partition function for range-based partitioning.
    /// Distributes data across filegroups based on key ranges for optimal query performance and maintenance.
    /// </summary>
    /// <param name="builder">The model builder for the database context.</param>
    /// <param name="functionName">The name of the partition function to create.</param>
    /// <param name="dataType">The SQL Server data type for the partition key.</param>
    /// <param name="boundaryValues">The boundary values that define partition ranges.</param>
    /// <returns>The same model builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Partition functions define how data is distributed across partitions based on a partitioning column.
    /// SQL Server supports two partition function types:
    /// - **RANGE LEFT**: Values equal to boundary go to left partition
    /// - **RANGE RIGHT**: Values equal to boundary go to right partition (default)
    /// </para>
    /// <para>
    /// Boundary values must be:
    /// - Compatible with the specified data type
    /// - Ordered in ascending sequence
    /// - Unique within the function
    /// - Appropriate for expected data distribution
    /// </para>
    /// <para>
    /// Performance considerations:
    /// - Aim for balanced data distribution across partitions
    /// - Consider query patterns when defining boundaries
    /// - Account for data growth over time
    /// - Limit partitions to 1000 or fewer for optimal performance
    /// </para>
    /// <example>
    /// Example T-SQL generated:
    /// <code>
    /// CREATE PARTITION FUNCTION pf_SalesDate (datetime2)
    /// AS RANGE RIGHT FOR VALUES 
    /// ('2023-01-01', '2023-06-01', '2024-01-01', '2024-06-01');
    /// </code>
    /// 
    /// Example usage:
    /// <code>
    /// modelBuilder.CreateSqlServerPartitionFunction(
    ///     "pf_SalesDate",
    ///     SqlDbType.DateTime2,
    ///     new DateTime(2023, 1, 1),
    ///     new DateTime(2023, 6, 1),
    ///     new DateTime(2024, 1, 1),
    ///     new DateTime(2024, 6, 1));
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="functionName"/>, or <paramref name="boundaryValues"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="functionName"/> is empty or when boundary values are invalid.</exception>
    public static ModelBuilder CreateSqlServerPartitionFunction(
        this ModelBuilder builder,
        string functionName,
        SqlDbType dataType,
        params object[] boundaryValues)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (functionName == null)
            throw new ArgumentNullException(nameof(functionName));
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be empty or whitespace.", nameof(functionName));
        if (boundaryValues == null)
            throw new ArgumentNullException(nameof(boundaryValues));
        if (boundaryValues.Length == 0)
            throw new ArgumentException("At least one boundary value is required.", nameof(boundaryValues));

        // Validate boundary values are compatible with data type
        ValidateBoundaryValues(boundaryValues, dataType);

        // Store partition function configuration for migration generation
        var functionKey = $"SqlServer:PartitionFunction:{functionName}";
        var functionConfig = new PartitionFunctionConfiguration
        {
            FunctionName = functionName,
            DataType = dataType,
            BoundaryValues = boundaryValues,
            RangeType = PartitionRangeType.Right // Default to RANGE RIGHT
        };

        builder.HasAnnotation(functionKey, functionConfig);

        return builder;
    }

    /// <summary>
    /// Enables partition switching for fast data archival.
    /// Moves entire partitions between tables instantaneously using metadata operations.
    /// </summary>
    /// <typeparam name="T">The entity type for the source table.</typeparam>
    /// <param name="context">The database context for executing the partition switch.</param>
    /// <param name="partitionNumber">The partition number to switch (1-based).</param>
    /// <param name="targetTable">The name of the target table to receive the partition.</param>
    /// <remarks>
    /// <para>
    /// Partition switching is a metadata operation that moves data instantly by reassigning partition ownership.
    /// This enables extremely fast data archival and staging table operations.
    /// </para>
    /// <para>
    /// Prerequisites for successful partition switching:
    /// - Source and target tables must have identical schema structure
    /// - Both tables must use the same partition function and scheme
    /// - Target partition must be empty
    /// - All constraints and indexes must be aligned
    /// - Partition elimination must be possible for the switched partition
    /// </para>
    /// <para>
    /// Common use cases:
    /// - **Fast Archival**: Move old data to archive tables instantly
    /// - **Staging Operations**: Load data into staging partition then switch
    /// - **Data Purging**: Switch partition to temporary table then drop
    /// - **Maintenance Windows**: Minimize downtime for large data operations
    /// </para>
    /// <para>
    /// Performance benefits:
    /// - Instant data movement regardless of partition size
    /// - Minimal transaction log impact (metadata-only operation)
    /// - No blocking of concurrent operations on other partitions
    /// - Maintains referential integrity and constraints
    /// </para>
    /// <example>
    /// Example T-SQL generated:
    /// <code>
    /// ALTER TABLE Orders SWITCH PARTITION 1 TO ArchiveOrders PARTITION 1;
    /// </code>
    /// 
    /// Example usage for monthly archival:
    /// <code>
    /// // Switch January 2023 partition to archive table
    /// context.SwitchSqlServerPartition&lt;Order&gt;(1, "ArchiveOrders");
    /// 
    /// // Verify switch success
    /// var remainingCount = context.Orders
    ///     .Where(o => o.OrderDate >= new DateTime(2023, 1, 1) 
    ///              && o.OrderDate < new DateTime(2023, 2, 1))
    ///     .Count(); // Should be 0
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="targetTable"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="targetTable"/> is empty or when <paramref name="partitionNumber"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not configured for partitioning or when partition switching constraints are not met.</exception>
    public static async Task SwitchSqlServerPartition<T>(
        this DbContext context,
        int partitionNumber,
        string targetTable) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (targetTable == null)
            throw new ArgumentNullException(nameof(targetTable));
        if (string.IsNullOrWhiteSpace(targetTable))
            throw new ArgumentException("Target table name cannot be empty or whitespace.", nameof(targetTable));
        if (partitionNumber < 1)
            throw new ArgumentException("Partition number must be 1 or greater.", nameof(partitionNumber));

        // Get source table name from entity configuration
        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured in the context.");

        var sourceTable = entityType.GetTableName();
        if (string.IsNullOrEmpty(sourceTable))
            throw new InvalidOperationException($"Table name not found for entity type {typeof(T).Name}.");

        // Verify entity is configured for partitioning
        var partitionScheme = entityType.FindAnnotation("SqlServer:PartitionScheme")?.Value as string;
        if (string.IsNullOrEmpty(partitionScheme))
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for partitioning. Use HasSqlServerPartitionScheme() first.");

        // Generate partition switch command
        var switchCommand = $"ALTER TABLE [{sourceTable}] SWITCH PARTITION {partitionNumber} TO [{targetTable}] PARTITION {partitionNumber}";
        
        try
        {
            await context.Database.ExecuteSqlRawAsync(switchCommand);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"Failed to switch partition {partitionNumber} from {sourceTable} to {targetTable}. " +
                $"Ensure both tables have identical schema and the target partition is empty. " +
                $"SQL Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Configures sliding window partitioning for time-series data.
    /// Automatically manages partition creation and removal for continuous data archival.
    /// </summary>
    /// <typeparam name="T">The entity type for time-series data.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="windowSizeDays">The size of the sliding window in days (default: 30 days).</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Sliding window partitioning is ideal for time-series data where you need to maintain a fixed retention period
    /// while continuously adding new data and removing old data. This pattern is commonly used for:
    /// - Application logs and audit trails
    /// - IoT sensor data and telemetry
    /// - Financial transaction data
    /// - System monitoring and metrics data
    /// </para>
    /// <para>
    /// Implementation pattern:
    /// 1. **New Data**: Automatically creates new partitions for incoming data
    /// 2. **Active Window**: Maintains specified window size of accessible data
    /// 3. **Automatic Archival**: Switches old partitions to archive tables
    /// 4. **Cleanup**: Drops or purges partitions outside the retention window
    /// </para>
    /// <para>
    /// Benefits of sliding window partitioning:
    /// - **Predictable Performance**: Query performance remains consistent regardless of total data volume
    /// - **Automatic Maintenance**: Reduces manual intervention for data lifecycle management
    /// - **Storage Optimization**: Keeps working set size manageable
    /// - **Compliance**: Enables automated data retention policies
    /// </para>
    /// <para>
    /// Configuration requirements:
    /// - Entity must have a datetime-based partition key
    /// - Partition function should be configured for range-based partitioning
    /// - Archive tables should be pre-created with identical schema
    /// - Regular maintenance jobs should be scheduled for partition management
    /// </para>
    /// <example>
    /// Example configuration for 90-day sliding window:
    /// <code>
    /// modelBuilder.Entity&lt;ApplicationLog&gt;()
    ///     .HasSqlServerPartitionScheme("ps_ApplicationLogs", log => log.Timestamp)
    ///     .WithSqlServerSlidingWindow(90); // 90-day retention window
    /// </code>
    /// 
    /// Example T-SQL for automated partition management:
    /// <code>
    /// -- Create new partition for next month
    /// ALTER PARTITION SCHEME ps_ApplicationLogs NEXT USED [fg_2024_04];
    /// ALTER PARTITION FUNCTION pf_LogTimestamp() SPLIT RANGE ('2024-04-01');
    /// 
    /// -- Archive old partition (automated by sliding window)
    /// ALTER TABLE ApplicationLogs SWITCH PARTITION 1 TO ArchiveApplicationLogs PARTITION 1;
    /// ALTER PARTITION FUNCTION pf_LogTimestamp() MERGE RANGE ('2024-01-01');
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="windowSizeDays"/> is less than 1.</exception>
    public static EntityTypeBuilder<T> WithSqlServerSlidingWindow<T>(
        this EntityTypeBuilder<T> builder,
        int windowSizeDays = 30) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (windowSizeDays < 1)
            throw new ArgumentException("Window size must be at least 1 day.", nameof(windowSizeDays));

        // Verify entity is configured for partitioning
        var partitionScheme = builder.Metadata.FindAnnotation("SqlServer:PartitionScheme")?.Value as string;
        if (string.IsNullOrEmpty(partitionScheme))
            throw new InvalidOperationException($"Entity type {typeof(T).Name} must be configured with HasSqlServerPartitionScheme() before using sliding window.");

        // Configure sliding window settings
        builder.HasAnnotation("SqlServer:SlidingWindow:Enabled", true);
        builder.HasAnnotation("SqlServer:SlidingWindow:WindowSizeDays", windowSizeDays);
        builder.HasAnnotation("SqlServer:SlidingWindow:ArchiveTablePattern", $"Archive{typeof(T).Name}");
        
        // Configure automatic partition management settings
        var slidingWindowConfig = new SlidingWindowConfiguration
        {
            WindowSizeDays = windowSizeDays,
            PartitionScheme = partitionScheme,
            EnableAutomaticCreation = true,
            EnableAutomaticArchival = true,
            EnableAutomaticCleanup = true,
            MaintenanceSchedule = "0 2 * * *" // Daily at 2 AM
        };
        
        builder.HasAnnotation("SqlServer:SlidingWindow:Configuration", slidingWindowConfig);
        
        return builder;
    }

    #region Helper Methods

    /// <summary>
    /// Extracts the property name from a lambda expression.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>The property name.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is not a valid property expression.</exception>
    private static string GetPropertyName<T>(Expression<Func<T, object>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression member)
        {
            return member.Member.Name;
        }
        
        if (propertyExpression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
        {
            return unaryMember.Member.Name;
        }
        
        throw new ArgumentException("Expression must be a property accessor.", nameof(propertyExpression));
    }

    /// <summary>
    /// Validates that boundary values are compatible with the specified SQL Server data type.
    /// </summary>
    /// <param name="boundaryValues">The boundary values to validate.</param>
    /// <param name="dataType">The target SQL Server data type.</param>
    /// <exception cref="ArgumentException">Thrown when boundary values are incompatible with the data type.</exception>
    private static void ValidateBoundaryValues(object[] boundaryValues, SqlDbType dataType)
    {
        foreach (var value in boundaryValues)
        {
            if (value == null)
                throw new ArgumentException("Boundary values cannot be null.", nameof(boundaryValues));

            var isValidType = dataType switch
            {
                SqlDbType.Int => value is int,
                SqlDbType.BigInt => value is long,
                SqlDbType.DateTime => value is DateTime,
                SqlDbType.DateTime2 => value is DateTime,
                SqlDbType.Date => value is DateTime or DateOnly,
                SqlDbType.UniqueIdentifier => value is Guid,
                SqlDbType.VarChar or SqlDbType.NVarChar => value is string,
                SqlDbType.Decimal => value is decimal,
                SqlDbType.Float => value is double,
                SqlDbType.Real => value is float,
                _ => false
            };

            if (!isValidType)
                throw new ArgumentException($"Boundary value '{value}' (type: {value.GetType().Name}) is not compatible with SQL Server data type {dataType}.", nameof(boundaryValues));
        }
    }

    #endregion

    #region Supporting Types

    /// <summary>
    /// Configuration for partition functions.
    /// </summary>
    private class PartitionFunctionConfiguration
    {
        public required string FunctionName { get; set; }
        public required SqlDbType DataType { get; set; }
        public required object[] BoundaryValues { get; set; }
        public PartitionRangeType RangeType { get; set; } = PartitionRangeType.Right;
    }

    /// <summary>
    /// Configuration for sliding window partitioning.
    /// </summary>
    private class SlidingWindowConfiguration
    {
        public required int WindowSizeDays { get; set; }
        public required string PartitionScheme { get; set; }
        public bool EnableAutomaticCreation { get; set; } = true;
        public bool EnableAutomaticArchival { get; set; } = true;
        public bool EnableAutomaticCleanup { get; set; } = true;
        public string MaintenanceSchedule { get; set; } = "0 2 * * *"; // Cron expression
    }

    /// <summary>
    /// Specifies the range type for partition functions.
    /// </summary>
    private enum PartitionRangeType
    {
        /// <summary>
        /// Values equal to boundary go to the left partition.
        /// </summary>
        Left,
        
        /// <summary>
        /// Values equal to boundary go to the right partition.
        /// </summary>
        Right
    }

    #endregion
}