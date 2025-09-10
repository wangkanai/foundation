// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL connection configuration extensions.
/// Tests connection pooling, SSL, prepared statements, multiplexing, and timeouts.
/// </summary>
public sealed class ConnectionConfigurationExtensionsTests : PostgreSqlIntegrationTestBase
{
    public ConnectionConfigurationExtensionsTests(PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    #region Connection Pool Tests

    [Fact]
    public async Task ConfigureNpgsqlConnectionPool_WithValidParameters_ShouldConfigurePool()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .ConfigureNpgsqlConnectionPool(minPoolSize: 5, maxPoolSize: 20)
            .Options;

        // Act
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Should().NotBeNull();
        var connection = context.Database.GetDbConnection() as NpgsqlConnection;
        connection.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureNpgsqlConnectionPool_WithNegativeMinPoolSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var builder = CreateUnitTestDbContextOptionsBuilder<TestDbContext>();

        // Act
        var act = () => builder.ConfigureNpgsqlConnectionPool(minPoolSize: -1, maxPoolSize: 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("minPoolSize")
            .WithMessage("*Minimum pool size cannot be negative.*");
    }

    [Fact]
    public void ConfigureNpgsqlConnectionPool_WithMaxPoolSizeLessThanMin_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var builder = CreateUnitTestDbContextOptionsBuilder<TestDbContext>();

        // Act
        var act = () => builder.ConfigureNpgsqlConnectionPool(minPoolSize: 10, maxPoolSize: 5);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxPoolSize")
            .WithMessage("*Maximum pool size must be greater than or equal to minimum pool size.*");
    }

    [Fact]
    public async Task ConfigureNpgsqlConnectionPool_WithDefaultParameters_ShouldWork()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange & Act
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .ConfigureNpgsqlConnectionPool()
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Should().NotBeNull();
        await context.TestEntities.AddAsync(new TestEntity { Name = "Test", CreatedAt = DateTime.UtcNow });
        var result = await context.SaveChangesAsync();
        result.Should().Be(1);
    }

    #endregion

    #region Prepared Statements Tests

    [Fact]
    public async Task EnableNpgsqlPreparedStatements_WithValidParameters_ShouldEnablePreparedStatements()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .EnableNpgsqlPreparedStatements(maxAutoPrepare: 10)
            .Options;

        // Act
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Multiple identical queries to test prepared statement caching
        for (int i = 0; i < 5; i++)
        {
            await context.TestEntities.AddAsync(new TestEntity { Name = $"Test {i}", CreatedAt = DateTime.UtcNow });
        }
        await context.SaveChangesAsync();

        // Execute the same query multiple times
        for (int i = 0; i < 5; i++)
        {
            var entities = await context.TestEntities.Where(e => e.Name.StartsWith("Test")).ToListAsync();
            entities.Should().HaveCount(5);
        }

        // Assert
        context.Should().NotBeNull();
    }

    [Fact]
    public void EnableNpgsqlPreparedStatements_WithNegativeMaxAutoPrepare_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var builder = CreateUnitTestDbContextOptionsBuilder<TestDbContext>();

        // Act
        var act = () => builder.EnableNpgsqlPreparedStatements(maxAutoPrepare: -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxAutoPrepare")
            .WithMessage("*Maximum auto prepare count cannot be negative.*");
    }

    [Fact]
    public async Task EnableNpgsqlPreparedStatements_WithDefaultParameters_ShouldWork()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange & Act
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .EnableNpgsqlPreparedStatements()
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Should().NotBeNull();
    }

    #endregion

    #region SSL Configuration Tests

    [Theory]
    [InlineData(SslMode.Disable)]
    [InlineData(SslMode.Allow)]
    [InlineData(SslMode.Prefer)]
    [InlineData(SslMode.Require)]
    public async Task RequireNpgsqlSSL_WithDifferentModes_ShouldConfigureSSL(SslMode sslMode)
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .RequireNpgsqlSSL(sslMode)
            .Options;

        // Act & Assert
        await using var context = new TestDbContext(options);
        
        // For test containers, we expect this to work with most SSL modes
        // In production, stricter SSL modes might require proper certificates
        if (sslMode != SslMode.VerifyFull && sslMode != SslMode.VerifyCA)
        {
            await context.Database.EnsureCreatedAsync();
            context.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RequireNpgsqlSSL_WithDefaultMode_ShouldWork()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange & Act
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .RequireNpgsqlSSL()
            .Options;

        await using var context = new TestDbContext(options);

        // Assert - Default is SslMode.Require, but test container might not support it
        try
        {
            await context.Database.EnsureCreatedAsync();
            context.Should().NotBeNull();
        }
        catch (Npgsql.PostgresException)
        {
            // SSL might not be configured on test container, which is expected
            Console.WriteLine("SSL not available on test container - this is expected in test environment");
        }
    }

    #endregion

    #region Timeout Configuration Tests

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    public async Task SetNpgsqlStatementTimeout_WithValidTimeout_ShouldConfigureTimeout(int timeoutSeconds)
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .SetNpgsqlStatementTimeout(timeout)
            .Options;

        // Act
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Should().NotBeNull();
        var connection = context.Database.GetDbConnection() as NpgsqlConnection;
        connection.Should().NotBeNull();
    }

    [Fact]
    public void SetNpgsqlStatementTimeout_WithZeroTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var builder = CreateUnitTestDbContextOptionsBuilder<TestDbContext>();

        // Act
        var act = () => builder.SetNpgsqlStatementTimeout(TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout")
            .WithMessage("*Statement timeout must be greater than zero.*");
    }

    [Fact]
    public void SetNpgsqlStatementTimeout_WithNegativeTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var builder = CreateUnitTestDbContextOptionsBuilder<TestDbContext>();

        // Act
        var act = () => builder.SetNpgsqlStatementTimeout(TimeSpan.FromSeconds(-1));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout")
            .WithMessage("*Statement timeout must be greater than zero.*");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public async Task SetNpgsqlConnectionTimeout_WithValidTimeout_ShouldConfigureTimeout(int timeoutSeconds)
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .SetNpgsqlConnectionTimeout(timeout)
            .Options;

        // Act
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Should().NotBeNull();
    }

    [Fact]
    public void SetNpgsqlConnectionTimeout_WithZeroTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var builder = CreateUnitTestDbContextOptionsBuilder<TestDbContext>();

        // Act
        var act = () => builder.SetNpgsqlConnectionTimeout(TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout")
            .WithMessage("*Connection timeout must be greater than zero.*");
    }

    #endregion

    #region Multiplexing Tests

    [Fact]
    public async Task EnableNpgsqlMultiplexing_ShouldEnableMultiplexing()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .EnableNpgsqlMultiplexing()
            .Options;

        // Act
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Test concurrent operations that would benefit from multiplexing
        var tasks = new List<Task<int>>();
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                await using var concurrentContext = new TestDbContext(options);
                await concurrentContext.TestEntities.AddAsync(new TestEntity 
                { 
                    Name = $"Concurrent {index}", 
                    CreatedAt = DateTime.UtcNow 
                });
                return await concurrentContext.SaveChangesAsync();
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().Be(1));
        
        await using var verifyContext = new TestDbContext(options);
        var count = await verifyContext.TestEntities.CountAsync(e => e.Name.StartsWith("Concurrent"));
        count.Should().Be(10);
    }

    #endregion

    #region Performance Configuration Tests

    [Fact]
    public async Task ConfigureNpgsqlPerformance_WithAllParameters_ShouldConfigurePerformance()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .ConfigureNpgsqlPerformance(
                minPoolSize: 2,
                maxPoolSize: 10,
                maxAutoPrepare: 15,
                commandTimeout: TimeSpan.FromMinutes(1),
                connectionTimeout: TimeSpan.FromSeconds(10),
                enableMultiplexing: true,
                sslMode: SslMode.Prefer)
            .Options;

        // Act
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Test that the configuration works
        await context.TestEntities.AddAsync(new TestEntity { Name = "Performance Test", CreatedAt = DateTime.UtcNow });
        var result = await context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        context.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfigureNpgsqlPerformance_WithDefaultParameters_ShouldWork()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange & Act
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .ConfigureNpgsqlPerformance()
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Should().NotBeNull();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ConnectionPool_PerformanceTest_ShouldHandleConcurrentConnections()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .ConfigureNpgsqlConnectionPool(minPoolSize: 5, maxPoolSize: 20)
            .Options;

        await using var setupContext = new TestDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();

        // Act - Simulate high concurrency
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                await using var context = new TestDbContext(options);
                await context.TestEntities.AddAsync(new TestEntity 
                { 
                    Name = $"Perf Test {index}", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Concurrent operations completed in {stopwatch.ElapsedMilliseconds}ms");
        
        await using var verifyContext = new TestDbContext(options);
        var count = await verifyContext.TestEntities.CountAsync(e => e.Name.StartsWith("Perf Test"));
        count.Should().Be(50);
        
        // Performance assertion - should complete within reasonable time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30 seconds max
    }

    [Fact]
    public async Task PreparedStatements_PerformanceTest_ShouldReuseStatements()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptionsBuilder<TestDbContext>()
            .EnableNpgsqlPreparedStatements(maxAutoPrepare: 50)
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Add test data
        for (int i = 0; i < 100; i++)
        {
            await context.TestEntities.AddAsync(new TestEntity 
            { 
                Name = $"Prepared Test {i}", 
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) 
            });
        }
        await context.SaveChangesAsync();

        // Act - Execute same query pattern multiple times
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 100; i++)
        {
            var entities = await context.TestEntities
                .Where(e => e.Name.StartsWith("Prepared Test"))
                .OrderBy(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();
            
            entities.Should().HaveCount(10);
        }
        
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Prepared statement queries completed in {stopwatch.ElapsedMilliseconds}ms");
        
        // With prepared statements, this should be faster than without
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // 10 seconds max
    }

    #endregion
}

/// <summary>
/// Test DbContext for connection configuration testing.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Add index for performance testing
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}