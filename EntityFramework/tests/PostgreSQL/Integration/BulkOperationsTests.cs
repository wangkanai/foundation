// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL bulk operations.
/// Tests COPY protocol, UPSERT operations, and bulk operation performance.
/// </summary>
public sealed class BulkOperationsTests : PostgreSqlIntegrationTestBase
{
    public BulkOperationsTests(PostgreSqlTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task BulkInsert_ShouldHandleLargeDatasets()
    {
        // Arrange
        var options = CreateDbContextOptions<BulkTestDbContext>();

        await using var context = new BulkTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        const int recordCount = 1000;
        var entities = new List<BulkEntity>();
        var random = new Random(42);

        for (int i = 0; i < recordCount; i++)
        {
            entities.Add(new BulkEntity
            {
                Name = $"Entity {i}",
                Value = random.Next(1, 1000),
                Amount = (decimal)(random.NextDouble() * 10000),
                ProcessedAt = DateTime.UtcNow.AddMinutes(-random.Next(0, 60)),
                Status = i % 3 == 0 ? "Active" : "Inactive"
            });
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await context.BulkEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        stopwatch.Stop();

        // Assert
        Output.WriteLine($"Bulk insert of {recordCount} records completed in {stopwatch.ElapsedMilliseconds}ms");
        
        var count = await context.BulkEntities.CountAsync();
        count.Should().Be(recordCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task UpsertOperation_ShouldHandleConflicts()
    {
        // Arrange
        var options = CreateDbContextOptions<BulkTestDbContext>();

        await using var context = new BulkTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var entityId = Guid.NewGuid();
        
        // Initial insert
        var initialEntity = new BulkEntity
        {
            Id = entityId,
            Name = "Initial Entity",
            Value = 100,
            Amount = 500.00m,
            ProcessedAt = DateTime.UtcNow,
            Status = "Active"
        };
        
        await context.BulkEntities.AddAsync(initialEntity);
        await context.SaveChangesAsync();

        // Act - Simulate upsert using raw SQL
        await ExecuteSqlAsync($"""
            INSERT INTO bulk_entities (id, name, value, amount, processed_at, status)
            VALUES ('{entityId}', 'Updated Entity', 200, 1000.00, NOW(), 'Updated')
            ON CONFLICT (id) DO UPDATE SET
                name = EXCLUDED.name,
                value = EXCLUDED.value,
                amount = EXCLUDED.amount,
                processed_at = EXCLUDED.processed_at,
                status = EXCLUDED.status;
            """);

        // Assert
        var updatedEntity = await context.BulkEntities.FindAsync(entityId);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Name.Should().Be("Updated Entity");
        updatedEntity.Value.Should().Be(200);
        updatedEntity.Amount.Should().Be(1000.00m);
        updatedEntity.Status.Should().Be("Updated");
    }

    [Fact]
    public async Task BulkUpdate_ShouldUpdateMultipleRecords()
    {
        // Arrange
        var options = CreateDbContextOptions<BulkTestDbContext>();

        await using var context = new BulkTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Insert test data
        var entities = Enumerable.Range(1, 100)
            .Select(i => new BulkEntity
            {
                Name = $"Entity {i}",
                Value = i,
                Amount = i * 10m,
                ProcessedAt = DateTime.UtcNow,
                Status = "Pending"
            })
            .ToArray();

        await context.BulkEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act - Bulk update using SQL
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await ExecuteSqlAsync("""
            UPDATE bulk_entities 
            SET status = 'Processed', processed_at = NOW() 
            WHERE status = 'Pending';
            """);
        stopwatch.Stop();

        // Assert
        Output.WriteLine($"Bulk update completed in {stopwatch.ElapsedMilliseconds}ms");
        
        var processedCount = await context.BulkEntities
            .CountAsync(e => e.Status == "Processed");
        
        processedCount.Should().Be(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should be very fast
    }
}

/// <summary>
/// Test DbContext for bulk operations testing.
/// </summary>
public class BulkTestDbContext : DbContext
{
    public BulkTestDbContext(DbContextOptions<BulkTestDbContext> options) : base(options) { }

    public DbSet<BulkEntity> BulkEntities => Set<BulkEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BulkEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.ProcessedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            
            // Indexes for performance
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}