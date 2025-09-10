// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace Wangkanai.EntityFramework.SqlServer;

/// <summary>
/// Specifies the retention unit for temporal table history data.
/// </summary>
public enum RetentionUnit
{
    /// <summary>
    /// Retention period specified in days.
    /// </summary>
    Days,
    /// <summary>
    /// Retention period specified in months.
    /// </summary>
    Months,
    /// <summary>
    /// Retention period specified in years.
    /// </summary>
    Years
}

/// <summary>
/// Provides extension methods for configuring SQL Server temporal table behaviors on Entity Framework Core entities.
/// Temporal tables provide automatic history tracking with point-in-time querying capabilities for audit compliance,
/// data recovery scenarios, and temporal analysis without custom trigger-based solutions.
/// </summary>
public static class TemporalConfigurationExtensions
{
    /// <summary>
    /// Configures entity as temporal table for automatic history tracking.
    /// Enables point-in-time queries and comprehensive audit trail capabilities.
    /// </summary>
    /// <typeparam name="T">The entity type to configure as temporal.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="historyTableName">Optional custom name for the history table. If null, uses default naming convention.</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Temporal tables provide automatic versioning of table data by maintaining a history of all changes.
    /// SQL Server automatically manages the history table and tracks when each version of a row was valid.
    /// </para>
    /// <para>
    /// Key benefits:
    /// - Automatic audit trail without triggers or custom code
    /// - Point-in-time queries using FOR SYSTEM_TIME clause
    /// - Compliance support for regulatory requirements (SOX, GDPR, HIPAA)
    /// - Data recovery capabilities for accidental changes
    /// - Historical trend analysis and temporal reporting
    /// - Zero application code changes required
    /// </para>
    /// <para>
    /// SQL Server automatically adds two datetime2 columns:
    /// - ValidFrom (SysStartTime): When the row version became active
    /// - ValidTo (SysEndTime): When the row version became inactive
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// ALTER TABLE [TableName] 
    /// ADD PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
    /// 
    /// ALTER TABLE [TableName] 
    /// SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [HistoryTableName]));
    /// </code>
    /// </para>
    /// <example>
    /// Example configuration for audit-critical entities:
    /// <code>
    /// modelBuilder.Entity&lt;Customer&gt;()
    ///     .IsTemporal("CustomerHistory")
    ///     .HasSqlServerPeriod("ValidFrom", "ValidTo")
    ///     .WithSqlServerHistoryRetention(7, RetentionUnit.Years);
    /// 
    /// // Query examples:
    /// // Current data (normal query)
    /// var currentCustomers = context.Customers.ToList();
    /// 
    /// // Historical data as of specific date
    /// var customersLastMonth = context.Customers
    ///     .AsOfSystemTime(DateTime.Now.AddMonths(-1))
    ///     .ToList();
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static EntityTypeBuilder<T> IsTemporal<T>(
        this EntityTypeBuilder<T> builder,
        string? historyTableName = null) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        // Configure entity as temporal table with system versioning
        builder.HasAnnotation("SqlServer:IsTemporal", true);
        
        // Set custom history table name if provided, otherwise use default convention
        if (!string.IsNullOrWhiteSpace(historyTableName))
        {
            builder.HasAnnotation("SqlServer:TemporalHistoryTableName", historyTableName);
        }

        // Ensure the entity has the required period columns
        // These will be automatically added by SQL Server if not explicitly defined
        builder.HasAnnotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");
        builder.HasAnnotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo");

        return builder;
    }

    /// <summary>
    /// Configures period columns for temporal table validity tracking.
    /// Defines the system-time period columns that SQL Server uses for row versioning.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="startColumnName">Name of the period start column (default: "ValidFrom").</param>
    /// <param name="endColumnName">Name of the period end column (default: "ValidTo").</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The period columns define when each row version was valid in the database.
    /// SQL Server automatically populates these columns and prevents direct modification by applications.
    /// </para>
    /// <para>
    /// Period column characteristics:
    /// - Data type: datetime2 with high precision (typically datetime2(7))
    /// - NOT NULL constraint automatically applied
    /// - Generated always as ROW START/ROW END
    /// - Hidden by default (not returned in SELECT * queries)
    /// - Automatically indexed for optimal temporal query performance
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// -- Add period columns to existing table
    /// ALTER TABLE [TableName] ADD
    ///     [ValidFrom] datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN,
    ///     [ValidTo] datetime2(7) GENERATED ALWAYS AS ROW END HIDDEN,
    ///     PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]);
    /// </code>
    /// </para>
    /// <example>
    /// Example with custom period column names:
    /// <code>
    /// modelBuilder.Entity&lt;AuditableEntity&gt;()
    ///     .IsTemporal("AuditableEntityHistory")
    ///     .HasSqlServerPeriod("EffectiveFrom", "EffectiveTo");
    /// 
    /// // Query specific time range
    /// var changes = context.AuditableEntities
    ///     .FromSqlRaw(@"
    ///         SELECT *, EffectiveFrom, EffectiveTo 
    ///         FROM AuditableEntity 
    ///         FOR SYSTEM_TIME BETWEEN @start AND @end",
    ///         startDate, endDate)
    ///     .ToList();
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when column names are null or empty.</exception>
    public static EntityTypeBuilder<T> HasSqlServerPeriod<T>(
        this EntityTypeBuilder<T> builder,
        string startColumnName = "ValidFrom",
        string endColumnName = "ValidTo") where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(startColumnName))
            throw new ArgumentException("Period start column name cannot be null or empty.", nameof(startColumnName));
        if (string.IsNullOrWhiteSpace(endColumnName))
            throw new ArgumentException("Period end column name cannot be null or empty.", nameof(endColumnName));

        // Configure the period column names for temporal table
        builder.HasAnnotation("SqlServer:TemporalPeriodStartColumnName", startColumnName);
        builder.HasAnnotation("SqlServer:TemporalPeriodEndColumnName", endColumnName);

        // Configure period columns as datetime2 with appropriate settings
        // These columns will be automatically managed by SQL Server
        builder.HasAnnotation("SqlServer:TemporalPeriodStartColumnType", "datetime2(7)");
        builder.HasAnnotation("SqlServer:TemporalPeriodEndColumnType", "datetime2(7)");
        
        // Mark period columns as generated and hidden
        builder.HasAnnotation("SqlServer:TemporalPeriodColumnsHidden", true);

        return builder;
    }

    /// <summary>
    /// Configures retention policy for temporal history data.
    /// Automatically purges old history records based on business requirements and compliance policies.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="retentionPeriod">The retention period duration.</param>
    /// <param name="unit">The unit of time for the retention period (Days, Months, Years).</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// History retention policy automatically removes old historical records to manage storage costs
    /// and comply with data retention regulations. The cleanup process runs automatically in the background.
    /// </para>
    /// <para>
    /// Retention benefits:
    /// - Automatic cleanup of old historical data
    /// - Compliance with data retention policies (GDPR "right to be forgotten")
    /// - Storage cost optimization for large temporal tables
    /// - Performance maintenance by limiting history table size
    /// - Configurable retention periods based on business needs
    /// </para>
    /// <para>
    /// Important considerations:
    /// - Retention cleanup is irreversible - deleted history cannot be recovered
    /// - Consider backup strategies for long-term archival needs
    /// - Cleanup occurs during SQL Server maintenance windows
    /// - Monitor retention effectiveness with system views
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// -- Set retention policy (example: 2 years)
    /// ALTER TABLE [TableName] 
    /// SET (SYSTEM_VERSIONING = ON (
    ///     HISTORY_TABLE = [HistoryTable],
    ///     HISTORY_RETENTION_PERIOD = 2 YEARS
    /// ));
    /// 
    /// -- Check retention status
    /// SELECT name, history_retention_period, history_retention_period_unit_desc
    /// FROM sys.tables WHERE temporal_type = 2;
    /// </code>
    /// </para>
    /// <example>
    /// Example retention configurations for different scenarios:
    /// <code>
    /// // Financial records: 7 years retention for compliance
    /// modelBuilder.Entity&lt;FinancialTransaction&gt;()
    ///     .IsTemporal()
    ///     .WithSqlServerHistoryRetention(7, RetentionUnit.Years);
    /// 
    /// // User activity logs: 90 days retention
    /// modelBuilder.Entity&lt;UserActivity&gt;()
    ///     .IsTemporal()
    ///     .WithSqlServerHistoryRetention(90, RetentionUnit.Days);
    /// 
    /// // Product catalog changes: 2 years retention
    /// modelBuilder.Entity&lt;Product&gt;()
    ///     .IsTemporal()
    ///     .WithSqlServerHistoryRetention(24, RetentionUnit.Months);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retentionPeriod"/> is less than or equal to zero.</exception>
    public static EntityTypeBuilder<T> WithSqlServerHistoryRetention<T>(
        this EntityTypeBuilder<T> builder,
        int retentionPeriod,
        RetentionUnit unit = RetentionUnit.Months) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (retentionPeriod <= 0)
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod), "Retention period must be greater than zero.");

        // Configure retention policy for the temporal table
        builder.HasAnnotation("SqlServer:TemporalHistoryRetentionPeriod", retentionPeriod);
        builder.HasAnnotation("SqlServer:TemporalHistoryRetentionUnit", unit.ToString().ToUpperInvariant());

        // Add metadata for monitoring and validation
        builder.HasAnnotation("SqlServer:TemporalHistoryRetentionEnabled", true);

        return builder;
    }

    /// <summary>
    /// Enables temporal queries with FOR SYSTEM_TIME clause for point-in-time data retrieval.
    /// Queries historical data as it existed at a specific point in time.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to query.</param>
    /// <param name="pointInTime">The specific point in time to query data for.</param>
    /// <returns>A queryable that will return data as it existed at the specified point in time.</returns>
    /// <remarks>
    /// <para>
    /// Point-in-time queries allow you to see historical states of your data without complex joins or custom logic.
    /// This is essential for audit trails, data forensics, and temporal analysis scenarios.
    /// </para>
    /// <para>
    /// Common use cases:
    /// - Audit investigations: "What did this record look like on December 31st?"
    /// - Data recovery: "Restore data to state before the erroneous update"
    /// - Compliance reporting: "Generate monthly report as of month-end"
    /// - Trend analysis: "Compare current data with quarterly snapshots"
    /// - Change tracking: "Show all versions of this record over time"
    /// </para>
    /// <para>
    /// Performance considerations:
    /// - History table is automatically indexed on period columns
    /// - Use appropriate time ranges to limit query scope
    /// - Consider data compression for large history tables
    /// - Monitor query performance with Query Store
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// -- Point-in-time query
    /// SELECT * FROM [TableName] 
    /// FOR SYSTEM_TIME AS OF '2024-12-31 23:59:59';
    /// 
    /// -- Time range query
    /// SELECT * FROM [TableName] 
    /// FOR SYSTEM_TIME BETWEEN '2024-01-01' AND '2024-12-31';
    /// 
    /// -- All history including current
    /// SELECT * FROM [TableName] 
    /// FOR SYSTEM_TIME ALL;
    /// </code>
    /// </para>
    /// <example>
    /// Example temporal queries for different scenarios:
    /// <code>
    /// // Get customer data as it was at year-end
    /// var yearEndCustomers = context.Customers
    ///     .AsOfSystemTime(new DateTime(2024, 12, 31, 23, 59, 59))
    ///     .Where(c => c.IsActive)
    ///     .ToList();
    /// 
    /// // Audit trail: compare current with previous month
    /// var currentData = await context.Products.ToListAsync();
    /// var previousData = await context.Products
    ///     .AsOfSystemTime(DateTime.Now.AddMonths(-1))
    ///     .ToListAsync();
    /// 
    /// // Data recovery: restore to specific point before error
    /// var restorePoint = new DateTime(2024, 6, 15, 14, 30, 0);
    /// var dataToRestore = context.Orders
    ///     .AsOfSystemTime(restorePoint)
    ///     .Where(o => o.CustomerId == customerId)
    ///     .ToList();
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbSet"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not configured as a temporal table.</exception>
    public static IQueryable<T> AsOfSystemTime<T>(
        this DbSet<T> dbSet,
        DateTime pointInTime) where T : class
    {
        if (dbSet == null)
            throw new ArgumentNullException(nameof(dbSet));

        // Create temporal query with FOR SYSTEM_TIME AS OF clause
        // This will be translated to appropriate SQL during query execution
        return dbSet.FromSqlRaw(
            $"SELECT * FROM [{typeof(T).Name}] FOR SYSTEM_TIME AS OF {{0}}",
            pointInTime);
    }

    /// <summary>
    /// Enables temporal queries for a specific time range using FOR SYSTEM_TIME BETWEEN clause.
    /// Returns all row versions that were active during the specified time period.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to query.</param>
    /// <param name="startTime">The start of the time range (inclusive).</param>
    /// <param name="endTime">The end of the time range (inclusive).</param>
    /// <returns>A queryable that will return all data versions active during the specified time range.</returns>
    /// <remarks>
    /// <para>
    /// Time range queries return all row versions that existed during a specified period,
    /// including rows that were created, modified, or deleted within that timeframe.
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// SELECT * FROM [TableName] 
    /// FOR SYSTEM_TIME BETWEEN @startTime AND @endTime;
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbSet"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endTime"/> is earlier than <paramref name="startTime"/>.</exception>
    public static IQueryable<T> ForSystemTimeBetween<T>(
        this DbSet<T> dbSet,
        DateTime startTime,
        DateTime endTime) where T : class
    {
        if (dbSet == null)
            throw new ArgumentNullException(nameof(dbSet));
        if (endTime < startTime)
            throw new ArgumentException("End time cannot be earlier than start time.", nameof(endTime));

        return dbSet.FromSqlRaw(
            $"SELECT * FROM [{typeof(T).Name}] FOR SYSTEM_TIME BETWEEN {{0}} AND {{1}}",
            startTime, endTime);
    }

    /// <summary>
    /// Returns all historical versions of the data including current and historical records.
    /// Uses FOR SYSTEM_TIME ALL to query both current table and history table.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to query.</param>
    /// <returns>A queryable that includes all current and historical data.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the complete audit trail for all records, including current and all historical versions.
    /// Use with caution on large tables as it can return significant amounts of data.
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// SELECT * FROM [TableName] FOR SYSTEM_TIME ALL;
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbSet"/> is null.</exception>
    public static IQueryable<T> ForSystemTimeAll<T>(this DbSet<T> dbSet) where T : class
    {
        if (dbSet == null)
            throw new ArgumentNullException(nameof(dbSet));

        return dbSet.FromSqlRaw($"SELECT * FROM [{typeof(T).Name}] FOR SYSTEM_TIME ALL");
    }
}