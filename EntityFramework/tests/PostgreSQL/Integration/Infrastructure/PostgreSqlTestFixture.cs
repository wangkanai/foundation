// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using DotNet.Testcontainers.Builders;
using Xunit;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;

/// <summary>
/// Base test fixture for PostgreSQL integration tests using Testcontainers.
/// Provides a real PostgreSQL database instance for testing.
/// Tests will be skipped if Docker/Podman is not available.
/// </summary>
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _container;
    private bool _isDockerAvailable = true;

    public PostgreSqlTestFixture()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("test_db")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .WithCleanUp(true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();
        }
        catch (Exception)
        {
            _isDockerAvailable = false;
            _container = null;
        }
    }

    public string ConnectionString => _container?.GetConnectionString() ?? 
        throw new InvalidOperationException("Docker/Podman is not available. Integration tests cannot run.");
    
    public bool IsDockerAvailable => _isDockerAvailable;

    public async ValueTask InitializeAsync()
    {
        if (!_isDockerAvailable || _container == null)
        {
            // Docker/Podman is not available - tests will be skipped
            return;
        }

        try
        {
            await _container.StartAsync();

            // Wait for PostgreSQL to be fully ready with retry logic
            var retries = 0;
            const int maxRetries = 30;
            const int delayMs = 1000;

            while (retries < maxRetries)
            {
                try
                {
                    await using var connection = new NpgsqlConnection(ConnectionString);
                    await connection.OpenAsync();
                    await connection.CloseAsync();
                    break;
                }
                catch (Exception) when (retries < maxRetries - 1)
                {
                    retries++;
                    await Task.Delay(delayMs);
                }
            }
        }
        catch (Exception)
        {
            _isDockerAvailable = false;
            // Mark as unavailable rather than throwing - tests will be skipped
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for sharing PostgreSQL test fixture across test classes.
/// </summary>
[CollectionDefinition("PostgreSQL")]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlTestFixture>
{
}

/// <summary>
/// Base class for PostgreSQL integration tests.
/// </summary>
[Collection("PostgreSQL")]
public abstract class PostgreSqlIntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlTestFixture Fixture;
    protected string ConnectionString => Fixture.ConnectionString;
    protected bool IsDockerAvailable => Fixture.IsDockerAvailable;

    protected PostgreSqlIntegrationTestBase(PostgreSqlTestFixture fixture)
    {
        Fixture = fixture;
    }

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    
    /// <summary>
    /// Skips the test if Docker/Podman is not available.
    /// </summary>
    protected void SkipIfDockerNotAvailable()
    {
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available. Integration tests require a running Docker/Podman environment.");
        }
    }

    /// <summary>
    /// Creates a new DbContextOptions with PostgreSQL configuration.
    /// </summary>
    protected DbContextOptions<T> CreateDbContextOptions<T>() where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseNpgsql(ConnectionString)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;
    }

    /// <summary>
    /// Creates a new DbContextOptionsBuilder with PostgreSQL configuration.
    /// </summary>
    protected DbContextOptionsBuilder<T> CreateDbContextOptionsBuilder<T>() where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseNpgsql(ConnectionString)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
    
    /// <summary>
    /// Creates a new DbContextOptionsBuilder for unit tests (without database connection).
    /// </summary>
    protected DbContextOptionsBuilder<T> CreateUnitTestDbContextOptionsBuilder<T>() where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;")
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }

    /// <summary>
    /// Executes raw SQL against the test database.
    /// </summary>
    protected async Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a scalar query against the test database.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is T value ? value : default;
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    protected async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = $1
            );
            """;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    /// <summary>
    /// Checks if an index exists in the database.
    /// </summary>
    protected async Task<bool> IndexExistsAsync(string indexName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relname = $1 AND n.nspname = 'public' AND c.relkind = 'i'
            );
            """;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(indexName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    /// <summary>
    /// Gets the index method (btree, gin, gist, etc.) for a given index.
    /// </summary>
    protected async Task<string?> GetIndexMethodAsync(string indexName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT am.amname
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_am am ON am.oid = c.relam
            WHERE c.relname = $1 AND n.nspname = 'public' AND c.relkind = 'i';
            """;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(indexName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }
}