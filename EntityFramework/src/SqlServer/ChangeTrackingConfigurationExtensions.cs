using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure;

namespace Wangkanai.EntityFramework.SqlServer;

/// <summary>
/// Extension methods for configuring SQL Server Change Tracking and Change Data Capture (CDC) features.
/// </summary>
/// <remarks>
/// Change Tracking provides lightweight change detection for sync scenarios without storing actual data.
/// Change Data Capture (CDC) provides comprehensive auditing by capturing full change data.
/// 
/// <para><strong>When to use Change Tracking:</strong></para>
/// <list type="bullet">
/// <item>Data synchronization scenarios</item>
/// <item>ETL processes requiring incremental updates</item>
/// <item>Lightweight change detection with minimal overhead</item>
/// <item>Applications that only need to know WHAT changed, not the actual data</item>
/// </list>
/// 
/// <para><strong>When to use Change Data Capture (CDC):</strong></para>
/// <list type="bullet">
/// <item>Comprehensive auditing requirements</item>
/// <item>Compliance scenarios requiring full data history</item>
/// <item>Data warehousing with complete change history</item>
/// <item>Applications that need both WHAT changed and the actual before/after data</item>
/// </list>
/// </remarks>
public static class ChangeTrackingConfigurationExtensions
{
    /// <summary>
    /// Enables SQL Server Change Tracking for the specified entity for synchronization scenarios.
    /// </summary>
    /// <typeparam name="T">The entity type to enable change tracking for.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="retentionDays">The number of days to retain change tracking information. Default is 2 days.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>Change Tracking is ideal for synchronization scenarios where you need to identify changed rows
    /// without the overhead of storing full change data. It provides:</para>
    /// <list type="bullet">
    /// <item>Minimal storage overhead (only tracking metadata)</item>
    /// <item>High performance for OLTP workloads</item>
    /// <item>Built-in cleanup of old tracking data</item>
    /// </list>
    /// 
    /// <para><strong>SQL Server Requirements:</strong></para>
    /// <list type="bullet">
    /// <item>SQL Server 2008 or later</item>
    /// <item>Database-level Change Tracking must be enabled</item>
    /// <item>Table must have a primary key</item>
    /// </list>
    /// 
    /// <para><strong>Example T-SQL generated:</strong></para>
    /// <code>
    /// -- Enable at database level (one-time setup)
    /// ALTER DATABASE [MyDatabase] SET CHANGE_TRACKING = ON 
    /// (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)
    /// 
    /// -- Enable for specific table
    /// ALTER TABLE [dbo].[Users] ENABLE CHANGE_TRACKING
    /// </code>
    /// 
    /// <para><strong>Usage example:</strong></para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .EnableSqlServerChangeTracking(retentionDays: 7);
    /// 
    /// // Query changes since last sync
    /// var changes = context.Users.GetSqlServerChanges(lastSyncVersion);
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when retentionDays is less than 1.</exception>
    public static EntityTypeBuilder<T> EnableSqlServerChangeTracking<T>(
        this EntityTypeBuilder<T> builder,
        int retentionDays = 2)
        where T : class
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retentionDays, 1);

        var entityType = builder.Metadata;
        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "dbo";

        // Store configuration for migration generation
        builder.HasAnnotation("SqlServer:ChangeTracking:Enabled", true);
        builder.HasAnnotation("SqlServer:ChangeTracking:RetentionDays", retentionDays);

        // Add database-level change tracking configuration
        builder.Metadata.Model.AddAnnotation("SqlServer:ChangeTracking:DatabaseEnabled", true);
        builder.Metadata.Model.AddAnnotation("SqlServer:ChangeTracking:DatabaseRetentionDays", retentionDays);
        builder.Metadata.Model.AddAnnotation("SqlServer:ChangeTracking:AutoCleanup", true);

        return builder;
    }

    /// <summary>
    /// Configures Change Data Capture (CDC) for comprehensive auditing of the specified entity.
    /// </summary>
    /// <typeparam name="T">The entity type to enable CDC for.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="instance">Optional capture instance configuration. If null, uses default settings.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>Change Data Capture provides comprehensive auditing by capturing all insert, update, and delete
    /// operations along with the actual data values. It's ideal for:</para>
    /// <list type="bullet">
    /// <item>Regulatory compliance and audit trails</item>
    /// <item>Data warehousing with full change history</item>
    /// <item>Forensic analysis of data changes</item>
    /// <item>Replication scenarios requiring complete change data</item>
    /// </list>
    /// 
    /// <para><strong>SQL Server Requirements:</strong></para>
    /// <list type="bullet">
    /// <item>SQL Server 2008 or later (Enterprise Edition recommended)</item>
    /// <item>SQL Server Agent must be running</item>
    /// <item>Database must have CDC enabled</item>
    /// <item>Requires sysadmin or db_owner permissions</item>
    /// </list>
    /// 
    /// <para><strong>Example T-SQL generated:</strong></para>
    /// <code>
    /// -- Enable CDC at database level (one-time setup)
    /// EXEC sys.sp_cdc_enable_db
    /// 
    /// -- Enable CDC for specific table
    /// EXEC sys.sp_cdc_enable_table
    ///     @source_schema = N'dbo',
    ///     @source_name = N'Users',
    ///     @role_name = NULL,
    ///     @capture_instance = N'dbo_Users',
    ///     @supports_net_changes = 1
    /// </code>
    /// 
    /// <para><strong>Change tables created:</strong></para>
    /// <list type="bullet">
    /// <item>cdc.dbo_Users_CT - Contains all change data</item>
    /// <item>cdc.change_tables - Metadata about CDC-enabled tables</item>
    /// <item>cdc.index_columns - Information about captured columns</item>
    /// </list>
    /// 
    /// <para><strong>Usage example:</strong></para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .EnableSqlServerCDC(new CaptureInstance
    ///     {
    ///         Name = "users_audit",
    ///         SupportsNetChanges = true,
    ///         RoleName = "cdc_readers"
    ///     });
    /// </code>
    /// </remarks>
    public static EntityTypeBuilder<T> EnableSqlServerCDC<T>(
        this EntityTypeBuilder<T> builder,
        CaptureInstance? instance = null)
        where T : class
    {
        var entityType = builder.Metadata;
        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "dbo";

        instance ??= new CaptureInstance();

        // Set default capture instance name if not specified
        if (string.IsNullOrEmpty(instance.Name))
        {
            instance.Name = $"{schema}_{tableName}";
        }

        // Store configuration for migration generation
        builder.HasAnnotation("SqlServer:CDC:Enabled", true);
        builder.HasAnnotation("SqlServer:CDC:CaptureInstance", instance.Name);
        builder.HasAnnotation("SqlServer:CDC:RoleName", instance.RoleName);
        builder.HasAnnotation("SqlServer:CDC:SupportsNetChanges", instance.SupportsNetChanges);
        builder.HasAnnotation("SqlServer:CDC:CapturedColumns", instance.CapturedColumns);

        // Add database-level CDC configuration
        builder.Metadata.Model.AddAnnotation("SqlServer:CDC:DatabaseEnabled", true);

        return builder;
    }

    /// <summary>
    /// Gets changes for the specified entity since the last synchronization point using Change Tracking.
    /// </summary>
    /// <typeparam name="T">The entity type to get changes for.</typeparam>
    /// <param name="dbSet">The database set for the entity.</param>
    /// <param name="lastSyncVersion">The last synchronization version obtained from previous sync operation.</param>
    /// <returns>An <see cref="IQueryable{T}"/> containing only the changed entities since the last sync.</returns>
    /// <remarks>
    /// <para>This method leverages SQL Server's CHANGETABLE function to efficiently retrieve only changed rows
    /// since the last synchronization point. It's optimized for ETL and synchronization scenarios.</para>
    /// 
    /// <para><strong>Change Operations Detected:</strong></para>
    /// <list type="bullet">
    /// <item><strong>I</strong> - Insert operations</item>
    /// <item><strong>U</strong> - Update operations</item>
    /// <item><strong>D</strong> - Delete operations (only primary key available)</item>
    /// </list>
    /// 
    /// <para><strong>Example T-SQL generated:</strong></para>
    /// <code>
    /// SELECT u.*, ct.SYS_CHANGE_VERSION, ct.SYS_CHANGE_OPERATION
    /// FROM [dbo].[Users] u
    /// RIGHT OUTER JOIN CHANGETABLE(CHANGES [dbo].[Users], @lastSyncVersion) ct
    ///     ON u.[Id] = ct.[Id]
    /// WHERE ct.SYS_CHANGE_VERSION > @lastSyncVersion
    /// ORDER BY ct.SYS_CHANGE_VERSION
    /// </code>
    /// 
    /// <para><strong>Usage example:</strong></para>
    /// <code>
    /// // Get current database version for next sync
    /// long currentVersion = context.Database.ExecuteScalarSql("SELECT CHANGE_TRACKING_CURRENT_VERSION()");
    /// 
    /// // Get changes since last sync
    /// var changes = await context.Users
    ///     .GetSqlServerChanges(lastSyncVersion)
    ///     .ToListAsync();
    /// 
    /// // Process changes and update lastSyncVersion for next operation
    /// foreach (var change in changes)
    /// {
    ///     // Process based on change.ChangeOperation (Insert, Update, Delete)
    ///     ProcessChange(change);
    /// }
    /// 
    /// // Store currentVersion as lastSyncVersion for next sync
    /// await UpdateSyncVersion(currentVersion);
    /// </code>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// <list type="bullet">
    /// <item>Results are automatically ordered by SYS_CHANGE_VERSION</item>
    /// <item>Deleted rows return only primary key values</item>
    /// <item>Use appropriate batch sizes for large change sets</item>
    /// <item>Consider using change tracking cleanup to manage retention</item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when Change Tracking is not enabled for the entity.</exception>
    public static IQueryable<T> GetSqlServerChanges<T>(
        this DbSet<T> dbSet,
        long lastSyncVersion)
        where T : class
    {
        var context = dbSet.GetDbContext();
        var entityType = context.Model.FindEntityType(typeof(T));
        
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured in the model.");

        var isChangeTrackingEnabled = entityType.FindAnnotation("SqlServer:ChangeTracking:Enabled")?.Value as bool? ?? false;
        if (!isChangeTrackingEnabled)
        {
            throw new InvalidOperationException(
                $"Change Tracking is not enabled for entity {typeof(T).Name}. " +
                $"Call EnableSqlServerChangeTracking() in your model configuration.");
        }

        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "dbo";
        var fullTableName = $"[{schema}].[{tableName}]";

        // Build raw SQL query using CHANGETABLE function
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey == null)
            throw new InvalidOperationException($"Entity {typeof(T).Name} must have a primary key to use Change Tracking.");

        var keyColumns = primaryKey.Properties.Select(p => p.GetColumnName()).ToList();
        var keyJoinConditions = keyColumns.Select(col => $"e.[{col}] = ct.[{col}]").ToList();

        var sql = $@"
            SELECT e.*, ct.SYS_CHANGE_VERSION, ct.SYS_CHANGE_OPERATION, ct.SYS_CHANGE_COLUMNS
            FROM {fullTableName} e
            RIGHT OUTER JOIN CHANGETABLE(CHANGES {fullTableName}, {{0}}) ct
                ON {string.Join(" AND ", keyJoinConditions)}
            WHERE ct.SYS_CHANGE_VERSION > {{0}}
            ORDER BY ct.SYS_CHANGE_VERSION";

        return dbSet.FromSqlRaw(sql, lastSyncVersion);
    }

    /// <summary>
    /// Configures automatic cleanup settings for Change Tracking to maintain optimal performance.
    /// </summary>
    /// <typeparam name="T">The entity type to configure cleanup for.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="autoCleanup">Whether to enable automatic cleanup. Default is true.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>Automatic cleanup is essential for maintaining Change Tracking performance in production
    /// environments. Without proper cleanup, the change tracking tables can grow indefinitely,
    /// causing performance degradation.</para>
    /// 
    /// <para><strong>Cleanup Process:</strong></para>
    /// <list type="bullet">
    /// <item>Automatically removes expired change tracking information</item>
    /// <item>Runs based on the retention period configured at database level</item>
    /// <item>Helps maintain consistent query performance</item>
    /// <item>Prevents unbounded growth of internal change tracking tables</item>
    /// </list>
    /// 
    /// <para><strong>Manual Cleanup Options:</strong></para>
    /// <code>
    /// -- Check change tracking retention settings
    /// SELECT name, retention_period, retention_period_units_desc, is_auto_cleanup_on
    /// FROM sys.change_tracking_databases
    /// 
    /// -- Manual cleanup (if auto-cleanup is disabled)
    /// EXEC sys.sp_flush_commit_table @flush_ts = @oldest_version_to_keep
    /// 
    /// -- Get minimum valid version for cleanup
    /// SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('dbo.Users'))
    /// </code>
    /// 
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item>Enable auto-cleanup in production environments</item>
    /// <item>Set retention period based on sync frequency</item>
    /// <item>Monitor change tracking table sizes</item>
    /// <item>Consider manual cleanup for high-volume scenarios</item>
    /// </list>
    /// 
    /// <para><strong>Usage example:</strong></para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .EnableSqlServerChangeTracking(retentionDays: 7)
    ///     .WithSqlServerChangeTrackingCleanup(autoCleanup: true);
    /// </code>
    /// </remarks>
    public static EntityTypeBuilder<T> WithSqlServerChangeTrackingCleanup<T>(
        this EntityTypeBuilder<T> builder,
        bool autoCleanup = true)
        where T : class
    {
        // Verify that Change Tracking is enabled
        var isChangeTrackingEnabled = builder.Metadata.FindAnnotation("SqlServer:ChangeTracking:Enabled")?.Value as bool? ?? false;
        if (!isChangeTrackingEnabled)
        {
            throw new InvalidOperationException(
                $"Change Tracking must be enabled before configuring cleanup. " +
                $"Call EnableSqlServerChangeTracking() first.");
        }

        builder.HasAnnotation("SqlServer:ChangeTracking:AutoCleanup", autoCleanup);

        // Update database-level cleanup setting
        builder.Metadata.Model.AddAnnotation("SqlServer:ChangeTracking:AutoCleanup", autoCleanup);

        return builder;
    }
}

/// <summary>
/// Configuration settings for SQL Server Change Data Capture (CDC) capture instances.
/// </summary>
/// <remarks>
/// A capture instance represents a specific configuration of CDC for a table. Multiple capture instances
/// can exist for a single table, allowing different capture configurations (e.g., different column sets,
/// different roles) for different purposes.
/// 
/// <para><strong>Capture Instance Naming:</strong></para>
/// <list type="bullet">
/// <item>Must be unique within the database</item>
/// <item>Limited to 100 characters</item>
/// <item>Should follow naming conventions for easy identification</item>
/// <item>Default format: [schema]_[table_name]</item>
/// </list>
/// </remarks>
public class CaptureInstance
{
    /// <summary>
    /// Gets or sets the name of the capture instance.
    /// </summary>
    /// <remarks>
    /// <para>The capture instance name is used to identify the CDC configuration and associated
    /// change tables. If not specified, defaults to [schema]_[table_name] format.</para>
    /// 
    /// <para><strong>Naming Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item>Use descriptive names for multiple capture instances</item>
    /// <item>Include purpose or department identifier if needed</item>
    /// <item>Example: "users_audit", "users_replication", "users_warehouse"</item>
    /// </list>
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database role name that controls access to change data.
    /// </summary>
    /// <remarks>
    /// <para>The role name specifies which database role is required to access the change data.
    /// If null, no role-based security is applied, and any user with appropriate permissions
    /// can access the change data.</para>
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// <list type="bullet">
    /// <item>Create specific roles for CDC access control</item>
    /// <item>Follow principle of least privilege</item>
    /// <item>Consider different roles for different capture instances</item>
    /// <item>Example roles: "cdc_readers", "audit_users", "replication_service"</item>
    /// </list>
    /// </remarks>
    public string? RoleName { get; set; }

    /// <summary>
    /// Gets or sets whether net changes tracking is supported for this capture instance.
    /// </summary>
    /// <remarks>
    /// <para>When enabled, CDC provides additional functions for querying net changes
    /// (cdc.fn_cdc_get_net_changes_[capture_instance]) which show only the final
    /// result of multiple changes to the same row within a given time period.</para>
    /// 
    /// <para><strong>Net Changes vs All Changes:</strong></para>
    /// <list type="bullet">
    /// <item><strong>Net Changes</strong>: Shows only the final state after all changes</item>
    /// <item><strong>All Changes</strong>: Shows every individual change operation</item>
    /// <item>Net changes are more efficient for synchronization scenarios</item>
    /// <item>All changes are better for complete audit trails</item>
    /// </list>
    /// 
    /// <para><strong>Example:</strong></para>
    /// <code>
    /// -- If a row is inserted, updated twice, then deleted:
    /// -- All Changes: INSERT, UPDATE, UPDATE, DELETE (4 records)
    /// -- Net Changes: DELETE (1 record, showing final state)
    /// </code>
    /// </remarks>
    public bool SupportsNetChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of column names to capture. If null or empty, captures all columns.
    /// </summary>
    /// <remarks>
    /// <para>By default, CDC captures all columns in the source table. You can specify a subset
    /// of columns to reduce storage requirements and improve performance when only certain
    /// columns are relevant for your auditing or replication needs.</para>
    /// 
    /// <para><strong>Column Selection Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item>Always include primary key columns (automatically included)</item>
    /// <item>Include audit-relevant columns for compliance</item>
    /// <item>Exclude large binary columns if not needed</item>
    /// <item>Consider security implications of sensitive data</item>
    /// </list>
    /// 
    /// <para><strong>Example:</strong></para>
    /// <code>
    /// new CaptureInstance
    /// {
    ///     Name = "users_security_audit",
    ///     CapturedColumns = new[] { "Id", "Username", "Email", "LastLoginDate", "IsActive" }
    /// };
    /// </code>
    /// </remarks>
    public string[]? CapturedColumns { get; set; }
}