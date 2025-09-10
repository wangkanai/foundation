// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using NpgsqlTypes;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL full-text search extensions.
/// Tests tsvector, tsquery, text search configurations, ranking, and highlighting.
/// </summary>
public sealed class FullTextSearchExtensionsTests : PostgreSqlIntegrationTestBase
{
    public FullTextSearchExtensionsTests(PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task HasTsVectorType_ShouldConfigureTsVectorColumn()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<FullTextSearchTestDbContext>();

        // Act
        await using var context = new FullTextSearchTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Verify tsvector column type
        var columnType = await ExecuteScalarAsync<string>("""
            SELECT data_type 
            FROM information_schema.columns 
            WHERE table_name = 'document_entities' AND column_name = 'search_vector';
            """);

        columnType.Should().Be("tsvector");
    }

    [Fact]
    public async Task FullTextSearch_ShouldWorkWithTsVectorQueries()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<FullTextSearchTestDbContext>();

        await using var context = new FullTextSearchTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var documents = new[]
        {
            new DocumentEntity
            {
                Title = "PostgreSQL Tutorial",
                Content = "Learn PostgreSQL database management system with full-text search capabilities.",
                Categories = ["database", "tutorial"],
                PublishedAt = DateTime.Today.AddDays(-5)
            },
            new DocumentEntity
            {
                Title = "Advanced SQL Queries",
                Content = "Master complex SQL queries including CTEs, window functions, and advanced joins.",
                Categories = ["sql", "advanced"],
                PublishedAt = DateTime.Today.AddDays(-10)
            },
            new DocumentEntity
            {
                Title = "Database Design Principles",
                Content = "Essential principles for designing efficient and scalable database schemas.",
                Categories = ["design", "principles"],
                PublishedAt = DateTime.Today.AddDays(-2)
            }
        };

        await context.Documents.AddRangeAsync(documents);
        await context.SaveChangesAsync();

        // Update tsvector manually for testing
        await ExecuteSqlAsync("""
            UPDATE document_entities 
            SET search_vector = to_tsvector('english', title || ' ' || content);
            """);

        // Act & Assert - Test full-text search queries

        // Test simple text search
        var postgresqlDocs = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM document_entities 
            WHERE search_vector @@ to_tsquery('english', 'postgresql');
            """);
        postgresqlDocs.Should().Be(1);

        // Test phrase search
        var sqlDocs = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM document_entities 
            WHERE search_vector @@ plainto_tsquery('english', 'SQL queries');
            """);
        sqlDocs.Should().Be(1);

        // Test ranking
        var topDoc = await ExecuteScalarAsync<string>("""
            SELECT title 
            FROM document_entities 
            WHERE search_vector @@ to_tsquery('english', 'database') 
            ORDER BY ts_rank(search_vector, to_tsquery('english', 'database')) DESC 
            LIMIT 1;
            """);
        topDoc.Should().NotBeNullOrEmpty();
    }
}

/// <summary>
/// Test DbContext for full-text search testing.
/// </summary>
public class FullTextSearchTestDbContext : DbContext
{
    public FullTextSearchTestDbContext(DbContextOptions<FullTextSearchTestDbContext> options) : base(options) { }

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.Categories).HasArrayType("text");
            entity.Property(e => e.PublishedAt).IsRequired();
            entity.Property(e => e.SearchVector).HasColumnType("tsvector");
        });
    }
}