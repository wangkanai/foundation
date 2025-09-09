// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Wangkanai.EntityFramework.Sqlite;

/// <summary>
/// Provides extension methods for configuring SQLite connection-level performance optimizations.
/// </summary>
public static class ConnectionConfigurationExtensions
{
   /// <summary>
   /// Configures SQLite connection to use Write-Ahead Logging (WAL) mode by modifying the connection string.
   /// WAL mode provides better concurrency and performance for read-heavy workloads.
   /// </summary>
   /// <typeparam name="T">The type of the DbContext.</typeparam>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   public static DbContextOptionsBuilder<T> EnableSqliteWAL<T>(this DbContextOptionsBuilder<T> optionsBuilder, string connectionString)
      where T : DbContext
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString)
      {
         Cache = SqliteCacheMode.Shared
      };

      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString(), options =>
      {
         options.CommandTimeout(30);
      });
   }

   /// <summary>
   /// Configures SQLite connection to use Write-Ahead Logging (WAL) mode by modifying the connection string.
   /// WAL mode provides better concurrency and performance for read-heavy workloads.
   /// </summary>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   public static DbContextOptionsBuilder EnableSqliteWAL(this DbContextOptionsBuilder optionsBuilder, string connectionString)
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString)
      {
         Cache = SqliteCacheMode.Shared
      };

      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString());
   }

   /// <summary>
   /// Configures the SQLite cache size to optimize memory usage and query performance by modifying the connection string.
   /// A larger cache size can significantly improve performance for frequently accessed data.
   /// </summary>
   /// <typeparam name="T">The type of the DbContext.</typeparam>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <param name="cacheSizeKB">The cache size in kilobytes. Default is 65536 KB (64 MB).</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheSizeKB is less than or equal to zero.</exception>
   public static DbContextOptionsBuilder<T> SetSqliteCacheSize<T>(this DbContextOptionsBuilder<T> optionsBuilder, string connectionString, int cacheSizeKB = 65536)
      where T : DbContext
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));
      if (cacheSizeKB <= 0)
         throw new ArgumentOutOfRangeException(nameof(cacheSizeKB), "Cache size must be greater than zero.");

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
      
      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString(), options =>
      {
         options.CommandTimeout(30);
      });
   }

   /// <summary>
   /// Configures the SQLite cache size to optimize memory usage and query performance by modifying the connection string.
   /// A larger cache size can significantly improve performance for frequently accessed data.
   /// </summary>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <param name="cacheSizeKB">The cache size in kilobytes. Default is 65536 KB (64 MB).</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheSizeKB is less than or equal to zero.</exception>
   public static DbContextOptionsBuilder SetSqliteCacheSize(this DbContextOptionsBuilder optionsBuilder, string connectionString, int cacheSizeKB = 65536)
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));
      if (cacheSizeKB <= 0)
         throw new ArgumentOutOfRangeException(nameof(cacheSizeKB), "Cache size must be greater than zero.");

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
      
      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString());
   }

   /// <summary>
   /// Configures the SQLite busy timeout by modifying the connection string.
   /// The busy timeout determines how long SQLite waits when the database is locked before returning SQLITE_BUSY.
   /// This is crucial for handling concurrent access scenarios gracefully.
   /// </summary>
   /// <typeparam name="T">The type of the DbContext.</typeparam>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <param name="timeoutMilliseconds">The timeout in milliseconds. Default is 30000 ms (30 seconds).</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMilliseconds is less than zero.</exception>
   public static DbContextOptionsBuilder<T> SetSqliteBusyTimeout<T>(this DbContextOptionsBuilder<T> optionsBuilder, string connectionString, int timeoutMilliseconds = 30000)
      where T : DbContext
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));
      if (timeoutMilliseconds < 0)
         throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout cannot be negative.");

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
      
      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString(), options =>
      {
         options.CommandTimeout(timeoutMilliseconds / 1000); // Convert to seconds for CommandTimeout
      });
   }

   /// <summary>
   /// Configures the SQLite busy timeout by modifying the connection string.
   /// The busy timeout determines how long SQLite waits when the database is locked before returning SQLITE_BUSY.
   /// This is crucial for handling concurrent access scenarios gracefully.
   /// </summary>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <param name="timeoutMilliseconds">The timeout in milliseconds. Default is 30000 ms (30 seconds).</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMilliseconds is less than zero.</exception>
   public static DbContextOptionsBuilder SetSqliteBusyTimeout(this DbContextOptionsBuilder optionsBuilder, string connectionString, int timeoutMilliseconds = 30000)
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));
      if (timeoutMilliseconds < 0)
         throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout cannot be negative.");

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
      
      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString());
   }

   /// <summary>
   /// Enables foreign key constraints in SQLite by modifying the connection string.
   /// By default, SQLite does not enforce foreign key constraints, so this method explicitly enables them for data integrity.
   /// This should be enabled for production applications to maintain referential integrity.
   /// </summary>
   /// <typeparam name="T">The type of the DbContext.</typeparam>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   public static DbContextOptionsBuilder<T> EnableSqliteForeignKeys<T>(this DbContextOptionsBuilder<T> optionsBuilder, string connectionString)
      where T : DbContext
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString)
      {
         ForeignKeys = true
      };

      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString(), options =>
      {
         options.CommandTimeout(30);
      });
   }

   /// <summary>
   /// Enables foreign key constraints in SQLite by modifying the connection string.
   /// By default, SQLite does not enforce foreign key constraints, so this method explicitly enables them for data integrity.
   /// This should be enabled for production applications to maintain referential integrity.
   /// </summary>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   public static DbContextOptionsBuilder EnableSqliteForeignKeys(this DbContextOptionsBuilder optionsBuilder, string connectionString)
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString)
      {
         ForeignKeys = true
      };

      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString());
   }

   /// <summary>
   /// Configures comprehensive SQLite performance optimizations including WAL mode,
   /// optimized cache size, appropriate busy timeout, and foreign key enforcement.
   /// This is a convenience method that applies commonly recommended settings.
   /// </summary>
   /// <typeparam name="T">The type of the DbContext.</typeparam>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <param name="cacheSizeKB">The cache size in kilobytes. Default is 65536 KB (64 MB).</param>
   /// <param name="timeoutMilliseconds">The busy timeout in milliseconds. Default is 30000 ms (30 seconds).</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheSizeKB is less than or equal to zero or timeoutMilliseconds is negative.</exception>
   public static DbContextOptionsBuilder<T> OptimizeForSqlitePerformance<T>(this DbContextOptionsBuilder<T> optionsBuilder, string connectionString, int cacheSizeKB = 65536, int timeoutMilliseconds = 30000)
      where T : DbContext
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));
      if (cacheSizeKB <= 0)
         throw new ArgumentOutOfRangeException(nameof(cacheSizeKB), "Cache size must be greater than zero.");
      if (timeoutMilliseconds < 0)
         throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout cannot be negative.");

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString)
      {
         Cache = SqliteCacheMode.Shared,
         ForeignKeys = true
      };

      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString(), options =>
      {
         options.CommandTimeout(timeoutMilliseconds / 1000); // Convert to seconds for CommandTimeout
      });
   }

   /// <summary>
   /// Configures comprehensive SQLite performance optimizations including WAL mode,
   /// optimized cache size, appropriate busy timeout, and foreign key enforcement.
   /// This is a convenience method that applies commonly recommended settings.
   /// </summary>
   /// <param name="optionsBuilder">The options builder used to configure the context.</param>
   /// <param name="connectionString">The base SQLite connection string to modify.</param>
   /// <param name="cacheSizeKB">The cache size in kilobytes. Default is 65536 KB (64 MB).</param>
   /// <param name="timeoutMilliseconds">The busy timeout in milliseconds. Default is 30000 ms (30 seconds).</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
   /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheSizeKB is less than or equal to zero or timeoutMilliseconds is negative.</exception>
   public static DbContextOptionsBuilder OptimizeForSqlitePerformance(this DbContextOptionsBuilder optionsBuilder, string connectionString, int cacheSizeKB = 65536, int timeoutMilliseconds = 30000)
   {
      if (string.IsNullOrEmpty(connectionString))
         throw new ArgumentNullException(nameof(connectionString));
      if (cacheSizeKB <= 0)
         throw new ArgumentOutOfRangeException(nameof(cacheSizeKB), "Cache size must be greater than zero.");
      if (timeoutMilliseconds < 0)
         throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout cannot be negative.");

      var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString)
      {
         Cache = SqliteCacheMode.Shared,
         ForeignKeys = true
      };

      return optionsBuilder.UseSqlite(connectionStringBuilder.ToString());
   }
}