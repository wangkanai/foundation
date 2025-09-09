using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Data;

namespace Wangkanai.EntityFramework.Sqlite;

/// <summary>
/// Provides SQLite-specific bulk operation configuration extensions for Entity Framework Core.
/// </summary>
public static class BulkConfigurationExtensions
{
    /// <summary>
    /// Optimizes entity configuration for SQLite bulk INSERT operations.
    /// </summary>
    /// <typeparam name="T">The entity type to optimize.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="batchSize">The batch size for bulk inserts. Default is 1000.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1 or greater than 10000.</exception>
    /// <remarks>
    /// This method configures the entity for optimal bulk INSERT performance in SQLite by:
    /// - Setting up proper indexing strategies for bulk operations
    /// - Configuring SQLite-specific pragma settings for performance
    /// - Optimizing batch processing parameters
    /// 
    /// Best practices:
    /// - Use batch sizes between 100-1000 for optimal memory usage
    /// - Consider transaction boundaries for large datasets
    /// - Monitor WAL file growth during bulk operations
    /// </remarks>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Product&gt;(entity =&gt;
    /// {
    ///     entity.OptimizeForSqliteBulkInserts(batchSize: 500);
    /// });
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> OptimizeForSqliteBulkInserts<T>(
        this EntityTypeBuilder<T> builder,
        int batchSize = 1000) where T : class
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (batchSize < 1 || batchSize > 10000)
            throw new ArgumentOutOfRangeException(nameof(batchSize), 
                "Batch size must be between 1 and 10000.");

        // Configure for bulk insert optimization
        builder.HasAnnotation("Sqlite:BulkInsert", true);
        builder.HasAnnotation("Sqlite:BulkInsertBatchSize", batchSize);
        
        // Optimize for sequential inserts
        builder.HasAnnotation("Sqlite:OptimizeSequentialInserts", true);
        
        // Configure SQLite-specific settings for bulk operations
        builder.HasAnnotation("Sqlite:SynchronousMode", "NORMAL");
        builder.HasAnnotation("Sqlite:JournalMode", "WAL");
        builder.HasAnnotation("Sqlite:CacheSize", -64000); // 64MB cache

        return builder;
    }

    /// <summary>
    /// Configures transaction settings for SQLite bulk operations with large data volumes.
    /// </summary>
    /// <typeparam name="T">The entity type to configure.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="isolationLevel">The transaction isolation level. Default is ReadCommitted.</param>
    /// <param name="commandTimeout">The command timeout in seconds. Default is 300 seconds.</param>
    /// <param name="enableDeferred">Whether to enable deferred constraint checking. Default is true.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when commandTimeout is less than 0.</exception>
    /// <remarks>
    /// This method optimizes transaction behavior for bulk operations by:
    /// - Configuring appropriate isolation levels for concurrent access
    /// - Setting optimal command timeouts for large operations
    /// - Enabling deferred constraint checking where beneficial
    /// - Optimizing lock escalation patterns
    /// 
    /// SQLite-specific considerations:
    /// - SQLite uses database-level locking, so isolation levels have limited effect
    /// - WAL mode provides better concurrency for read operations during bulk writes
    /// - Deferred constraints can improve bulk insert performance
    /// </remarks>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;(entity =&gt;
    /// {
    ///     entity.EnableSqliteBulkTransactions(
    ///         isolationLevel: IsolationLevel.ReadCommitted,
    ///         commandTimeout: 600);
    /// });
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> EnableSqliteBulkTransactions<T>(
        this EntityTypeBuilder<T> builder,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int commandTimeout = 300,
        bool enableDeferred = true) where T : class
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (commandTimeout < 0)
            throw new ArgumentOutOfRangeException(nameof(commandTimeout),
                "Command timeout must be non-negative.");

        // Configure transaction settings
        builder.HasAnnotation("Sqlite:BulkTransactionIsolation", isolationLevel.ToString());
        builder.HasAnnotation("Sqlite:BulkCommandTimeout", commandTimeout);
        builder.HasAnnotation("Sqlite:EnableDeferredConstraints", enableDeferred);
        
        // Optimize for bulk transaction performance
        builder.HasAnnotation("Sqlite:BulkTransactionMode", "IMMEDIATE");
        builder.HasAnnotation("Sqlite:BulkLockingMode", "EXCLUSIVE");
        
        // Configure connection pooling for bulk operations
        builder.HasAnnotation("Sqlite:BulkConnectionPooling", true);
        builder.HasAnnotation("Sqlite:BulkMaxPoolSize", 10);

        return builder;
    }

    /// <summary>
    /// Optimizes entity configuration for high-frequency SQLite UPDATE operations.
    /// </summary>
    /// <typeparam name="T">The entity type to optimize.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="enableRowLevelLocking">Whether to enable row-level locking optimization. Default is true.</param>
    /// <param name="optimizeIndexes">Whether to optimize indexes for update operations. Default is true.</param>
    /// <param name="batchSize">The batch size for bulk updates. Default is 500.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1 or greater than 5000.</exception>
    /// <remarks>
    /// This method optimizes UPDATE operations for SQLite by:
    /// - Configuring efficient index usage patterns for WHERE clauses
    /// - Optimizing row-level locking strategies (SQLite-specific adaptations)
    /// - Setting up proper batch processing for multiple updates
    /// - Minimizing index reorganization overhead
    /// 
    /// SQLite considerations:
    /// - SQLite doesn't have true row-level locking, but this optimizes for minimal lock contention
    /// - Index optimization focuses on covering indexes and query planning
    /// - Batch updates can reduce transaction overhead
    /// </remarks>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Inventory&gt;(entity =&gt;
    /// {
    ///     entity.OptimizeForSqliteBulkUpdates(
    ///         enableRowLevelLocking: true,
    ///         optimizeIndexes: true,
    ///         batchSize: 250);
    /// });
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> OptimizeForSqliteBulkUpdates<T>(
        this EntityTypeBuilder<T> builder,
        bool enableRowLevelLocking = true,
        bool optimizeIndexes = true,
        int batchSize = 500) where T : class
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (batchSize < 1 || batchSize > 5000)
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be between 1 and 5000.");

        // Configure bulk update optimization
        builder.HasAnnotation("Sqlite:BulkUpdate", true);
        builder.HasAnnotation("Sqlite:BulkUpdateBatchSize", batchSize);
        builder.HasAnnotation("Sqlite:OptimizeRowLocking", enableRowLevelLocking);
        builder.HasAnnotation("Sqlite:OptimizeUpdateIndexes", optimizeIndexes);
        
        // Configure SQLite-specific update optimizations
        builder.HasAnnotation("Sqlite:UpdateStrategy", "INDEXED");
        builder.HasAnnotation("Sqlite:EnableUpdateStatistics", true);
        
        // Optimize query planning for updates
        builder.HasAnnotation("Sqlite:UpdateQueryPlan", "COVERING");
        builder.HasAnnotation("Sqlite:MinimizeIndexReorganization", true);
        
        // Configure connection behavior for updates
        builder.HasAnnotation("Sqlite:UpdateTransactionBehavior", "IMMEDIATE");
        builder.HasAnnotation("Sqlite:UpdateConcurrencyMode", "OPTIMIZED");

        return builder;
    }

    /// <summary>
    /// Configures SQLite-specific settings for all bulk operations on an entity.
    /// </summary>
    /// <typeparam name="T">The entity type to configure.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="enableWalMode">Whether to enable WAL (Write-Ahead Logging) mode. Default is true.</param>
    /// <param name="pragmaSettings">Additional SQLite PRAGMA settings for bulk operations.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    /// <remarks>
    /// This method applies comprehensive bulk operation optimizations including:
    /// - WAL mode for better concurrency
    /// - Optimized cache and memory settings
    /// - Bulk-specific PRAGMA configurations
    /// - Performance monitoring setup
    /// 
    /// This method can be used in combination with specific bulk operation methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;LogEntry&gt;(entity =&gt;
    /// {
    ///     entity.ConfigureSqliteBulkOperations(
    ///         enableWalMode: true,
    ///         new Dictionary&lt;string, object&gt;
    ///         {
    ///             ["cache_size"] = -128000, // 128MB cache
    ///             ["temp_store"] = "MEMORY"
    ///         });
    /// });
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureSqliteBulkOperations<T>(
        this EntityTypeBuilder<T> builder,
        bool enableWalMode = true,
        IDictionary<string, object>? pragmaSettings = null) where T : class
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        // Enable bulk operations
        builder.HasAnnotation("Sqlite:BulkOperationsEnabled", true);
        builder.HasAnnotation("Sqlite:EnableWalMode", enableWalMode);
        
        // Set default pragma settings for bulk operations
        var defaultPragmas = new Dictionary<string, object>
        {
            ["synchronous"] = "NORMAL",
            ["cache_size"] = -64000,
            ["temp_store"] = "MEMORY",
            ["mmap_size"] = 268435456, // 256MB
            ["optimize"] = true
        };

        // Merge with user-provided settings
        if (pragmaSettings != null)
        {
            foreach (var setting in pragmaSettings)
            {
                defaultPragmas[setting.Key] = setting.Value;
            }
        }

        builder.HasAnnotation("Sqlite:BulkPragmaSettings", defaultPragmas);
        
        // Configure monitoring and statistics
        builder.HasAnnotation("Sqlite:EnableBulkStatistics", true);
        builder.HasAnnotation("Sqlite:BulkPerformanceMonitoring", true);

        return builder;
    }
}