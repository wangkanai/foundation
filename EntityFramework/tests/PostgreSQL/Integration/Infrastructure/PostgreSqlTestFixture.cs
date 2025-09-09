// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;

/// <summary>
/// Base test fixture for PostgreSQL integration tests using Testcontainers.
/// Provides a real PostgreSQL database instance for testing.
/// </summary>
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlTestFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Wait for PostgreSQL to be fully ready
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
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
    protected readonly ITestOutputHelper Output;
    protected string ConnectionString => Fixture.ConnectionString;

    protected PostgreSqlIntegrationTestBase(PostgreSqlTestFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates a new DbContextOptions with PostgreSQL configuration.
    /// </summary>
    protected DbContextOptions<T> CreateDbContextOptions<T>() where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseNpgsql(ConnectionString)
            .LogTo(Output.WriteLine, LogLevel.Information)
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
            .LogTo(Output.WriteLine, LogLevel.Information)
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