// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Wangkanai.EntityFramework.MySql;

/// <summary>
/// Provides extension methods for configuring MySQL connection optimization and performance settings
/// for Entity Framework Core applications using the Pomelo MySQL provider.
/// </summary>
public static class ConnectionConfigurationExtensions
{
    /// <summary>
    /// Configures MySQL connection pooling for optimal performance in web applications.
    /// Connection pooling reduces connection overhead and improves scalability by reusing connections.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="connectionString">The MySQL connection string to configure.</param>
    /// <param name="minPoolSize">Minimum number of connections to maintain in the pool (default: 10).</param>
    /// <param name="maxPoolSize">Maximum number of connections allowed in the pool (default: 100).</param>
    /// <param name="connectionLifeTime">Connection lifetime in seconds before being closed (default: 3600).</param>
    /// <returns>The same DbContext options builder for method chaining.</returns>
    /// <remarks>
    /// <para>MySQL equivalent SQL configuration:</para>
    /// <code>
    /// -- Set connection pool limits
    /// SET GLOBAL max_connections = 100;
    /// SET GLOBAL thread_cache_size = 50;
    /// SET GLOBAL interactive_timeout = 3600;
    /// SET GLOBAL wait_timeout = 3600;
    /// </code>
    /// <para>
    /// Recommended settings:
    /// - For high-traffic web apps: minPoolSize=20, maxPoolSize=200
    /// - For moderate traffic: minPoolSize=10, maxPoolSize=100
    /// - For development: minPoolSize=5, maxPoolSize=50
    /// </para>
    /// </remarks>
    public static DbContextOptionsBuilder<T> ConfigureMySqlConnectionPool<T>(
        this DbContextOptionsBuilder<T> optionsBuilder,
        string connectionString,
        uint minPoolSize = 10,
        uint maxPoolSize = 100,
        uint connectionLifeTime = 3600) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        
        if (minPoolSize > maxPoolSize)
            throw new ArgumentException("Minimum pool size cannot be greater than maximum pool size.");
        
        if (connectionLifeTime == 0)
            throw new ArgumentException("Connection lifetime must be greater than zero.");

        var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MinimumPoolSize = minPoolSize,
            MaximumPoolSize = maxPoolSize,
            ConnectionLifeTime = connectionLifeTime
        };

        return optionsBuilder.UseMySql(connectionStringBuilder.ConnectionString, ServerVersion.AutoDetect(connectionStringBuilder.ConnectionString));
    }

    /// <summary>
    /// Configures SSL/TLS encryption for secure MySQL connections to protect data in transit.
    /// This is essential for production environments and compliance requirements.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="connectionString">The MySQL connection string to configure.</param>
    /// <param name="sslMode">The SSL mode to use for connections (default: Required).</param>
    /// <returns>The same DbContext options builder for method chaining.</returns>
    /// <remarks>
    /// <para>MySQL SSL modes:</para>
    /// <list type="bullet">
    /// <item><description><strong>None:</strong> No SSL encryption (not recommended for production)</description></item>
    /// <item><description><strong>Preferred:</strong> Use SSL if available, fallback to unencrypted</description></item>
    /// <item><description><strong>Required:</strong> Always use SSL, fail if not available</description></item>
    /// <item><description><strong>VerifyCA:</strong> Use SSL and verify certificate authority</description></item>
    /// <item><description><strong>VerifyFull:</strong> Use SSL and verify certificate and hostname</description></item>
    /// </list>
    /// <para>Production recommendation: Use Required, VerifyCA, or VerifyFull for maximum security.</para>
    /// </remarks>
    public static DbContextOptionsBuilder<T> RequireMySqlSSL<T>(
        this DbContextOptionsBuilder<T> optionsBuilder,
        string connectionString,
        MySqlSslMode sslMode = MySqlSslMode.Required) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            SslMode = sslMode
        };

        return optionsBuilder.UseMySql(connectionStringBuilder.ConnectionString, ServerVersion.AutoDetect(connectionStringBuilder.ConnectionString));
    }

    /// <summary>
    /// Configures MySQL-specific timeout values for connection and command execution.
    /// Proper timeout configuration prevents hanging connections and improves application resilience.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="connectionString">The MySQL connection string to configure.</param>
    /// <param name="connectionTimeout">Time to wait while attempting to establish a connection in seconds (default: 15).</param>
    /// <param name="defaultCommandTimeout">Default time to wait for a command to execute in seconds (default: 30).</param>
    /// <returns>The same DbContext options builder for method chaining.</returns>
    /// <remarks>
    /// <para>Timeout considerations:</para>
    /// <list type="bullet">
    /// <item><description><strong>Connection Timeout:</strong> Should be long enough for network latency but short enough to fail fast</description></item>
    /// <item><description><strong>Command Timeout:</strong> Should account for longest expected query execution time</description></item>
    /// </list>
    /// <para>Recommended settings:</para>
    /// <list type="bullet">
    /// <item><description><strong>Development:</strong> connectionTimeout=30, commandTimeout=60</description></item>
    /// <item><description><strong>Production:</strong> connectionTimeout=15, commandTimeout=30</description></item>
    /// <item><description><strong>Batch operations:</strong> commandTimeout=300 or higher</description></item>
    /// </list>
    /// </remarks>
    public static DbContextOptionsBuilder<T> SetMySqlTimeouts<T>(
        this DbContextOptionsBuilder<T> optionsBuilder,
        string connectionString,
        uint connectionTimeout = 15,
        uint defaultCommandTimeout = 30) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        
        if (connectionTimeout == 0)
            throw new ArgumentException("Connection timeout must be greater than zero.");
        
        if (defaultCommandTimeout == 0)
            throw new ArgumentException("Command timeout must be greater than zero.");

        var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = connectionTimeout,
            DefaultCommandTimeout = defaultCommandTimeout
        };

        return optionsBuilder.UseMySql(connectionStringBuilder.ConnectionString, ServerVersion.AutoDetect(connectionStringBuilder.ConnectionString));
    }

    /// <summary>
    /// Enables server-side prepared statements for improved performance of repeated queries.
    /// Prepared statements reduce parsing overhead and can improve query execution performance,
    /// especially for parameterized queries executed multiple times.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="connectionString">The MySQL connection string to configure.</param>
    /// <returns>The same DbContext options builder for method chaining.</returns>
    /// <remarks>
    /// <para>Benefits of prepared statements:</para>
    /// <list type="bullet">
    /// <item><description>Reduced SQL parsing overhead for repeated queries</description></item>
    /// <item><description>Better protection against SQL injection attacks</description></item>
    /// <item><description>Optimized query execution plans</description></item>
    /// <item><description>Reduced network traffic for complex queries</description></item>
    /// </list>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// -- Prepare statement
    /// PREPARE stmt1 FROM 'SELECT * FROM users WHERE id = ?';
    /// -- Execute with parameter
    /// SET @user_id = 123;
    /// EXECUTE stmt1 USING @user_id;
    /// -- Deallocate when done
    /// DEALLOCATE PREPARE stmt1;
    /// </code>
    /// <para>
    /// Best for: Applications with many repeated parameterized queries.
    /// Consider disabling for: Applications with mostly ad-hoc queries that don't repeat.
    /// </para>
    /// </remarks>
    public static DbContextOptionsBuilder<T> EnableMySqlPreparedStatements<T>(
        this DbContextOptionsBuilder<T> optionsBuilder,
        string connectionString) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            UseAffectedRows = false,
            UseCompression = true
        };

        return optionsBuilder.UseMySql(connectionStringBuilder.ConnectionString, ServerVersion.AutoDetect(connectionStringBuilder.ConnectionString), 
            mySqlOptions => mySqlOptions.EnableRetryOnFailure());
    }

    /// <summary>
    /// Configures MySQL connection for read-heavy workloads with optimized settings.
    /// This method combines multiple optimizations suitable for applications with high read volumes.
    /// </summary>
    /// <typeparam name="T">The type of DbContext being configured.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="connectionString">The MySQL connection string to configure.</param>
    /// <param name="maxPoolSize">Maximum number of connections in the pool (default: 200).</param>
    /// <returns>The same DbContext options builder for method chaining.</returns>
    /// <remarks>
    /// <para>This method configures the following optimizations:</para>
    /// <list type="bullet">
    /// <item><description>Large connection pool for high concurrency</description></item>
    /// <item><description>Prepared statement caching</description></item>
    /// <item><description>Batch query support</description></item>
    /// <item><description>Connection compression</description></item>
    /// <item><description>Retry on failure for resilience</description></item>
    /// </list>
    /// <para>Best for: Read-heavy web applications, reporting systems, data analytics workloads.</para>
    /// </remarks>
    public static DbContextOptionsBuilder<T> OptimizeForReadHeavyWorkloads<T>(
        this DbContextOptionsBuilder<T> optionsBuilder,
        string connectionString,
        uint maxPoolSize = 200) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        
        if (maxPoolSize == 0)
            throw new ArgumentException("Maximum pool size must be greater than zero.");

        var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MinimumPoolSize = Math.Min(20, maxPoolSize / 4), // 25% of max or 20, whichever is smaller
            MaximumPoolSize = maxPoolSize,
            ConnectionLifeTime = 7200, // 2 hours
            ConnectionTimeout = 30,
            DefaultCommandTimeout = 60,
            UseCompression = true,
            UseAffectedRows = false
        };

        return optionsBuilder.UseMySql(connectionStringBuilder.ConnectionString, ServerVersion.AutoDetect(connectionStringBuilder.ConnectionString),
            mySqlOptions => 
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3, 
                    maxRetryDelay: TimeSpan.FromSeconds(30), 
                    errorNumbersToAdd: null);
                mySqlOptions.CommandTimeout(60);
            });
    }
}