// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.MySql;

/// <summary>
/// Provides extension methods for configuring MySQL-specific behaviors on Entity Framework Core properties.
/// </summary>
public static class ConfigurationExtensions
{
   private const string Now = "NOW()";

   /// <summary>
   /// Configures a property to use the MySQL "NOW()" function as the default value and
   /// to generate its value only when a new entity is added.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured.</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   public static void MySqlValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder)
   {
      builder.HasDefaultValueSql(Now);
      builder.ValueGeneratedOnAdd();
   }

   /// <summary>
   /// Configures the entity to use the InnoDB storage engine with optimized settings for transactional workloads.
   /// InnoDB provides ACID compliance, row-level locking, and foreign key constraints support.
   /// </summary>
   /// <typeparam name="T">The entity type being configured.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   /// <param name="rowFormat">The row format to use (default: Dynamic for optimal performance).</param>
   /// <returns>The same entity type builder for method chaining.</returns>
   /// <remarks>
   /// <para>InnoDB is the recommended storage engine for most applications because it provides:</para>
   /// <list type="bullet">
   /// <item><description>ACID (Atomicity, Consistency, Isolation, Durability) compliance</description></item>
   /// <item><description>Row-level locking for better concurrency</description></item>
   /// <item><description>Foreign key constraint support</description></item>
   /// <item><description>Crash recovery capabilities</description></item>
   /// <item><description>Multi-version concurrency control (MVCC)</description></item>
   /// </list>
   /// <para>MySQL equivalent SQL:</para>
   /// <code>
   /// CREATE TABLE users (
   ///     id INT PRIMARY KEY,
   ///     name VARCHAR(255)
   /// ) ENGINE=InnoDB ROW_FORMAT=DYNAMIC;
   /// </code>
   /// <para>Row format options:</para>
   /// <list type="bullet">
   /// <item><description><strong>Dynamic:</strong> Variable-length rows, optimal for most use cases</description></item>
   /// <item><description><strong>Compressed:</strong> Table-level compression, slower but space-efficient</description></item>
   /// <item><description><strong>Compact:</strong> More compact than Redundant, good for older compatibility</description></item>
   /// <item><description><strong>Redundant:</strong> Legacy format, not recommended for new applications</description></item>
   /// </list>
   /// </remarks>
   public static EntityTypeBuilder<T> UseMySqlInnoDB<T>(
       this EntityTypeBuilder<T> builder,
       InnoDBRowFormat rowFormat = InnoDBRowFormat.Dynamic) where T : class
   {
       ArgumentNullException.ThrowIfNull(builder);

       builder.HasAnnotation("MySql:StorageEngine", "InnoDB");
       
       var rowFormatString = rowFormat switch
       {
           InnoDBRowFormat.Dynamic => "DYNAMIC",
           InnoDBRowFormat.Compressed => "COMPRESSED",
           InnoDBRowFormat.Compact => "COMPACT",
           InnoDBRowFormat.Redundant => "REDUNDANT",
           _ => "DYNAMIC"
       };
       
       builder.HasAnnotation("MySql:RowFormat", rowFormatString);

       return builder;
   }

   /// <summary>
   /// Configures the entity to use the MyISAM storage engine optimized for read-heavy workloads.
   /// MyISAM provides table-level locking and is best suited for read-only or read-mostly applications.
   /// </summary>
   /// <typeparam name="T">The entity type being configured.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   /// <returns>The same entity type builder for method chaining.</returns>
   /// <remarks>
   /// <para>MyISAM characteristics:</para>
   /// <list type="bullet">
   /// <item><description>Table-level locking (not suitable for high-concurrency writes)</description></item>
   /// <item><description>No foreign key constraints support</description></item>
   /// <item><description>Fast for read operations and bulk inserts</description></item>
   /// <item><description>Smaller disk footprint than InnoDB</description></item>
   /// <item><description>No crash recovery (may require repair after crashes)</description></item>
   /// </list>
   /// <para>MySQL equivalent SQL:</para>
   /// <code>
   /// CREATE TABLE logs (
   ///     id INT PRIMARY KEY,
   ///     message TEXT
   /// ) ENGINE=MyISAM;
   /// </code>
   /// <para>
   /// <strong>Best for:</strong> Read-only tables, logging tables, data warehousing, bulk data loads.
   /// </para>
   /// <para>
   /// <strong>Avoid for:</strong> Applications with frequent writes, transactional requirements, or foreign key needs.
   /// </para>
   /// </remarks>
   public static EntityTypeBuilder<T> UseMySqlMyISAM<T>(
       this EntityTypeBuilder<T> builder) where T : class
   {
       ArgumentNullException.ThrowIfNull(builder);

       builder.HasAnnotation("MySql:StorageEngine", "MyISAM");

       return builder;
   }

   /// <summary>
   /// Configures the entity to use the Memory (HEAP) storage engine for ultra-fast access.
   /// Memory storage engine stores all data in RAM, providing extremely fast access but with no persistence.
   /// </summary>
   /// <typeparam name="T">The entity type being configured.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   /// <returns>The same entity type builder for method chaining.</returns>
   /// <remarks>
   /// <para>Memory (HEAP) storage engine characteristics:</para>
   /// <list type="bullet">
   /// <item><description>All data stored in RAM for maximum speed</description></item>
   /// <item><description>Data is lost when MySQL server restarts</description></item>
   /// <item><description>Fixed-length rows only (no VARCHAR, TEXT, BLOB)</description></item>
   /// <item><description>Table-level locking</description></item>
   /// <item><description>No foreign key support</description></item>
   /// </list>
   /// <para>MySQL equivalent SQL:</para>
   /// <code>
   /// CREATE TABLE session_cache (
   ///     session_id CHAR(32) PRIMARY KEY,
   ///     user_id INT,
   ///     created_at TIMESTAMP
   /// ) ENGINE=MEMORY;
   /// </code>
   /// <para>
   /// <strong>Best for:</strong> Temporary data, caches, session storage, lookup tables.
   /// </para>
   /// <para>
   /// <strong>Limitations:</strong> Data size limited by available RAM, no persistence across restarts.
   /// </para>
   /// </remarks>
   public static EntityTypeBuilder<T> UseMySqlMemory<T>(
       this EntityTypeBuilder<T> builder) where T : class
   {
       ArgumentNullException.ThrowIfNull(builder);

       builder.HasAnnotation("MySql:StorageEngine", "MEMORY");

       return builder;
   }

   /// <summary>
   /// Configures the entity to use the Archive storage engine for compressed, long-term storage.
   /// Archive storage engine provides high compression ratios and is optimized for storing large amounts of data
   /// that is rarely accessed.
   /// </summary>
   /// <typeparam name="T">The entity type being configured.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   /// <returns>The same entity type builder for method chaining.</returns>
   /// <remarks>
   /// <para>Archive storage engine characteristics:</para>
   /// <list type="bullet">
   /// <item><description>High compression ratios (up to 75% space savings)</description></item>
   /// <item><description>INSERT and SELECT operations only (no UPDATE or DELETE)</description></item>
   /// <item><description>No indexes except on AUTO_INCREMENT columns</description></item>
   /// <item><description>Optimized for long-term data retention</description></item>
   /// <item><description>Slower access compared to other engines</description></item>
   /// </list>
   /// <para>MySQL equivalent SQL:</para>
   /// <code>
   /// CREATE TABLE historical_logs (
   ///     id BIGINT AUTO_INCREMENT PRIMARY KEY,
   ///     event_date DATE,
   ///     log_data TEXT,
   ///     user_id INT
   /// ) ENGINE=ARCHIVE;
   /// </code>
   /// <para>
   /// <strong>Best for:</strong> Historical data, audit trails, data archiving, compliance data retention.
   /// </para>
   /// <para>
   /// <strong>Limitations:</strong> No updates or deletes, limited indexing, slower query performance.
   /// </para>
   /// </remarks>
   public static EntityTypeBuilder<T> UseMySqlArchive<T>(
       this EntityTypeBuilder<T> builder) where T : class
   {
       ArgumentNullException.ThrowIfNull(builder);

       builder.HasAnnotation("MySql:StorageEngine", "ARCHIVE");

       return builder;
   }

   /// <summary>
   /// Configures MySQL-specific table options including character set and collation for proper text handling.
   /// These settings ensure correct sorting, comparison, and storage of international text data.
   /// </summary>
   /// <typeparam name="T">The entity type being configured.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   /// <param name="charset">The character set to use (default: utf8mb4 for full Unicode support).</param>
   /// <param name="collation">The collation to use (default: utf8mb4_unicode_ci for proper Unicode sorting).</param>
   /// <returns>The same entity type builder for method chaining.</returns>
   /// <remarks>
   /// <para>Character set and collation considerations:</para>
   /// <list type="bullet">
   /// <item><description><strong>utf8mb4:</strong> Full 4-byte UTF-8 support, including emojis and special characters</description></item>
   /// <item><description><strong>utf8:</strong> 3-byte UTF-8 (legacy, does not support all Unicode characters)</description></item>
   /// <item><description><strong>latin1:</strong> Single-byte character set for Western European languages</description></item>
   /// </list>
   /// <para>Common collations:</para>
   /// <list type="bullet">
   /// <item><description><strong>utf8mb4_unicode_ci:</strong> Unicode rules, case-insensitive, accent-insensitive</description></item>
   /// <item><description><strong>utf8mb4_bin:</strong> Binary comparison, case-sensitive, accent-sensitive</description></item>
   /// <item><description><strong>utf8mb4_general_ci:</strong> Faster but less accurate than unicode_ci</description></item>
   /// </list>
   /// <para>MySQL equivalent SQL:</para>
   /// <code>
   /// CREATE TABLE products (
   ///     id INT PRIMARY KEY,
   ///     name VARCHAR(255),
   ///     description TEXT
   /// ) ENGINE=InnoDB 
   ///   DEFAULT CHARSET=utf8mb4 
   ///   COLLATE=utf8mb4_unicode_ci;
   /// </code>
   /// <para>
   /// <strong>Recommendation:</strong> Always use utf8mb4 with utf8mb4_unicode_ci for new applications
   /// to ensure proper international text support and emoji handling.
   /// </para>
   /// </remarks>
   public static EntityTypeBuilder<T> WithMySqlTableOptions<T>(
       this EntityTypeBuilder<T> builder,
       string charset = "utf8mb4",
       string collation = "utf8mb4_unicode_ci") where T : class
   {
       ArgumentNullException.ThrowIfNull(builder);
       ArgumentException.ThrowIfNullOrWhiteSpace(charset);
       ArgumentException.ThrowIfNullOrWhiteSpace(collation);

       builder.HasAnnotation("MySql:Charset", charset);
       builder.HasAnnotation("MySql:Collation", collation);

       return builder;
   }
}

/// <summary>
/// Specifies the row format options available for InnoDB storage engine.
/// Row format affects how data is stored and compressed within InnoDB tables.
/// </summary>
public enum InnoDBRowFormat
{
   /// <summary>
   /// Dynamic row format - variable-length rows, optimal for most use cases.
   /// Supports efficient storage of large columns and is the default for new tables.
   /// </summary>
   Dynamic,

   /// <summary>
   /// Compressed row format - provides table-level compression to save disk space.
   /// Slower performance but significant space savings, good for archival data.
   /// </summary>
   Compressed,

   /// <summary>
   /// Compact row format - more space-efficient than Redundant format.
   /// Good for applications that need backward compatibility with older MySQL versions.
   /// </summary>
   Compact,

   /// <summary>
   /// Redundant row format - legacy format for backward compatibility.
   /// Not recommended for new applications due to less efficient space usage.
   /// </summary>
   Redundant
}