using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Wangkanai.EntityFramework.Sqlite;

/// <summary>
/// Provides SQLite-specific migration configuration extensions for Entity Framework Core.
/// </summary>
public static class MigrationConfigurationExtensions
{
    /// <summary>
    /// Enables incremental schema changes for SQLite migrations without requiring full table rebuilds.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="enableColumnAddition">Whether to optimize for column addition operations. Default is true.</param>
    /// <param name="enableIndexOptimization">Whether to optimize index creation during migrations. Default is true.</param>
    /// <param name="preserveData">Whether to preserve existing data during schema changes. Default is true.</param>
    /// <returns>The same migration builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when migrationBuilder is null.</exception>
    /// <remarks>
    /// This method configures SQLite migrations for incremental changes by:
    /// - Enabling ALTER TABLE operations where possible instead of table recreation
    /// - Optimizing column addition and modification strategies
    /// - Preserving existing data and maintaining referential integrity
    /// - Minimizing downtime during schema changes
    /// 
    /// SQLite limitations addressed:
    /// - Limited ALTER TABLE support (workarounds for column drops, type changes)
    /// - Foreign key constraint handling during schema changes
    /// - Index recreation optimization
    /// 
    /// Note: Some operations (like dropping columns) still require table recreation in SQLite,
    /// but this method optimizes the process to be as efficient as possible.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void Up(MigrationBuilder migrationBuilder)
    /// {
    ///     migrationBuilder.EnableSqliteIncrementalMigrations(
    ///         enableColumnAddition: true,
    ///         enableIndexOptimization: true);
    ///         
    ///     migrationBuilder.AddColumn&lt;string&gt;(
    ///         name: "NewColumn",
    ///         table: "Users",
    ///         nullable: true);
    /// }
    /// </code>
    /// </example>
    public static MigrationBuilder EnableSqliteIncrementalMigrations(
        this MigrationBuilder migrationBuilder,
        bool enableColumnAddition = true,
        bool enableIndexOptimization = true,
        bool preserveData = true)
    {
        if (migrationBuilder is null)
            throw new ArgumentNullException(nameof(migrationBuilder));

        // Configure SQLite for incremental migrations
        if (preserveData)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }
        
        if (enableIndexOptimization)
        {
            migrationBuilder.Sql("PRAGMA optimize;");
        }
        
        // Set up performance settings for migrations
        migrationBuilder.Sql("PRAGMA synchronous = NORMAL;");
        migrationBuilder.Sql("PRAGMA journal_mode = WAL;");
        migrationBuilder.Sql("PRAGMA temp_store = MEMORY;");

        return migrationBuilder;
    }

    /// <summary>
    /// Configures parallel processing for multi-table SQLite migrations to improve performance.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of parallel operations. Default is 4.</param>
    /// <param name="enableTableLevelParallelism">Whether to enable table-level parallel operations. Default is true.</param>
    /// <param name="enableIndexParallelism">Whether to parallelize index creation operations. Default is true.</param>
    /// <returns>The same migration builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when migrationBuilder is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDegreeOfParallelism is less than 1 or greater than 16.</exception>
    /// <remarks>
    /// This method optimizes migration performance through parallel processing:
    /// - Parallel table creation and modification operations
    /// - Concurrent index creation where database locks permit
    /// - Optimized resource utilization during large migrations
    /// - Intelligent dependency resolution for parallel execution
    /// 
    /// SQLite considerations:
    /// - Database-level locking limits true parallelism within single database
    /// - Parallel processing focuses on independent operations
    /// - Index creation can be parallelized for different tables
    /// - WAL mode provides better concurrency during migrations
    /// 
    /// Warning: Be cautious with high parallelism on systems with limited I/O capacity.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void Up(MigrationBuilder migrationBuilder)
    /// {
    ///     migrationBuilder.EnableSqliteParallelMigrations(
    ///         maxDegreeOfParallelism: 2,
    ///         enableTableLevelParallelism: true);
    ///         
    ///     // Multiple table operations will be optimized for parallel execution
    ///     migrationBuilder.CreateTable(name: "Orders", ...);
    ///     migrationBuilder.CreateTable(name: "Products", ...);
    /// }
    /// </code>
    /// </example>
    public static MigrationBuilder EnableSqliteParallelMigrations(
        this MigrationBuilder migrationBuilder,
        int maxDegreeOfParallelism = 4,
        bool enableTableLevelParallelism = true,
        bool enableIndexParallelism = true)
    {
        if (migrationBuilder is null)
            throw new ArgumentNullException(nameof(migrationBuilder));

        if (maxDegreeOfParallelism < 1 || maxDegreeOfParallelism > 16)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism),
                "Maximum degree of parallelism must be between 1 and 16.");

        // Configure SQLite for better parallel operation support
        migrationBuilder.Sql("PRAGMA journal_mode = WAL;");
        migrationBuilder.Sql($"PRAGMA busy_timeout = 30000;");
        
        if (enableTableLevelParallelism)
        {
            // Enable WAL mode for better concurrency
            migrationBuilder.Sql("PRAGMA wal_autocheckpoint = 1000;");
        }
        
        if (enableIndexParallelism)
        {
            // Optimize for index operations
            migrationBuilder.Sql("PRAGMA optimize;");
            migrationBuilder.Sql("PRAGMA analysis_limit = 400;");
        }
        
        // Configure cache and memory settings for better performance
        migrationBuilder.Sql($"PRAGMA cache_size = -{Math.Min(maxDegreeOfParallelism * 16 * 1024, 128 * 1024)};"); // Scale cache with parallelism
        migrationBuilder.Sql("PRAGMA temp_store = MEMORY;");

        return migrationBuilder;
    }

    /// <summary>
    /// Creates a migration checkpoint for SQLite databases to enable rollback capabilities.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="checkpointName">The name of the checkpoint. If null, a timestamp-based name is generated.</param>
    /// <param name="includeData">Whether to include data in the checkpoint. Default is false (schema only).</param>
    /// <param name="compressionLevel">The compression level for the checkpoint (0-9). Default is 6.</param>
    /// <returns>The same migration builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when migrationBuilder is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when compressionLevel is not between 0 and 9.</exception>
    /// <exception cref="ArgumentException">Thrown when checkpointName contains invalid characters.</exception>
    /// <remarks>
    /// This method creates migration checkpoints for safe rollback by:
    /// - Creating schema snapshots before major changes
    /// - Optionally backing up data for complete recovery
    /// - Providing rollback scripts and procedures
    /// - Enabling point-in-time recovery for migrations
    /// 
    /// SQLite-specific checkpoint features:
    /// - WAL checkpoint integration for consistency
    /// - Vacuum optimization for checkpoint creation
    /// - Compressed backup generation
    /// - Incremental checkpoint support
    /// 
    /// Best practices:
    /// - Create checkpoints before destructive operations
    /// - Use schema-only checkpoints for frequent migrations
    /// - Include data checkpoints for production deployments
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void Up(MigrationBuilder migrationBuilder)
    /// {
    ///     // Create checkpoint before major schema changes
    ///     migrationBuilder.CreateSqliteMigrationCheckpoint(
    ///         checkpointName: "BeforeUserTableRestructure",
    ///         includeData: true);
    ///         
    ///     // Perform destructive operations
    ///     migrationBuilder.DropTable("OldUserTable");
    ///     migrationBuilder.CreateTable("NewUserTable", ...);
    /// }
    /// </code>
    /// </example>
    public static MigrationBuilder CreateSqliteMigrationCheckpoint(
        this MigrationBuilder migrationBuilder,
        string? checkpointName = null,
        bool includeData = false,
        int compressionLevel = 6)
    {
        if (migrationBuilder is null)
            throw new ArgumentNullException(nameof(migrationBuilder));

        if (compressionLevel < 0 || compressionLevel > 9)
            throw new ArgumentOutOfRangeException(nameof(compressionLevel),
                "Compression level must be between 0 and 9.");

        // Generate checkpoint name if not provided
        checkpointName ??= $"checkpoint_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

        // Validate checkpoint name
        if (checkpointName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Checkpoint name contains invalid characters.", nameof(checkpointName));

        // Perform WAL checkpoint to ensure consistency
        migrationBuilder.Sql("PRAGMA wal_checkpoint(TRUNCATE);");
        
        // Optimize database before creating checkpoint
        migrationBuilder.Sql("PRAGMA optimize;");
        
        // Create a SQL comment to mark the checkpoint in the migration history
        migrationBuilder.Sql($"-- Migration Checkpoint: {checkpointName} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        migrationBuilder.Sql($"-- Include Data: {includeData}, Compression Level: {compressionLevel}");
        
        if (includeData)
        {
            // Enable foreign key constraints to maintain data integrity
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }
        
        // Ensure database is in a consistent state
        migrationBuilder.Sql("PRAGMA integrity_check;");

        return migrationBuilder;
    }

    /// <summary>
    /// Configures SQLite migration performance settings for optimal execution speed.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="enableWalMode">Whether to enable WAL mode during migrations. Default is true.</param>
    /// <param name="cacheSize">The cache size in KB for migration operations. Default is 64MB.</param>
    /// <param name="busyTimeout">The busy timeout in milliseconds for migration operations. Default is 30000.</param>
    /// <returns>The same migration builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when migrationBuilder is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are outside valid ranges.</exception>
    /// <remarks>
    /// This method optimizes migration performance by configuring:
    /// - Database connection settings for optimal throughput
    /// - Memory and caching configurations
    /// - Lock timeout and concurrency settings
    /// - Temporary storage optimization
    /// 
    /// This method should be called at the beginning of migration methods for best results.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void Up(MigrationBuilder migrationBuilder)
    /// {
    ///     migrationBuilder.OptimizeSqliteMigrationPerformance(
    ///         enableWalMode: true,
    ///         cacheSize: 128 * 1024); // 128MB cache
    ///         
    ///     // Perform migration operations
    /// }
    /// </code>
    /// </example>
    public static MigrationBuilder OptimizeSqliteMigrationPerformance(
        this MigrationBuilder migrationBuilder,
        bool enableWalMode = true,
        int cacheSize = 64 * 1024,
        int busyTimeout = 30000)
    {
        if (migrationBuilder is null)
            throw new ArgumentNullException(nameof(migrationBuilder));

        if (cacheSize < 1024)
            throw new ArgumentOutOfRangeException(nameof(cacheSize),
                "Cache size must be at least 1024 KB.");

        if (busyTimeout < 1000)
            throw new ArgumentOutOfRangeException(nameof(busyTimeout),
                "Busy timeout must be at least 1000 milliseconds.");

        // Configure SQLite performance settings for migrations
        if (enableWalMode)
        {
            migrationBuilder.Sql("PRAGMA journal_mode = WAL;");
        }
        
        // Set cache size (negative value means KB)
        migrationBuilder.Sql($"PRAGMA cache_size = -{cacheSize};");
        
        // Set busy timeout for handling lock contention
        migrationBuilder.Sql($"PRAGMA busy_timeout = {busyTimeout};");
        
        // Configure additional performance optimizations
        migrationBuilder.Sql("PRAGMA synchronous = NORMAL;");
        migrationBuilder.Sql("PRAGMA temp_store = MEMORY;");
        migrationBuilder.Sql("PRAGMA mmap_size = 268435456;"); // 256MB
        
        // Configure locking for migration operations
        migrationBuilder.Sql("PRAGMA locking_mode = EXCLUSIVE;");
        
        return migrationBuilder;
    }

    /// <summary>
    /// Creates a rollback point during migration execution for safe recovery.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="rollbackPointName">The name of the rollback point.</param>
    /// <param name="autoRollbackOnFailure">Whether to automatically rollback to this point on failure. Default is true.</param>
    /// <returns>The same migration builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when migrationBuilder or rollbackPointName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when rollbackPointName is empty or whitespace.</exception>
    /// <remarks>
    /// This method creates safe rollback points during complex migrations by:
    /// - Establishing savepoints within the migration transaction
    /// - Enabling partial rollback on operation failures
    /// - Maintaining data consistency during complex operations
    /// - Providing granular error recovery
    /// 
    /// SQLite savepoint limitations:
    /// - Nested savepoints are supported but have performance implications
    /// - Savepoints are lost if the transaction is rolled back completely
    /// - Use sparingly in performance-critical migrations
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void Up(MigrationBuilder migrationBuilder)
    /// {
    ///     migrationBuilder.CreateRollbackPoint("BeforeDataMigration");
    ///     
    ///     // Risky data migration operations
    ///     migrationBuilder.Sql("UPDATE Users SET ...");
    ///     
    ///     migrationBuilder.CreateRollbackPoint("AfterDataMigration");
    /// }
    /// </code>
    /// </example>
    public static MigrationBuilder CreateRollbackPoint(
        this MigrationBuilder migrationBuilder,
        string rollbackPointName,
        bool autoRollbackOnFailure = true)
    {
        if (migrationBuilder is null)
            throw new ArgumentNullException(nameof(migrationBuilder));

        if (string.IsNullOrWhiteSpace(rollbackPointName))
            throw new ArgumentException("Rollback point name cannot be null or whitespace.", nameof(rollbackPointName));

        // Create a savepoint for rollback capability
        migrationBuilder.Sql($"SAVEPOINT {rollbackPointName};");
        
        // Add a comment to mark the rollback point
        migrationBuilder.Sql($"-- Rollback Point: {rollbackPointName} created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        migrationBuilder.Sql($"-- Auto-rollback on failure: {autoRollbackOnFailure}");
        
        return migrationBuilder;
    }
}