// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring MySQL-specific bulk operations on Entity Framework Core entities.
/// These extensions leverage MySQL's native capabilities for high-performance bulk data operations.
/// </summary>
public static class BulkConfigurationExtensions
{
    /// <summary>
    /// Enables MySQL LOAD DATA INFILE for ultra-fast bulk inserts (20-100x performance improvement).
    /// LOAD DATA INFILE bypasses the MySQL SQL layer and directly loads data into storage engine.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="options">Configuration options for LOAD DATA INFILE operation.</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>LOAD DATA INFILE performance benefits:</para>
    /// <list type="bullet">
    /// <item><description>20-100x faster than individual INSERT statements</description></item>
    /// <item><description>Bypasses query parsing and optimization overhead</description></item>
    /// <item><description>Direct storage engine data loading</description></item>
    /// <item><description>Minimal transaction log overhead</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// LOAD DATA LOCAL INFILE '/path/to/data.csv'
    /// INTO TABLE users
    /// FIELDS TERMINATED BY ',' 
    /// ENCLOSED BY '"' 
    /// LINES TERMINATED BY '\n'
    /// CHARACTER SET utf8mb4;
    /// </code>
    /// <para>Usage example:</para>
    /// <code>
    /// modelBuilder.Entity&lt;LogEntry&gt;()
    ///     .EnableMySqlBulkLoad(new LoadDataOptions 
    ///     { 
    ///         FieldTerminator = "\t",
    ///         Local = true,
    ///         CharacterSet = "utf8mb4"
    ///     });
    /// </code>
    /// </remarks>
    public static EntityTypeBuilder<T> EnableMySqlBulkLoad<T>(
        this EntityTypeBuilder<T> builder,
        LoadDataOptions? options = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        options ??= new LoadDataOptions();
        
        builder.HasAnnotation("MySql:BulkLoad:Enabled", true);
        builder.HasAnnotation("MySql:BulkLoad:FieldTerminator", options.FieldTerminator);
        builder.HasAnnotation("MySql:BulkLoad:LineTerminator", options.LineTerminator);
        builder.HasAnnotation("MySql:BulkLoad:FieldEnclosure", options.FieldEnclosure);
        builder.HasAnnotation("MySql:BulkLoad:EscapeCharacter", options.EscapeCharacter);
        builder.HasAnnotation("MySql:BulkLoad:IgnoreLines", options.IgnoreLines);
        builder.HasAnnotation("MySql:BulkLoad:IgnoreLineCount", options.IgnoreLineCount);
        builder.HasAnnotation("MySql:BulkLoad:Local", options.Local);
        builder.HasAnnotation("MySql:BulkLoad:CharacterSet", options.CharacterSet);
        
        return builder;
    }

    /// <summary>
    /// Configures INSERT ... ON DUPLICATE KEY UPDATE for efficient upsert operations.
    /// This MySQL-specific syntax provides atomic insert-or-update functionality with high performance.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="updateColumns">Columns to update when a duplicate key is encountered.</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>ON DUPLICATE KEY UPDATE benefits:</para>
    /// <list type="bullet">
    /// <item><description>10-50x faster than SELECT + UPDATE/INSERT patterns</description></item>
    /// <item><description>Atomic operation - no race conditions</description></item>
    /// <item><description>Single round-trip to database</description></item>
    /// <item><description>Automatic handling of duplicate key scenarios</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// INSERT INTO products (id, name, price, updated_at)
    /// VALUES (1, 'Product A', 99.99, NOW())
    /// ON DUPLICATE KEY UPDATE 
    ///     price = VALUES(price),
    ///     updated_at = VALUES(updated_at);
    /// </code>
    /// <para>Usage example:</para>
    /// <code>
    /// modelBuilder.Entity&lt;Product&gt;()
    ///     .EnableMySqlUpsert(
    ///         p => p.Price, 
    ///         p => p.UpdatedAt, 
    ///         p => p.Description
    ///     );
    /// </code>
    /// </remarks>
    public static EntityTypeBuilder<T> EnableMySqlUpsert<T>(
        this EntityTypeBuilder<T> builder,
        params Expression<Func<T, object>>[] updateColumns) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(updateColumns);
        
        if (updateColumns.Length == 0)
            throw new ArgumentException("At least one update column must be specified.", nameof(updateColumns));
        
        builder.HasAnnotation("MySql:Upsert:Enabled", true);
        
        var columnNames = updateColumns.Select(GetPropertyName).ToArray();
        builder.HasAnnotation("MySql:Upsert:UpdateColumns", columnNames);
        
        return builder;
    }

    /// <summary>
    /// Optimizes entity for bulk INSERT operations using extended INSERT syntax.
    /// Configures multi-row INSERT statements for improved throughput and reduced network overhead.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="batchSize">Number of rows to include in each INSERT statement (default: 1000).</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>Multi-row INSERT benefits:</para>
    /// <list type="bullet">
    /// <item><description>Reduced network round-trips</description></item>
    /// <item><description>Lower SQL parsing overhead</description></item>
    /// <item><description>Improved transaction efficiency</description></item>
    /// <item><description>Better MySQL optimizer utilization</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// INSERT INTO users (name, email, created_at) VALUES
    /// ('User 1', 'user1@example.com', NOW()),
    /// ('User 2', 'user2@example.com', NOW()),
    /// ('User 3', 'user3@example.com', NOW());
    /// </code>
    /// <para>Optimal batch sizes:</para>
    /// <list type="bullet">
    /// <item><description>Small records (&lt;1KB): 1000-5000 rows</description></item>
    /// <item><description>Medium records (1-10KB): 100-1000 rows</description></item>
    /// <item><description>Large records (&gt;10KB): 10-100 rows</description></item>
    /// </list>
    /// </remarks>
    public static EntityTypeBuilder<T> OptimizeMySqlBulkInsert<T>(
        this EntityTypeBuilder<T> builder,
        int batchSize = 1000) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(batchSize, 65535); // MySQL limit
        
        builder.HasAnnotation("MySql:BulkInsert:Enabled", true);
        builder.HasAnnotation("MySql:BulkInsert:BatchSize", batchSize);
        
        // Configure additional optimizations for bulk operations
        builder.HasAnnotation("MySql:BulkInsert:DelayedInserts", true);
        builder.HasAnnotation("MySql:BulkInsert:ExtendedSyntax", true);
        
        return builder;
    }

    /// <summary>
    /// Configures REPLACE INTO operations for delete-then-insert semantics.
    /// REPLACE provides atomic delete-and-insert behavior, useful for complete record replacement.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>REPLACE INTO characteristics:</para>
    /// <list type="bullet">
    /// <item><description>Atomic delete-then-insert operation</description></item>
    /// <item><description>Handles unique constraint violations by replacement</description></item>
    /// <item><description>Triggers DELETE and INSERT triggers (not UPDATE)</description></item>
    /// <item><description>Auto-increment values may have gaps</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// REPLACE INTO cache (key, value, expires_at)
    /// VALUES ('user:123', '{"name":"John"}', '2025-01-01 00:00:00');
    /// </code>
    /// <para>Usage considerations:</para>
    /// <list type="bullet">
    /// <item><description>Use when complete record replacement is desired</description></item>
    /// <item><description>Avoid if foreign key references exist to this record</description></item>
    /// <item><description>Consider performance impact on auto-increment sequences</description></item>
    /// </list>
    /// </remarks>
    public static EntityTypeBuilder<T> EnableMySqlReplace<T>(
        this EntityTypeBuilder<T> builder) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.HasAnnotation("MySql:Replace:Enabled", true);
        
        return builder;
    }

    /// <summary>
    /// Configures index management strategy during bulk operations for optimal performance.
    /// Provides control over index behavior to maximize bulk operation throughput.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="strategy">The index management strategy to use during bulk operations.</param>
    /// <returns>The same entity type builder for method chaining.</returns>
    /// <remarks>
    /// <para>Index strategy performance comparison:</para>
    /// <list type="bullet">
    /// <item><description><strong>KeepEnabled:</strong> Safest but slowest (good for small batches)</description></item>
    /// <item><description><strong>DisableEnable:</strong> 50-80% faster loading (recommended for most cases)</description></item>
    /// <item><description><strong>DropRecreate:</strong> Fastest for massive loads but highest risk</description></item>
    /// </list>
    /// <para>MySQL equivalent operations:</para>
    /// <code>
    /// -- DisableEnable strategy
    /// ALTER TABLE users DISABLE KEYS;
    /// -- Perform bulk operations
    /// ALTER TABLE users ENABLE KEYS;
    /// 
    /// -- DropRecreate strategy  
    /// DROP INDEX idx_email ON users;
    /// -- Perform bulk operations
    /// CREATE INDEX idx_email ON users (email);
    /// </code>
    /// <para>Strategy selection guidelines:</para>
    /// <list type="bullet">
    /// <item><description>&lt;10K records: KeepEnabled for safety</description></item>
    /// <item><description>10K-1M records: DisableEnable for balance</description></item>
    /// <item><description>&gt;1M records: DropRecreate for maximum speed</description></item>
    /// </list>
    /// </remarks>
    public static EntityTypeBuilder<T> WithMySqlBulkIndexStrategy<T>(
        this EntityTypeBuilder<T> builder,
        BulkIndexStrategy strategy = BulkIndexStrategy.DisableEnable) where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.HasAnnotation("MySql:BulkIndex:Strategy", strategy.ToString());
        
        // Configure additional settings based on strategy
        switch (strategy)
        {
            case BulkIndexStrategy.DisableEnable:
                builder.HasAnnotation("MySql:BulkIndex:PreOperation", "ALTER TABLE {table} DISABLE KEYS");
                builder.HasAnnotation("MySql:BulkIndex:PostOperation", "ALTER TABLE {table} ENABLE KEYS");
                break;
                
            case BulkIndexStrategy.DropRecreate:
                builder.HasAnnotation("MySql:BulkIndex:PreOperation", "DROP INDEX {index} ON {table}");
                builder.HasAnnotation("MySql:BulkIndex:PostOperation", "CREATE INDEX {index} ON {table} ({columns})");
                builder.HasAnnotation("MySql:BulkIndex:BackupIndexes", true);
                break;
                
            case BulkIndexStrategy.KeepEnabled:
            default:
                // No special operations needed
                break;
        }
        
        return builder;
    }

    /// <summary>
    /// Extracts the property name from a lambda expression for use in MySQL operations.
    /// </summary>
    private static string GetPropertyName<T>(Expression<Func<T, object>> expression)
    {
        return expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member.Name,
            _ => throw new ArgumentException("Invalid property expression", nameof(expression))
        };
    }
}

/// <summary>
/// Configuration options for MySQL LOAD DATA INFILE operations.
/// These options control the format and behavior of bulk data loading.
/// </summary>
public class LoadDataOptions
{
    /// <summary>
    /// Character sequence that separates fields in the input data.
    /// Default: comma (,) for CSV format.
    /// </summary>
    public string FieldTerminator { get; set; } = ",";

    /// <summary>
    /// Character sequence that terminates each line in the input data.
    /// Default: newline (\n) for standard text files.
    /// </summary>
    public string LineTerminator { get; set; } = "\n";

    /// <summary>
    /// Character used to enclose field values, typically for escaping delimiters.
    /// Default: double quote (") for CSV format.
    /// </summary>
    public string FieldEnclosure { get; set; } = "\"";

    /// <summary>
    /// Character used for escaping special characters within field values.
    /// Default: backslash (\) following MySQL conventions.
    /// </summary>
    public string EscapeCharacter { get; set; } = "\\";

    /// <summary>
    /// Whether to ignore a specified number of lines at the beginning of the file.
    /// Useful for skipping header rows in CSV files.
    /// </summary>
    public bool IgnoreLines { get; set; } = false;

    /// <summary>
    /// Number of lines to ignore at the beginning of the file when IgnoreLines is true.
    /// Typically set to 1 to skip CSV headers.
    /// </summary>
    public int IgnoreLineCount { get; set; } = 0;

    /// <summary>
    /// Whether to use LOCAL keyword for loading data from client-side files.
    /// Default: true for loading files from the application server.
    /// Set to false when loading server-side files (requires FILE privilege).
    /// </summary>
    public bool Local { get; set; } = true;

    /// <summary>
    /// Character set used for interpreting the input data file.
    /// Default: utf8mb4 for full Unicode support including emojis.
    /// </summary>
    public string CharacterSet { get; set; } = "utf8mb4";
}

/// <summary>
/// Defines strategies for managing indexes during bulk operations.
/// Different strategies offer trade-offs between safety, performance, and complexity.
/// </summary>
public enum BulkIndexStrategy
{
    /// <summary>
    /// Keep all indexes enabled during bulk operations.
    /// Safest option but slowest performance for large bulk loads.
    /// Best for: Small batches (&lt;10K records), high-availability systems.
    /// </summary>
    KeepEnabled,

    /// <summary>
    /// Disable indexes before bulk operations, re-enable after completion.
    /// Good balance of performance and safety for most bulk loading scenarios.
    /// Best for: Medium batches (10K-1M records), most production systems.
    /// Performance gain: 50-80% faster than KeepEnabled.
    /// </summary>
    DisableEnable,

    /// <summary>
    /// Drop indexes before bulk operations, recreate after completion.
    /// Maximum performance but higher risk and longer recovery time if interrupted.
    /// Best for: Large batches (&gt;1M records), data warehouse loads, maintenance windows.
    /// Performance gain: Maximum speed but requires careful error handling.
    /// </summary>
    DropRecreate
}