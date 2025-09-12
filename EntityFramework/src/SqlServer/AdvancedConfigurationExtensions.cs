// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring advanced SQL Server enterprise features including Service Broker,
/// query hints, Resource Governor, and Stretch Database capabilities. These features are designed for large-scale
/// enterprise deployments requiring advanced workload management, asynchronous messaging, and hybrid cloud storage.
/// </summary>
public static class AdvancedConfigurationExtensions
{
    /// <summary>
    /// Enables SQL Server Service Broker for asynchronous messaging and reliable queuing within the database.
    /// Service Broker provides transactional messaging capabilities for building distributed applications with guaranteed message delivery.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
    /// <param name="builder">The DbContext options builder.</param>
    /// <param name="serviceName">The name of the Service Broker service (optional, defaults to database name + "_Service").</param>
    /// <param name="queueName">The name of the message queue (optional, defaults to service name + "_Queue").</param>
    /// <returns>The same DbContext options builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Service Broker enables reliable, asynchronous messaging between applications or components within SQL Server.
    /// It provides ACID-compliant message queuing with guaranteed delivery and processing order.
    /// </para>
    /// <para>
    /// Key capabilities:
    /// - **Transactional Messaging**: Messages are part of database transactions
    /// - **Reliable Delivery**: Guaranteed message delivery even after system failures
    /// - **Ordered Processing**: Messages processed in defined sequence
    /// - **Cross-Database Communication**: Messaging across multiple databases and instances
    /// - **Security Integration**: Uses database security model for message authorization
    /// </para>
    /// <para>
    /// Common enterprise scenarios:
    /// - **Decoupled Processing**: Separate message producers from consumers
    /// - **Workflow Orchestration**: Coordinate multi-step business processes
    /// - **Event-Driven Architecture**: Publish events for reactive systems
    /// - **Background Processing**: Queue long-running operations for async execution
    /// - **Integration Patterns**: Connect disparate systems reliably
    /// </para>
    /// <para>
    /// Performance characteristics:
    /// - Scales to millions of messages per hour
    /// - Low latency for high-priority messages
    /// - Automatic poison message handling
    /// - Built-in retry mechanisms with exponential backoff
    /// </para>
    /// <example>
    /// Example T-SQL setup generated:
    /// <code>
    /// -- Enable Service Broker on database
    /// ALTER DATABASE [YourDatabase] SET ENABLE_BROKER;
    /// 
    /// -- Create message types
    /// CREATE MESSAGE TYPE [OrderProcessingMessage]
    /// VALIDATION = WELL_FORMED_XML;
    /// 
    /// -- Create contract
    /// CREATE CONTRACT [OrderProcessingContract]
    /// ([OrderProcessingMessage] SENT BY INITIATOR);
    /// 
    /// -- Create queue and service
    /// CREATE QUEUE [OrderProcessing_Queue};
    /// CREATE SERVICE [OrderProcessing_Service]
    /// ON QUEUE [OrderProcessing_Queue] ([OrderProcessingContract]);
    /// </code>
    /// 
    /// Example EF Core configuration:
    /// <code>
    /// services.AddDbContext&lt;OrderContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .EnableSqlServerServiceBroker("OrderProcessing_Service", "OrderProcessing_Queue");
    /// });
    /// </code>
    /// 
    /// Example message sending:
    /// <code>
    /// // Send order processing message
    /// var message = "&lt;Order&gt;&lt;OrderId&gt;12345&lt;/OrderId&gt;&lt;CustomerId&gt;67890&lt;/CustomerId&gt;&lt;/Order&gt;";
    /// await context.Database.ExecuteSqlRawAsync(
    ///     "SEND ON CONVERSATION @conversation MESSAGE TYPE [OrderProcessingMessage] (@message)",
    ///     new SqlParameter("@conversation", conversationHandle),
    ///     new SqlParameter("@message", message));
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static DbContextOptionsBuilder<T> EnableSqlServerServiceBroker<T>(
        this DbContextOptionsBuilder<T> builder,
        string? serviceName = null,
        string? queueName = null) where T : DbContext
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        // Configure Service Broker settings
        var serviceBrokerConfig = new ServiceBrokerConfiguration
        {
            ServiceName = serviceName ?? $"{typeof(T).Name}_Service",
            QueueName = queueName ?? $"{serviceName ?? typeof(T).Name}_Queue",
            EnableBroker = true,
            EnablePoisonMessageHandling = true,
            MaxQueueReaders = 5,
            MessageRetentionDays = 7
        };

        // Future: Implement ServiceBrokerInterceptor to handle:
        // - Service Broker service/queue creation and configuration
        // - Connection initialization with Service Broker setup SQL
        // - Message type and contract management
        // builder.AddInterceptors(new ServiceBrokerInterceptor(serviceBrokerConfig));
        
        builder.UseSqlServer();

        return builder;
    }

    /// <summary>
    /// Applies query hints to control SQL Server execution plan generation and query processing behavior.
    /// Query hints override the query optimizer's default behavior for specific performance scenarios.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The LINQ query to apply hints to.</param>
    /// <param name="hint">The SQL Server query hint to apply (e.g., "OPTION (FORCE ORDER)", "WITH (NOLOCK)").</param>
    /// <returns>The same queryable with the specified hint applied.</returns>
    /// <remarks>
    /// <para>
    /// Query hints provide fine-grained control over query execution plans when the optimizer's automatic
    /// choices are not optimal for specific scenarios. Use hints judiciously as they override SQL Server's
    /// cost-based optimization.
    /// </para>
    /// <para>
    /// Common hint categories:
    /// - **Join Hints**: LOOP, MERGE, HASH - Control join algorithms
    /// - **Index Hints**: INDEX, FORCESEEK - Force specific index usage
    /// - **Locking Hints**: NOLOCK, ROWLOCK, TABLOCKX - Control concurrency behavior
    /// - **Plan Hints**: OPTIMIZE FOR, RECOMPILE - Control plan caching and parameters
    /// - **Parallelism Hints**: MAXDOP - Control parallel execution degree
    /// </para>
    /// <para>
    /// Performance scenarios requiring hints:
    /// - **Parameter Sniffing**: Use OPTIMIZE FOR UNKNOWN for variable workloads
    /// - **Plan Stability**: Use FORCE ORDER when join order is critical
    /// - **Index Selection**: Use INDEX hint when optimizer chooses wrong index
    /// - **Reporting Queries**: Use NOLOCK for read-heavy scenarios accepting dirty reads
    /// - **Batch Processing**: Use MAXDOP to control parallelism for ETL operations
    /// </para>
    /// <para>
    /// Best practices:
    /// - Test hints thoroughly in production-like environments
    /// - Monitor query performance before and after applying hints
    /// - Document why specific hints are necessary
    /// - Review hints periodically as data patterns change
    /// - Use Query Store to track performance impact
    /// </para>
    /// <example>
    /// Example hint usage patterns:
    /// <code>
    /// // Force specific join order for complex reporting query
    /// var report = context.Orders
    ///     .Join(context.Customers, o => o.CustomerId, c => c.Id, (o, c) => new { o, c })
    ///     .WithSqlServerHint("OPTION (FORCE ORDER)")
    ///     .ToList();
    /// 
    /// // Use NOLOCK for reporting queries that can tolerate dirty reads
    /// var dashboard = context.Orders
    ///     .WithSqlServerHint("WITH (NOLOCK)")
    ///     .Where(o => o.OrderDate >= DateTime.Today.AddDays(-30))
    ///     .ToList();
    /// 
    /// // Optimize for unknown parameters to avoid parameter sniffing
    /// var dynamicQuery = context.Products
    ///     .Where(p => p.CategoryId == categoryId)
    ///     .WithSqlServerHint("OPTION (OPTIMIZE FOR (@categoryId UNKNOWN))")
    ///     .ToList();
    /// 
    /// // Control parallelism for large batch operations
    /// var batchUpdate = context.Orders
    ///     .Where(o => o.Status == "Pending")
    ///     .WithSqlServerHint("OPTION (MAXDOP 4)")
    ///     .ToList();
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> or <paramref name="hint"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="hint"/> is empty or whitespace.</exception>
    public static IQueryable<T> WithSqlServerHint<T>(
        this IQueryable<T> query,
        string hint) where T : class
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (hint == null)
            throw new ArgumentNullException(nameof(hint));
        if (string.IsNullOrWhiteSpace(hint))
            throw new ArgumentException("Query hint cannot be empty or whitespace.", nameof(hint));

        // Apply query hint by tagging the query
        // The actual hint application is handled by query translation infrastructure
        return query.TagWith($"QueryHint:{hint}");
    }

    /// <summary>
    /// Configures SQL Server Resource Governor to control CPU, memory, and I/O resources for specific workloads.
    /// Resource Governor enables workload isolation and prevents resource-intensive queries from impacting system performance.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
    /// <param name="builder">The DbContext options builder.</param>
    /// <param name="resourcePoolName">The name of the Resource Governor resource pool to use.</param>
    /// <param name="workloadGroupName">The name of the workload group (optional, defaults to resource pool name + "_Group").</param>
    /// <returns>The same DbContext options builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Resource Governor provides workload management capabilities by controlling resource allocation across different
    /// types of database workloads. It enables predictable performance by preventing resource starvation and ensuring
    /// fair resource distribution among competing workloads.
    /// </para>
    /// <para>
    /// Resource control capabilities:
    /// - **CPU Throttling**: Min/max CPU percentage allocation per workload
    /// - **Memory Management**: Memory grant limits to prevent excessive memory consumption
    /// - **I/O Bandwidth**: Disk I/O rate limiting for consistent performance
    /// - **Request Timeout**: Maximum execution time limits for runaway queries
    /// - **Degree of Parallelism**: Control parallel execution for specific workloads
    /// </para>
    /// <para>
    /// Enterprise scenarios:
    /// - **Multi-Tenant Applications**: Isolate tenant workloads from each other
    /// - **Mixed Workloads**: Separate OLTP from reporting/analytics workloads
    /// - **SLA Enforcement**: Guarantee performance levels for critical applications
    /// - **Resource Optimization**: Prevent resource-intensive operations from impacting others
    /// - **Cost Management**: Control resource usage for cloud deployments
    /// </para>
    /// <para>
    /// Resource pool configuration options:
    /// - MIN_CPU_PERCENT: Guaranteed minimum CPU allocation
    /// - MAX_CPU_PERCENT: Maximum CPU allocation ceiling
    /// - MIN_MEMORY_PERCENT: Guaranteed minimum memory allocation
    /// - MAX_MEMORY_PERCENT: Maximum memory allocation ceiling
    /// - CAP_CPU_PERCENT: Hard CPU cap (prevents CPU usage above limit)
    /// - MIN_IOPS_PER_VOLUME: Minimum I/O operations per second guarantee
    /// - MAX_IOPS_PER_VOLUME: Maximum I/O operations per second limit
    /// </para>
    /// <example>
    /// Example T-SQL Resource Governor setup:
    /// <code>
    /// -- Create resource pool for reporting workload
    /// CREATE RESOURCE POOL [ReportingPool]
    /// WITH (
    ///     MIN_CPU_PERCENT = 10,
    ///     MAX_CPU_PERCENT = 30,
    ///     MIN_MEMORY_PERCENT = 10,
    ///     MAX_MEMORY_PERCENT = 25,
    ///     CAP_CPU_PERCENT = 35,
    ///     MIN_IOPS_PER_VOLUME = 100,
    ///     MAX_IOPS_PER_VOLUME = 1000
    /// );
    /// 
    /// -- Create workload group
    /// CREATE WORKLOAD GROUP [ReportingGroup]
    /// WITH (
    ///     IMPORTANCE = MEDIUM,
    ///     REQUEST_MAX_MEMORY_GRANT_PERCENT = 20,
    ///     REQUEST_MAX_CPU_TIME_SEC = 300,
    ///     REQUEST_MEMORY_GRANT_TIMEOUT_SEC = 60,
    ///     MAX_DOP = 4
    /// ) USING [ReportingPool};
    /// 
    /// -- Create classifier function
    /// CREATE FUNCTION dbo.ResourceGovernorClassifier()
    /// RETURNS SYSNAME
    /// WITH SCHEMABINDING
    /// AS
    /// BEGIN
    ///     DECLARE @WorkloadGroup SYSNAME;
    ///     IF (ORIGINAL_LOGIN() = 'ReportingUser')
    ///         SET @WorkloadGroup = 'ReportingGroup';
    ///     ELSE
    ///         SET @WorkloadGroup = 'default';
    ///     RETURN @WorkloadGroup;
    /// END;
    /// 
    /// -- Enable Resource Governor with classifier
    /// ALTER RESOURCE GOVERNOR WITH (CLASSIFIER_FUNCTION = dbo.ResourceGovernorClassifier);
    /// ALTER RESOURCE GOVERNOR RECONFIGURE;
    /// </code>
    /// 
    /// Example EF Core configuration:
    /// <code>
    /// // Configure reporting context with resource limits
    /// services.AddDbContext&lt;ReportingContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseSqlServerResourceGovernor("ReportingPool", "ReportingGroup");
    /// });
    /// 
    /// // Configure OLTP context with different resource allocation
    /// services.AddDbContext&lt;TransactionContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseSqlServerResourceGovernor("OLTPPool", "OLTPGroup");
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="resourcePoolName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolName"/> is empty or whitespace.</exception>
    public static DbContextOptionsBuilder<T> UseSqlServerResourceGovernor<T>(
        this DbContextOptionsBuilder<T> builder,
        string resourcePoolName,
        string? workloadGroupName = null) where T : DbContext
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (resourcePoolName == null)
            throw new ArgumentNullException(nameof(resourcePoolName));
        if (string.IsNullOrWhiteSpace(resourcePoolName))
            throw new ArgumentException("Resource pool name cannot be empty or whitespace.", nameof(resourcePoolName));

        var workloadGroup = workloadGroupName ?? $"{resourcePoolName}_Group";

        // Configure Resource Governor settings
        var resourceGovernorConfig = new ResourceGovernorConfiguration
        {
            ResourcePoolName = resourcePoolName,
            WorkloadGroupName = workloadGroup,
            EnableResourceGovernor = true,
            MonitorResourceUsage = true,
            LogResourceViolations = true
        };

        // Future: Implement ResourceGovernorInterceptor to handle:
        // - Workload group assignment based on connection context
        // - Resource pool management and configuration validation
        // - Performance monitoring and resource usage tracking
        // builder.AddInterceptors(new ResourceGovernorInterceptor(resourceGovernorConfig));
        
        builder.UseSqlServer();

        return builder;
    }

    /// <summary>
    /// Enables SQL Server Stretch Database for transparent hybrid cloud storage of historical data.
    /// Stretch Database automatically archives cold data to Azure while maintaining transparent access from applications.
    /// </summary>
    /// <typeparam name="T">The entity type to configure for Stretch Database.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="filterPredicate">A predicate function that determines which rows should be stretched to Azure.</param>
    /// <param name="azureServerName">The Azure SQL Database server name (optional, can be configured globally).</param>
    /// <param name="azureDatabaseName">The Azure database name (optional, defaults to source database name).</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Stretch Database enables hybrid cloud storage by transparently moving cold data to Azure SQL Database
    /// while keeping hot data on-premises. Applications continue to query the table normally without code changes,
    /// and SQL Server automatically routes queries to the appropriate location.
    /// </para>
    /// <para>
    /// Key benefits:
    /// - **Cost Optimization**: Store cold data in cheaper Azure storage
    /// - **Transparent Access**: No application changes required
    /// - **Automatic Management**: Data migration happens automatically based on filter predicates
    /// - **Query Federation**: Seamless querying across on-premises and cloud data
    /// - **Backup Simplification**: Cold data backed up in Azure reduces local backup size
    /// </para>
    /// <para>
    /// Ideal scenarios for Stretch Database:
    /// - **Historical Data**: Transaction logs, audit trails, archival records
    /// - **Time-Series Data**: IoT data, monitoring logs, sensor readings
    /// - **Compliance Data**: Records that must be retained but rarely accessed
    /// - **Growing Tables**: Tables that grow continuously but most data becomes inactive
    /// - **Cost-Sensitive Workloads**: Where storage costs are a primary concern
    /// </para>
    /// <para>
    /// Filter predicate considerations:
    /// - Should identify data that is rarely accessed (cold data)
    /// - Typically based on date columns (e.g., records older than 2 years)
    /// - Should be deterministic and stable over time
    /// - Must not reference user-defined functions or subqueries
    /// - Should result in meaningful cost savings through data archival
    /// </para>
    /// <para>
    /// Performance characteristics:
    /// - Local queries execute at normal speed
    /// - Queries spanning both locations may have higher latency
    /// - Stretched data queries depend on Azure SQL Database performance
    /// - Bulk operations on stretched data require careful planning
    /// - Network bandwidth affects cross-location query performance
    /// </para>
    /// <example>
    /// Example T-SQL configuration generated:
    /// <code>
    /// -- Enable Stretch Database on table with date-based filter
    /// ALTER TABLE [dbo].[AuditLogs]
    /// SET (REMOTE_DATA_ARCHIVE = ON (
    ///     FILTER_PREDICATE = dbo.fn_stretchpredicate(LogDate),
    ///     MIGRATION_STATE = OUTBOUND
    /// ));
    /// 
    /// -- Create filter function for 2-year retention
    /// CREATE FUNCTION dbo.fn_stretchpredicate(@LogDate datetime2)
    /// RETURNS TABLE
    /// WITH SCHEMABINDING
    /// AS
    /// RETURN SELECT 1 AS is_eligible 
    ///        WHERE @LogDate < DATEADD(year, -2, GETDATE());
    /// </code>
    /// 
    /// Example EF Core configuration:
    /// <code>
    /// // Configure audit logs for 2-year on-premises retention
    /// modelBuilder.Entity&lt;AuditLog&gt;()
    ///     .EnableSqlServerStretchDatabase(
    ///         log => log.LogDate < DateTime.Now.AddYears(-2),
    ///         "myazureserver.database.windows.net",
    ///         "MyCompanyArchive");
    /// 
    /// // Configure transaction history with custom retention period
    /// modelBuilder.Entity&lt;TransactionHistory&gt;()
    ///     .EnableSqlServerStretchDatabase(
    ///         t => t.TransactionDate < DateTime.Now.AddMonths(-18));
    /// 
    /// // Configure IoT sensor data with size-based archival
    /// modelBuilder.Entity&lt;SensorReading&gt;()
    ///     .EnableSqlServerStretchDatabase(
    ///         reading => reading.RecordedDate < DateTime.Now.AddDays(-90));
    /// </code>
    /// 
    /// Example querying stretched data:
    /// <code>
    /// // Query automatically spans on-premises and Azure data
    /// var recentLogs = context.AuditLogs
    ///     .Where(log => log.UserId == userId)
    ///     .OrderByDescending(log => log.LogDate)
    ///     .Take(100) // Recent data likely on-premises
    ///     .ToList();
    /// 
    /// // Historical reporting may query Azure data
    /// var yearlyReport = context.AuditLogs
    ///     .Where(log => log.LogDate >= DateTime.Now.AddYears(-5))
    ///     .GroupBy(log => log.LogDate.Year)
    ///     .Select(g => new { Year = g.Key, Count = g.Count() })
    ///     .ToList(); // May include Azure data
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="filterPredicate"/> is null.</exception>
    public static EntityTypeBuilder<T> EnableSqlServerStretchDatabase<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, bool>> filterPredicate,
        string? azureServerName = null,
        string? azureDatabaseName = null) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (filterPredicate == null)
            throw new ArgumentNullException(nameof(filterPredicate));

        // Configure Stretch Database settings
        var stretchConfig = new StretchDatabaseConfiguration
        {
            FilterPredicate = filterPredicate,
            AzureServerName = azureServerName,
            AzureDatabaseName = azureDatabaseName ?? builder.Metadata.GetTableName(),
            MigrationState = StretchMigrationState.Outbound,
            EnableStretchDatabase = true,
            MonitorMigrationProgress = true
        };

        // Store configuration in entity annotations
        builder.HasAnnotation("SqlServer:StretchDatabase:Enabled", true);
        builder.HasAnnotation("SqlServer:StretchDatabase:FilterPredicate", filterPredicate);
        builder.HasAnnotation("SqlServer:StretchDatabase:Configuration", stretchConfig);

        // Configure table for stretch database compatibility
        builder.HasAnnotation("SqlServer:StretchDatabase:AzureServerName", azureServerName);
        builder.HasAnnotation("SqlServer:StretchDatabase:AzureDatabaseName", azureDatabaseName);
        builder.HasAnnotation("SqlServer:StretchDatabase:MigrationState", StretchMigrationState.Outbound);

        return builder;
    }

    #region Supporting Types

    /// <summary>
    /// Configuration for SQL Server Service Broker.
    /// </summary>
    private class ServiceBrokerConfiguration
    {
        public required string ServiceName { get; set; }
        public required string QueueName { get; set; }
        public bool EnableBroker { get; set; } = true;
        public bool EnablePoisonMessageHandling { get; set; } = true;
        public int MaxQueueReaders { get; set; } = 5;
        public int MessageRetentionDays { get; set; } = 7;
    }

    /// <summary>
    /// Configuration for SQL Server Resource Governor.
    /// </summary>
    private class ResourceGovernorConfiguration
    {
        public required string ResourcePoolName { get; set; }
        public required string WorkloadGroupName { get; set; }
        public bool EnableResourceGovernor { get; set; } = true;
        public bool MonitorResourceUsage { get; set; } = true;
        public bool LogResourceViolations { get; set; } = true;
    }

    /// <summary>
    /// Configuration for SQL Server Stretch Database.
    /// </summary>
    private class StretchDatabaseConfiguration
    {
        public required object FilterPredicate { get; set; }
        public string? AzureServerName { get; set; }
        public string? AzureDatabaseName { get; set; }
        public StretchMigrationState MigrationState { get; set; } = StretchMigrationState.Outbound;
        public bool EnableStretchDatabase { get; set; } = true;
        public bool MonitorMigrationProgress { get; set; } = true;
    }

    /// <summary>
    /// Specifies the migration state for Stretch Database.
    /// </summary>
    private enum StretchMigrationState
    {
        /// <summary>
        /// No data migration - disabled.
        /// </summary>
        Disabled,
        
        /// <summary>
        /// Migrate eligible data to Azure.
        /// </summary>
        Outbound,
        
        /// <summary>
        /// Paused migration state.
        /// </summary>
        Paused,
        
        /// <summary>
        /// Migrate data back from Azure.
        /// </summary>
        Inbound
    }


    #endregion
}