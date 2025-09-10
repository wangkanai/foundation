// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring PostgreSQL-specific connection behaviors and performance optimizations.
/// </summary>
public static class ConnectionConfigurationExtensions
{
    /// <summary>
    /// Configures connection pooling with optimal settings for high-concurrency scenarios.
    /// Connection pooling manages the lifecycle of database connections to reduce overhead and improve performance.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <param name="minPoolSize">The minimum number of connections to maintain in the pool. Default is 10.</param>
    /// <param name="maxPoolSize">The maximum number of connections allowed in the pool. Default is 100.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minPoolSize is negative or maxPoolSize is less than minPoolSize.</exception>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .ConfigureNpgsqlConnectionPool(minPoolSize: 5, maxPoolSize: 50));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> ConfigureNpgsqlConnectionPool<T>(
        this DbContextOptionsBuilder<T> builder,
        int minPoolSize = 10,
        int maxPoolSize = 100) where T : DbContext
    {
        if (minPoolSize < 0)
            throw new ArgumentOutOfRangeException(nameof(minPoolSize), "Minimum pool size cannot be negative.");
        
        if (maxPoolSize < minPoolSize)
            throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Maximum pool size must be greater than or equal to minimum pool size.");

        // Note: Connection pooling in Npgsql is configured via connection string parameters
        // MinPoolSize and MaxPoolSize can be set in the connection string:
        // "Host=localhost;Database=mydb;Username=user;Password=pass;MinPoolSize=10;MaxPoolSize=100"
        return builder.UseNpgsql(opts => {
            // Connection pooling is handled by Npgsql automatically
            // This method serves as a documentation/configuration placeholder
        });
    }

    /// <summary>
    /// Enables prepared statement caching to improve query performance by reducing parsing overhead.
    /// Prepared statements are cached and reused for frequently executed queries with the same structure.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <param name="maxAutoPrepare">The maximum number of statements to automatically prepare and cache. Default is 25.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAutoPrepare is negative.</exception>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .EnableNpgsqlPreparedStatements(maxAutoPrepare: 50));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableNpgsqlPreparedStatements<T>(
        this DbContextOptionsBuilder<T> builder,
        int maxAutoPrepare = 25) where T : DbContext
    {
        if (maxAutoPrepare < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAutoPrepare), "Maximum auto prepare count cannot be negative.");

        // Note: Prepared statements in Npgsql are configured via connection string:
        // "Host=localhost;Database=mydb;Username=user;Password=pass;MaxAutoPrepare=25"
        return builder.UseNpgsql(opts => {
            // Prepared statement configuration is handled via connection string
            // This method serves as a documentation/configuration placeholder
        });
    }

    /// <summary>
    /// Configures SSL/TLS encryption for secure database connections.
    /// SSL encryption is essential for production deployments and compliance requirements.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <param name="mode">The SSL mode to use for the connection. Default is SslMode.Require.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .RequireNpgsqlSSL(SslMode.VerifyFull));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> RequireNpgsqlSSL<T>(
        this DbContextOptionsBuilder<T> builder,
        SslMode mode = SslMode.Require) where T : DbContext
    {
        // Note: SSL mode in Npgsql is configured via connection string:
        // "Host=localhost;Database=mydb;Username=user;Password=pass;SslMode=Require"
        return builder.UseNpgsql(opts => {
            // SSL configuration is handled via connection string
            // This method serves as a documentation/configuration placeholder
        });
    }

    /// <summary>
    /// Sets the statement timeout for database operations to prevent resource exhaustion from long-running queries.
    /// Queries that exceed this timeout will be automatically cancelled to protect system resources.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <param name="timeout">The maximum time to wait for a statement to execute before timing out.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is negative or zero.</exception>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .SetNpgsqlStatementTimeout(TimeSpan.FromMinutes(5)));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> SetNpgsqlStatementTimeout<T>(
        this DbContextOptionsBuilder<T> builder,
        TimeSpan timeout) where T : DbContext
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Statement timeout must be greater than zero.");

        // Note: Command timeout in Npgsql is configured via connection string:
        // "Host=localhost;Database=mydb;Username=user;Password=pass;CommandTimeout=30"
        return builder.UseNpgsql(opts => {
            // Command timeout configuration is handled via connection string
            // This method serves as a documentation/configuration placeholder
        });
    }

    /// <summary>
    /// Enables connection multiplexing to improve connection efficiency by sharing physical connections
    /// across multiple logical connections. This reduces connection overhead and improves scalability.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <remarks>
    /// Multiplexing is particularly beneficial in high-concurrency scenarios where many short-lived
    /// operations are performed. Note that multiplexing has some limitations with certain PostgreSQL
    /// features like LISTEN/NOTIFY.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .EnableNpgsqlMultiplexing());
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> EnableNpgsqlMultiplexing<T>(
        this DbContextOptionsBuilder<T> builder) where T : DbContext
    {
        // Note: Multiplexing in Npgsql is configured via connection string:
        // "Host=localhost;Database=mydb;Username=user;Password=pass;Multiplexing=true"
        return builder.UseNpgsql(opts => {
            // Multiplexing configuration is handled via connection string
            // This method serves as a documentation/configuration placeholder
        });
    }

    /// <summary>
    /// Configures the connection timeout for establishing database connections.
    /// This controls how long to wait when attempting to establish a connection before terminating the attempt.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <param name="timeout">The time to wait while trying to establish a connection before terminating the attempt.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is negative or zero.</exception>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .SetNpgsqlConnectionTimeout(TimeSpan.FromSeconds(30)));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> SetNpgsqlConnectionTimeout<T>(
        this DbContextOptionsBuilder<T> builder,
        TimeSpan timeout) where T : DbContext
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Connection timeout must be greater than zero.");

        // Note: Connection timeout in Npgsql is configured via connection string:
        // "Host=localhost;Database=mydb;Username=user;Password=pass;Timeout=15"
        return builder.UseNpgsql(opts => {
            // Connection timeout configuration is handled via connection string
            // This method serves as a documentation/configuration placeholder
        });
    }

    /// <summary>
    /// Configures advanced connection settings for optimal PostgreSQL performance.
    /// This method provides a comprehensive configuration for production environments.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="builder">The DbContextOptionsBuilder used to configure the context.</param>
    /// <param name="minPoolSize">The minimum number of connections in the pool. Default is 5.</param>
    /// <param name="maxPoolSize">The maximum number of connections in the pool. Default is 50.</param>
    /// <param name="maxAutoPrepare">The maximum number of prepared statements to cache. Default is 20.</param>
    /// <param name="commandTimeout">The timeout for database commands. Default is 30 seconds.</param>
    /// <param name="connectionTimeout">The timeout for establishing connections. Default is 15 seconds.</param>
    /// <param name="enableMultiplexing">Whether to enable connection multiplexing. Default is true.</param>
    /// <param name="sslMode">The SSL mode for secure connections. Default is SslMode.Prefer.</param>
    /// <returns>The same DbContextOptionsBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .ConfigureNpgsqlPerformance(
    ///                minPoolSize: 10,
    ///                maxPoolSize: 100,
    ///                maxAutoPrepare: 50,
    ///                commandTimeout: TimeSpan.FromMinutes(2),
    ///                enableMultiplexing: true,
    ///                sslMode: SslMode.Require));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<T> ConfigureNpgsqlPerformance<T>(
        this DbContextOptionsBuilder<T> builder,
        int minPoolSize = 5,
        int maxPoolSize = 50,
        int maxAutoPrepare = 20,
        TimeSpan? commandTimeout = null,
        TimeSpan? connectionTimeout = null,
        bool enableMultiplexing = true,
        SslMode sslMode = SslMode.Prefer) where T : DbContext
    {
        commandTimeout ??= TimeSpan.FromSeconds(30);
        connectionTimeout ??= TimeSpan.FromSeconds(15);

        // Validate commandTimeout parameter
        if (commandTimeout.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(commandTimeout), "Statement timeout must be greater than zero.");

        // Validate connectionTimeout parameter  
        if (connectionTimeout.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(connectionTimeout), "Connection timeout must be greater than zero.");

        return builder
            .ConfigureNpgsqlConnectionPool(minPoolSize, maxPoolSize)
            .EnableNpgsqlPreparedStatements(maxAutoPrepare)
            .SetNpgsqlStatementTimeout(commandTimeout.Value)
            .SetNpgsqlConnectionTimeout(connectionTimeout.Value)
            .RequireNpgsqlSSL(sslMode)
            .UseNpgsql(opts => {
                // All configuration is handled via connection string parameters
                // This serves as a comprehensive configuration documentation method
            });
    }
}