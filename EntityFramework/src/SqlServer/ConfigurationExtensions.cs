// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Data;

namespace Wangkanai.EntityFramework.SqlServer;

/// <summary>
/// Provides extension methods for configuring SQL Server-specific behaviors on Entity Framework Core properties.
/// </summary>
public static class ConfigurationExtensions
{
   private const string GetDate = "GETDATE()";

   /// <summary>
   /// Configures a property to use the SQL Server "GETDATE()" function as the default value and
   /// to generate its value only when a new entity is added.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured.</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   public static void SqlValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder)
   {
      builder.HasDefaultValueSql(GetDate);
      builder.ValueGeneratedOnAdd();
   }

   /// <summary>
   /// Configures snapshot isolation for read-heavy workloads.
   /// Provides consistent point-in-time view without blocking writers.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <param name="builder">The entity type builder for the entity.</param>
   /// <returns>The same entity type builder instance for method chaining.</returns>
   /// <remarks>
   /// <para>
   /// Snapshot isolation uses row versioning to provide transaction-level read consistency.
   /// Readers do not block writers and writers do not block readers, reducing contention in high-concurrency scenarios.
   /// </para>
   /// <para>
   /// This configuration is most effective for:
   /// - Read-heavy workloads with occasional writes
   /// - Reporting queries that need consistent data views
   /// - Applications requiring high read concurrency
   /// </para>
   /// <para>
   /// SQL Server equivalent: SET TRANSACTION ISOLATION LEVEL SNAPSHOT
   /// </para>
   /// <example>
   /// Example configuration:
   /// <code>
   /// modelBuilder.Entity&lt;Product&gt;()
   ///     .UseSqlServerSnapshotIsolation();
   /// </code>
   /// </example>
   /// </remarks>
   public static EntityTypeBuilder<T> UseSqlServerSnapshotIsolation<T>(this EntityTypeBuilder<T> builder) where T : class
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      // Configure the entity to use snapshot isolation by setting the appropriate annotation
      // This will be translated to appropriate SQL Server isolation level settings during query execution
      builder.HasAnnotation("SqlServer:IsolationLevel", IsolationLevel.Snapshot);
      
      return builder;
   }

   /// <summary>
   /// Enables row versioning for optimistic concurrency control.
   /// Uses SQL Server's built-in rowversion/timestamp type for conflict detection.
   /// </summary>
   /// <param name="builder">The property builder for the byte array property.</param>
   /// <returns>The same property builder instance for method chaining.</returns>
   /// <remarks>
   /// <para>
   /// Row versioning provides automatic optimistic concurrency control using SQL Server's rowversion data type.
   /// The database automatically updates this value whenever any column in the row changes.
   /// </para>
   /// <para>
   /// Benefits:
   /// - Automatic concurrency conflict detection
   /// - No need to compare all columns for changes
   /// - Minimal storage overhead (8 bytes per row)
   /// - Database-generated values ensure uniqueness
   /// </para>
   /// <para>
   /// SQL Server equivalent: column_name rowversion NOT NULL
   /// </para>
   /// <example>
   /// Example configuration:
   /// <code>
   /// modelBuilder.Entity&lt;Product&gt;()
   ///     .Property(p => p.RowVersion)
   ///     .HasSqlServerRowVersion();
   /// </code>
   /// </example>
   /// </remarks>
   /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
   public static PropertyBuilder<byte[]> HasSqlServerRowVersion(this PropertyBuilder<byte[]> builder)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      // Configure as rowversion/timestamp column
      builder.HasColumnType("rowversion");
      builder.ValueGeneratedOnAddOrUpdate();
      builder.IsConcurrencyToken();
      
      return builder;
   }

   /// <summary>
   /// Configures read uncommitted isolation for reporting queries.
   /// Reduces lock contention for non-critical read operations by allowing dirty reads.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <param name="builder">The entity type builder for the entity.</param>
   /// <returns>The same entity type builder instance for method chaining.</returns>
   /// <remarks>
   /// <para>
   /// Read uncommitted isolation (NOLOCK) allows queries to read uncommitted data, 
   /// reducing blocking but potentially returning inconsistent or dirty data.
   /// </para>
   /// <para>
   /// Use this isolation level only when:
   /// - Data consistency is not critical (e.g., reporting, analytics)
   /// - Performance is more important than data accuracy
   /// - You can tolerate dirty reads, phantom reads, and non-repeatable reads
   /// </para>
   /// <para>
   /// WARNING: This isolation level can return:
   /// - Uncommitted data that may be rolled back
   /// - Duplicate or missing rows
   /// - Inconsistent data across related tables
   /// </para>
   /// <para>
   /// SQL Server equivalent: SELECT * FROM table WITH (NOLOCK) or SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
   /// </para>
   /// <example>
   /// Example configuration for reporting entities:
   /// <code>
   /// modelBuilder.Entity&lt;ReportData&gt;()
   ///     .UseSqlServerNoLock();
   /// </code>
   /// </example>
   /// </remarks>
   /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
   public static EntityTypeBuilder<T> UseSqlServerNoLock<T>(this EntityTypeBuilder<T> builder) where T : class
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      // Configure the entity to use read uncommitted isolation level
      // This will be translated to NOLOCK hints or READ UNCOMMITTED isolation during query execution
      builder.HasAnnotation("SqlServer:IsolationLevel", IsolationLevel.ReadUncommitted);
      builder.HasAnnotation("SqlServer:UseNoLock", true);
      
      return builder;
   }
}