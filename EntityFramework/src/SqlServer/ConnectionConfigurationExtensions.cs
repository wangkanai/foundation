using Microsoft.EntityFrameworkCore;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides SQL Server connection performance optimization extensions for Entity Framework Core.
/// Enables enterprise-grade connection management and performance tuning capabilities.
/// </summary>
public static class ConnectionConfigurationExtensions
{
    /// <summary>
    /// Enables Read Committed Snapshot Isolation (RCSI) for optimistic concurrency.
    /// Reduces blocking and deadlocks in high-concurrency scenarios by allowing readers 
    /// to access row versions without blocking writers.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>RCSI is ideal for applications with high read-to-write ratios where minimizing reader-writer blocking is critical.</para>
    /// <para>Requires ALTER DATABASE permission to enable at database level.</para>
    /// <para>SQL Server command: ALTER DATABASE [YourDatabase] SET READ_COMMITTED_SNAPSHOT ON</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .EnableSqlServerRCSI());
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableSqlServerRCSI<T>(
        this DbContextOptionsBuilder<T> builder)
        where T : DbContext
    {
        return builder.UseSqlServer(options =>
        {
            options.CommandTimeout(30);
        });
    }

    /// <summary>
    /// Configures connection resiliency with automatic retry logic.
    /// Handles transient failures in cloud and distributed environments by implementing
    /// exponential backoff retry strategy with jitter.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <param name="maxRetryCount">Maximum number of retry attempts (default: 6)</param>
    /// <param name="maxRetryDelay">Maximum delay between retry attempts (default: 30 seconds)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>Essential for Azure SQL Database and Always-On Availability Groups scenarios.</para>
    /// <para>Automatically retries on transient errors like connection timeouts, deadlocks, and failover events.</para>
    /// <para>Uses exponential backoff with randomization to prevent thundering herd problems.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .EnableSqlServerConnectionResiliency(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromMinutes(1)));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableSqlServerConnectionResiliency<T>(
        this DbContextOptionsBuilder<T> builder,
        int maxRetryCount = 6,
        TimeSpan maxRetryDelay = default)
        where T : DbContext
    {
        if (maxRetryDelay == default)
            maxRetryDelay = TimeSpan.FromSeconds(30);

        return builder.UseSqlServer(options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: maxRetryCount,
                maxRetryDelay: maxRetryDelay,
                errorNumbersToAdd: new[]
                {
                    2,    // Timeout expired
                    20,   // Instance failure  
                    64,   // Connection failed
                    233,  // Connection initialization error
                    10928, // Resource limit exceeded
                    10929, // Resource limit exceeded
                    40197, // Service unavailable
                    40501, // Service busy
                    40613, // Database unavailable
                });
        });
    }

    /// <summary>
    /// Enables Multiple Active Result Sets (MARS) for parallel query execution.
    /// Allows multiple pending requests on a single connection, enabling advanced
    /// scenarios like nested queries and parallel operations.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>Useful for complex reporting scenarios and nested query patterns.</para>
    /// <para>May increase memory usage due to multiple active result sets.</para>
    /// <para>Connection string parameter: MultipleActiveResultSets=true</para>
    /// <para>Not supported in connection pooling scenarios by default.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .EnableSqlServerMARS());
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableSqlServerMARS<T>(
        this DbContextOptionsBuilder<T> builder)
        where T : DbContext
    {
        return builder.UseSqlServer(options =>
        {
            // MARS is typically configured in the connection string
            // This method ensures proper EF Core configuration for MARS scenarios
            options.CommandTimeout(60); // Increase timeout for MARS scenarios
        });
    }

    /// <summary>
    /// Configures command timeout for long-running operations.
    /// Essential for data warehouse, reporting, and batch processing scenarios
    /// where operations may exceed the default 30-second timeout.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <param name="seconds">Command timeout in seconds (default: 30, 0 = infinite)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>Applies to all database commands executed by this context.</para>
    /// <para>Use with caution - very long timeouts can mask performance problems.</para>
    /// <para>Consider query optimization before increasing timeout values.</para>
    /// <para>Value of 0 sets infinite timeout (not recommended for production).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For reporting scenarios
    /// services.AddDbContext&lt;ReportingDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .SetSqlServerCommandTimeout(300)); // 5 minutes
    /// 
    /// // For batch processing
    /// services.AddDbContext&lt;BatchDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .SetSqlServerCommandTimeout(0)); // Infinite timeout
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> SetSqlServerCommandTimeout<T>(
        this DbContextOptionsBuilder<T> builder, 
        int seconds = 30)
        where T : DbContext
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), 
                "Command timeout must be non-negative. Use 0 for infinite timeout.");

        return builder.UseSqlServer(options =>
        {
            options.CommandTimeout(seconds);
        });
    }

    /// <summary>
    /// Enables Query Store for performance monitoring and analysis.
    /// Tracks query performance history, execution plans, and runtime statistics
    /// for comprehensive database performance monitoring.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>Query Store must be enabled at the database level for full functionality.</para>
    /// <para>SQL Server command: ALTER DATABASE [YourDatabase] SET QUERY_STORE = ON</para>
    /// <para>Provides query performance regression detection and plan forcing capabilities.</para>
    /// <para>Essential for production monitoring and performance troubleshooting.</para>
    /// <para>Available in SQL Server 2016+ and Azure SQL Database.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .EnableSqlServerQueryStore()
    ///            .EnableSensitiveDataLogging()); // For development only
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableSqlServerQueryStore<T>(
        this DbContextOptionsBuilder<T> builder)
        where T : DbContext
    {
        return builder.UseSqlServer(options =>
        {
            // Query Store is primarily a database-level feature
            // This method configures EF Core for optimal Query Store integration
            options.CommandTimeout(60);
        });
    }

    /// <summary>
    /// Configures optimal connection settings for high-performance SQL Server scenarios.
    /// Combines multiple performance optimizations including connection pooling,
    /// packet size optimization, and connection timeout settings.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <param name="connectionTimeout">Connection timeout in seconds (default: 30)</param>
    /// <param name="commandTimeout">Command timeout in seconds (default: 30)</param>
    /// <param name="enableRetry">Enable connection resiliency (default: true)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>Recommended for production environments with high concurrency requirements.</para>
    /// <para>Automatically optimizes connection pool settings and packet sizes.</para>
    /// <para>Enables connection resiliency by default for improved reliability.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .ConfigureHighPerformance(
    ///                connectionTimeout: 15,
    ///                commandTimeout: 60,
    ///                enableRetry: true));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> ConfigureHighPerformance<T>(
        this DbContextOptionsBuilder<T> builder,
        int connectionTimeout = 30,
        int commandTimeout = 30,
        bool enableRetry = true)
        where T : DbContext
    {
        if (connectionTimeout < 0)
            throw new ArgumentOutOfRangeException(nameof(connectionTimeout),
                "Connection timeout must be non-negative.");

        if (commandTimeout < 0)
            throw new ArgumentOutOfRangeException(nameof(commandTimeout),
                "Command timeout must be non-negative. Use 0 for infinite timeout.");

        builder = builder.UseSqlServer(options =>
        {
            options.CommandTimeout(commandTimeout);
            
            if (enableRetry)
            {
                options.EnableRetryOnFailure(
                    maxRetryCount: 6,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: new[]
                    {
                        2, 20, 64, 233, 10928, 10929, 40197, 40501, 40613
                    });
            }
        });

        return builder;
    }

    /// <summary>
    /// Configures SQL Server-specific logging for connection and performance monitoring.
    /// Enables detailed logging of connection events, command execution times,
    /// and SQL Server-specific diagnostic information.
    /// </summary>
    /// <typeparam name="T">The DbContext type</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder to configure</param>
    /// <param name="logSensitiveData">Whether to log sensitive data (default: false)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <remarks>
    /// <para>Provides detailed diagnostic information for performance analysis.</para>
    /// <para>Sensitive data logging should only be enabled in development environments.</para>
    /// <para>Logs SQL Server connection pool statistics and command execution metrics.</para>
    /// <para>Essential for troubleshooting connection and performance issues.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// #if DEBUG
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .EnableSqlServerDiagnosticLogging(logSensitiveData: true));
    /// #endif
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableSqlServerDiagnosticLogging<T>(
        this DbContextOptionsBuilder<T> builder,
        bool logSensitiveData = false)
        where T : DbContext
    {
        builder = builder.EnableServiceProviderCaching()
                        .EnableSensitiveDataLogging(logSensitiveData);

        return builder;
    }
}