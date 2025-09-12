// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Defines the durability types for memory-optimized tables.
/// </summary>
public enum DurabilityType
{
    /// <summary>
    /// Both schema and data are persisted to disk, providing full durability.
    /// Slower than schema-only but ensures data survives server restarts.
    /// </summary>
    SchemaAndData,
    
    /// <summary>
    /// Only schema is persisted, data is lost on server restart.
    /// Provides maximum performance for temporary or cache-like data.
    /// </summary>
    SchemaOnly
}

/// <summary>
/// Provides extension methods for configuring SQL Server In-Memory OLTP (memory-optimized) tables and features.
/// Memory-optimized tables provide lock-free data structures with C-like performance characteristics,
/// delivering up to 100x performance improvement for high-frequency OLTP scenarios.
/// </summary>
/// <remarks>
/// <para>
/// Memory-optimized tables store data entirely in memory and use optimistic concurrency control
/// to eliminate locks and latches. This results in extremely high throughput and low latency
/// for transactional workloads.
/// </para>
/// <para>
/// Key benefits:
/// - Lock-free data structures with optimistic concurrency
/// - Up to 100x performance improvement for hot data scenarios
/// - Elimination of blocking, latches, and spin locks
/// - Native compilation of stored procedures for additional performance
/// - Seamless integration with disk-based tables
/// </para>
/// <para>
/// Requirements:
/// - SQL Server 2014+ or Azure SQL Database
/// - Memory-optimized filegroup configured in database
/// - Sufficient memory allocation for table data
/// </para>
/// </remarks>
public static class MemoryOptimizedConfigurationExtensions
{
    /// <summary>
    /// Configures entity as memory-optimized table for extreme performance.
    /// Provides lock-free data structures and optimistic concurrency control for maximum throughput.
    /// </summary>
    /// <typeparam name="T">The entity type to configure as memory-optimized.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <param name="durability">The durability setting for the memory-optimized table.</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Memory-optimized tables eliminate locks, latches, and spin locks by using optimistic concurrency control.
    /// This provides extreme performance benefits for high-frequency OLTP scenarios such as:
    /// - Session state management
    /// - Real-time data ingestion
    /// - High-frequency trading systems
    /// - IoT telemetry collection
    /// - Gaming leaderboards and statistics
    /// </para>
    /// <para>
    /// Durability options:
    /// - SchemaAndData: Full durability with disk persistence (default)
    /// - SchemaOnly: Maximum performance, data lost on restart (for cache scenarios)
    /// </para>
    /// <para>
    /// Performance characteristics:
    /// - 30-100x improvement in throughput for point lookups
    /// - Sub-millisecond latency for simple operations
    /// - Linear scalability with CPU core count
    /// - No blocking between concurrent transactions
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// CREATE TABLE table_name (...) 
    /// WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
    /// </code>
    /// </para>
    /// <example>
    /// Configure a session state entity as memory-optimized:
    /// <code>
    /// modelBuilder.Entity&lt;SessionState&gt;()
    ///     .IsMemoryOptimized(DurabilityType.SchemaOnly) // Data lost on restart, maximum performance
    ///     .HasKey(s => s.SessionId);
    /// </code>
    /// 
    /// Configure a high-frequency transaction log as durable memory-optimized:
    /// <code>
    /// modelBuilder.Entity&lt;TransactionLog&gt;()
    ///     .IsMemoryOptimized(DurabilityType.SchemaAndData) // Full durability
    ///     .HasKey(t => t.TransactionId);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static EntityTypeBuilder<T> IsMemoryOptimized<T>(
        this EntityTypeBuilder<T> builder,
        DurabilityType durability = DurabilityType.SchemaAndData) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        // Configure memory-optimized table settings
        builder.HasAnnotation("SqlServer:MemoryOptimized", true);
        builder.HasAnnotation("SqlServer:Durability", durability);
        
        // Memory-optimized tables require a primary key
        builder.HasAnnotation("SqlServer:RequiresPrimaryKey", true);
        
        // Set table options based on durability
        var durabilityOption = durability == DurabilityType.SchemaAndData ? "SCHEMA_AND_DATA" : "SCHEMA_ONLY";
        builder.HasAnnotation("SqlServer:TableOptions", $"MEMORY_OPTIMIZED = ON, DURABILITY = {durabilityOption}");
        
        return builder;
    }

    /// <summary>
    /// Creates hash index for memory-optimized tables optimized for point lookups.
    /// Hash indexes provide O(1) performance for equality searches on memory-optimized tables.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="builder">The property builder for the property to index.</param>
    /// <param name="bucketCount">The number of hash buckets. Should be 1-2x the expected unique row count.</param>
    /// <returns>The same property builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Hash indexes are specifically designed for memory-optimized tables and provide
    /// constant-time O(1) performance for exact match queries. They are ideal for:
    /// - Primary key lookups
    /// - Foreign key relationships
    /// - Unique constraint enforcement
    /// - High-frequency point queries
    /// </para>
    /// <para>
    /// Bucket count considerations:
    /// - Set to 1-2x the expected number of unique values
    /// - Powers of 2 provide optimal distribution
    /// - Too few buckets cause hash collisions and performance degradation
    /// - Too many buckets waste memory
    /// </para>
    /// <para>
    /// Performance benefits:
    /// - O(1) lookup time regardless of table size
    /// - No CPU overhead for traversing index structures
    /// - Optimal for high-concurrency scenarios
    /// - Lock-free access with optimistic concurrency
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// INDEX ix_column_hash HASH (column_name) WITH (BUCKET_COUNT = 1024)
    /// </code>
    /// </para>
    /// <example>
    /// Configure hash index for primary key lookup:
    /// <code>
    /// modelBuilder.Entity&lt;SessionState&gt;()
    ///     .Property(s => s.SessionId)
    ///     .HasSqlServerHashIndex(bucketCount: 16384); // For ~10K expected sessions
    /// </code>
    /// 
    /// Configure hash index for user lookup:
    /// <code>
    /// modelBuilder.Entity&lt;UserSession&gt;()
    ///     .Property(u => u.UserId)
    ///     .HasSqlServerHashIndex(bucketCount: 2048); // For ~1K expected active users
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bucketCount"/> is less than 1.</exception>
    public static PropertyBuilder<T> HasSqlServerHashIndex<T>(
        this PropertyBuilder<T> builder,
        int bucketCount = 1024)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        
        if (bucketCount < 1)
            throw new ArgumentOutOfRangeException(nameof(bucketCount), "Bucket count must be greater than 0.");

        // Ensure bucket count is a power of 2 for optimal distribution
        var adjustedBucketCount = GetNextPowerOfTwo(bucketCount);
        
        // Configure hash index annotation
        builder.HasAnnotation("SqlServer:HashIndex", true);
        builder.HasAnnotation("SqlServer:HashBucketCount", adjustedBucketCount);
        builder.HasAnnotation("SqlServer:IndexType", "HASH");
        
        return builder;
    }

    /// <summary>
    /// Creates natively compiled stored procedure support for memory-optimized tables.
    /// Provides C-like performance for data access operations with compiled execution plans.
    /// </summary>
    /// <typeparam name="T">The entity type for which to enable natively compiled procedures.</typeparam>
    /// <param name="builder">The entity type builder for the entity.</param>
    /// <returns>The same entity type builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Natively compiled stored procedures are compiled to native machine code, providing
    /// significant performance improvements over interpreted T-SQL:
    /// - 3-30x faster execution than regular stored procedures
    /// - Elimination of query plan compilation overhead
    /// - Optimized CPU instruction sequences
    /// - Direct memory access patterns
    /// </para>
    /// <para>
    /// Best use cases:
    /// - High-frequency CRUD operations
    /// - Real-time data processing
    /// - Microsecond-latency requirements
    /// - CPU-intensive business logic
    /// - Repetitive operations with predictable patterns
    /// </para>
    /// <para>
    /// Limitations:
    /// - Can only access memory-optimized tables
    /// - Limited T-SQL surface area support
    /// - No DDL operations allowed
    /// - No cross-database queries
    /// - Requires recompilation for schema changes
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// CREATE PROCEDURE dbo.GetSessionById(@SessionId UNIQUEIDENTIFIER)
    /// WITH NATIVE_COMPILATION, SCHEMABINDING
    /// AS BEGIN ATOMIC WITH (TRANSACTION_ISOLATION_LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    ///     SELECT * FROM dbo.SessionState WHERE SessionId = @SessionId;
    /// END
    /// </code>
    /// </para>
    /// <example>
    /// Configure entity for natively compiled procedure generation:
    /// <code>
    /// modelBuilder.Entity&lt;SessionState&gt;()
    ///     .IsMemoryOptimized()
    ///     .WithNativelyCompiledProcedures();
    /// </code>
    /// 
    /// This will generate procedures like:
    /// - GetSessionStateById (SELECT by PK)
    /// - InsertSessionState (INSERT)
    /// - UpdateSessionState (UPDATE)
    /// - DeleteSessionState (DELETE)
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static EntityTypeBuilder<T> WithNativelyCompiledProcedures<T>(
        this EntityTypeBuilder<T> builder) where T : class
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        // Configure native compilation support
        builder.HasAnnotation("SqlServer:NativelyCompiledProcedures", true);
        builder.HasAnnotation("SqlServer:GenerateNativeProcedures", true);
        
        // Set required options for natively compiled procedures
        builder.HasAnnotation("SqlServer:NativeCompilationOptions", new
        {
            TransactionIsolationLevel = "SNAPSHOT",
            Language = "us_english",
            SchemaBinding = true
        });
        
        // Generate standard CRUD procedures
        var entityName = typeof(T).Name;
        builder.HasAnnotation("SqlServer:NativeProcedureNames", new[]
        {
            $"Get{entityName}ById",
            $"Insert{entityName}",
            $"Update{entityName}",
            $"Delete{entityName}"
        });
        
        return builder;
    }

    /// <summary>
    /// Configures memory-optimized filegroup settings for database-level memory management.
    /// Sets up the container and checkpoint behavior for memory-optimized data persistence.
    /// </summary>
    /// <param name="builder">The model builder for the database context.</param>
    /// <param name="filegroupName">The name of the memory-optimized filegroup.</param>
    /// <param name="containerPath">The file system path for the memory-optimized container.</param>
    /// <returns>The same model builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Memory-optimized filegroups manage the persistence of durable memory-optimized tables.
    /// The filegroup contains one or more containers that store checkpoint file pairs:
    /// - Data files: Contain actual row data
    /// - Delta files: Contain information about deleted rows
    /// </para>
    /// <para>
    /// Configuration considerations:
    /// - Place containers on fast storage (SSD recommended)
    /// - Use multiple containers for parallelism
    /// - Monitor checkpoint file merge operations
    /// - Plan for growth based on data volume
    /// </para>
    /// <para>
    /// Performance impact:
    /// - Affects durability overhead for SCHEMA_AND_DATA tables
    /// - Checkpoint frequency impacts recovery time
    /// - Container count affects parallel loading performance
    /// - Storage performance impacts checkpoint operations
    /// </para>
    /// <para>
    /// SQL Server equivalent:
    /// <code>
    /// ALTER DATABASE database_name 
    /// ADD FILEGROUP filegroup_name CONTAINS MEMORY_OPTIMIZED_DATA;
    /// 
    /// ALTER DATABASE database_name 
    /// ADD FILE (NAME='container_name', FILENAME='path') 
    /// TO FILEGROUP filegroup_name;
    /// </code>
    /// </para>
    /// <example>
    /// Configure memory-optimized filegroup:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ConfigureSqlServerMemoryOptimizedFilegroup(
    ///         filegroupName: "MemoryOptimized_Data",
    ///         containerPath: @"C:\Data\MemoryOptimized");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filegroupName"/> or <paramref name="containerPath"/> is empty.</exception>
    public static ModelBuilder ConfigureSqlServerMemoryOptimizedFilegroup(
        this ModelBuilder builder,
        string filegroupName,
        string containerPath)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        
        if (string.IsNullOrWhiteSpace(filegroupName))
            throw new ArgumentException("Filegroup name cannot be null or empty.", nameof(filegroupName));
        
        if (string.IsNullOrWhiteSpace(containerPath))
            throw new ArgumentException("Container path cannot be null or empty.", nameof(containerPath));

        // Configure memory-optimized filegroup settings
        builder.HasAnnotation("SqlServer:MemoryOptimizedFilegroup", filegroupName);
        builder.HasAnnotation("SqlServer:MemoryOptimizedContainerPath", containerPath);
        
        // Set default checkpoint behavior for optimal performance
        builder.HasAnnotation("SqlServer:MemoryOptimizedSettings", new
        {
            FilegroupName = filegroupName,
            ContainerPath = containerPath,
            AutoMergeCheckpoint = true,
            MaxCheckpointFiles = 8,
            CheckpointFrequencyMinutes = 5
        });
        
        return builder;
    }

    /// <summary>
    /// Gets the next power of two that is greater than or equal to the specified value.
    /// Used for optimizing hash bucket counts.
    /// </summary>
    /// <param name="value">The input value.</param>
    /// <returns>The next power of two.</returns>
    private static int GetNextPowerOfTwo(int value)
    {
        if (value <= 0)
            return 1;
        
        // Handle powers of 2
        if ((value & (value - 1)) == 0)
            return value;
        
        // Find next power of 2
        int power = 1;
        while (power < value)
            power <<= 1;
        
        return power;
    }
}