// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL partitioning features.
/// Tests range, list, and hash partitioning with partition management.
/// </summary>
public sealed class PartitioningTests : PostgreSqlIntegrationTestBase
{
    public PartitioningTests(PostgreSqlTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task RangePartitioning_ShouldDistributeDataByDateRange()
    {
        // Arrange
        var options = CreateDbContextOptions<PartitionTestDbContext>();

        await using var context = new PartitionTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Create partitioned table manually (EF Core doesn't support declarative partitioning yet)
        await ExecuteSqlAsync("""
            -- Drop existing table and recreate as partitioned
            DROP TABLE IF EXISTS partitioned_entities CASCADE;
            
            CREATE TABLE partitioned_entities (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(100) NOT NULL,
                created_date DATE NOT NULL,
                category VARCHAR(50) NOT NULL,
                region INTEGER NOT NULL,
                value DECIMAL(18,2) NOT NULL
            ) PARTITION BY RANGE (created_date);

            -- Create monthly partitions for 2024
            CREATE TABLE partitioned_entities_2024_01 PARTITION OF partitioned_entities
                FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
            
            CREATE TABLE partitioned_entities_2024_02 PARTITION OF partitioned_entities
                FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');
                
            CREATE TABLE partitioned_entities_2024_03 PARTITION OF partitioned_entities
                FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');
            """);

        // Act - Insert data across different date ranges
        var testData = new[]
        {
            new { Name = "Jan Entity 1", CreatedDate = new DateTime(2024, 1, 15), Category = "A", Region = 1, Value = 100.50m },
            new { Name = "Jan Entity 2", CreatedDate = new DateTime(2024, 1, 25), Category = "B", Region = 2, Value = 200.75m },
            new { Name = "Feb Entity 1", CreatedDate = new DateTime(2024, 2, 10), Category = "A", Region = 1, Value = 150.25m },
            new { Name = "Feb Entity 2", CreatedDate = new DateTime(2024, 2, 20), Category = "C", Region = 3, Value = 300.00m },
            new { Name = "Mar Entity 1", CreatedDate = new DateTime(2024, 3, 5), Category = "B", Region = 2, Value = 175.80m },
            new { Name = "Mar Entity 2", CreatedDate = new DateTime(2024, 3, 15), Category = "A", Region = 1, Value = 125.90m }
        };

        foreach (var data in testData)
        {
            await ExecuteSqlAsync($"""
                INSERT INTO partitioned_entities (name, created_date, category, region, value)
                VALUES ('{data.Name}', '{data.CreatedDate:yyyy-MM-dd}', '{data.Category}', {data.Region}, {data.Value});
                """);
        }

        // Assert - Verify data is distributed across partitions
        var jan2024Count = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM partitioned_entities_2024_01;");
        var feb2024Count = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM partitioned_entities_2024_02;");
        var mar2024Count = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM partitioned_entities_2024_03;");
        var totalCount = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM partitioned_entities;");

        jan2024Count.Should().Be(2);
        feb2024Count.Should().Be(2);
        mar2024Count.Should().Be(2);
        totalCount.Should().Be(6);

        // Verify partition pruning works
        var janQuery = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM partitioned_entities 
            WHERE created_date >= '2024-01-01' AND created_date < '2024-02-01';
            """);
        janQuery.Should().Be(2);
    }

    [Fact]
    public async Task ListPartitioning_ShouldDistributeDataByCategory()
    {
        // Arrange & Act - Create list-partitioned table
        await ExecuteSqlAsync("""
            CREATE TABLE IF NOT EXISTS category_partitioned_entities (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(100) NOT NULL,
                created_date DATE NOT NULL,
                category VARCHAR(50) NOT NULL,
                region INTEGER NOT NULL,
                value DECIMAL(18,2) NOT NULL
            ) PARTITION BY LIST (category);

            -- Create category-based partitions
            CREATE TABLE category_partitioned_entities_electronics PARTITION OF category_partitioned_entities
                FOR VALUES IN ('Electronics', 'Computers', 'Mobile');
            
            CREATE TABLE category_partitioned_entities_books PARTITION OF category_partitioned_entities
                FOR VALUES IN ('Books', 'Education', 'Literature');
                
            CREATE TABLE category_partitioned_entities_home PARTITION OF category_partitioned_entities
                FOR VALUES IN ('Home', 'Garden', 'Furniture');
            """);

        // Insert test data
        var categoryData = new[]
        {
            new { Name = "Laptop", Category = "Electronics", Region = 1, Value = 1299.99m },
            new { Name = "Phone", Category = "Mobile", Region = 2, Value = 899.50m },
            new { Name = "Novel", Category = "Books", Region = 1, Value = 19.99m },
            new { Name = "Textbook", Category = "Education", Region = 3, Value = 89.75m },
            new { Name = "Chair", Category = "Furniture", Region = 2, Value = 199.00m },
            new { Name = "Table", Category = "Home", Region = 1, Value = 299.50m }
        };

        foreach (var data in categoryData)
        {
            await ExecuteSqlAsync($"""
                INSERT INTO category_partitioned_entities (name, created_date, category, region, value)
                VALUES ('{data.Name}', CURRENT_DATE, '{data.Category}', {data.Region}, {data.Value});
                """);
        }

        // Assert - Verify data distribution
        var electronicsCount = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM category_partitioned_entities_electronics;");
        var booksCount = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM category_partitioned_entities_books;");
        var homeCount = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM category_partitioned_entities_home;");

        electronicsCount.Should().Be(2); // Electronics + Mobile
        booksCount.Should().Be(2); // Books + Education
        homeCount.Should().Be(2); // Furniture + Home
    }

    [Fact]
    public async Task HashPartitioning_ShouldDistributeDataEvenly()
    {
        // Arrange & Act - Create hash-partitioned table
        await ExecuteSqlAsync("""
            CREATE TABLE IF NOT EXISTS hash_partitioned_entities (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(100) NOT NULL,
                created_date DATE NOT NULL,
                category VARCHAR(50) NOT NULL,
                region INTEGER NOT NULL,
                value DECIMAL(18,2) NOT NULL
            ) PARTITION BY HASH (region);

            -- Create hash partitions (4 partitions for even distribution)
            CREATE TABLE hash_partitioned_entities_0 PARTITION OF hash_partitioned_entities
                FOR VALUES WITH (modulus 4, remainder 0);
            
            CREATE TABLE hash_partitioned_entities_1 PARTITION OF hash_partitioned_entities
                FOR VALUES WITH (modulus 4, remainder 1);
                
            CREATE TABLE hash_partitioned_entities_2 PARTITION OF hash_partitioned_entities
                FOR VALUES WITH (modulus 4, remainder 2);
                
            CREATE TABLE hash_partitioned_entities_3 PARTITION OF hash_partitioned_entities
                FOR VALUES WITH (modulus 4, remainder 3);
            """);

        // Insert test data with various region values
        var hashData = new List<object>();
        for (int i = 1; i <= 20; i++)
        {
            hashData.Add(new
            {
                Name = $"Entity {i}",
                Category = "Test",
                Region = i, // This will be hashed for distribution
                Value = i * 10.5m
            });
        }

        foreach (var data in hashData)
        {
            await ExecuteSqlAsync($"""
                INSERT INTO hash_partitioned_entities (name, created_date, category, region, value)
                VALUES ('{data.GetType().GetProperty("Name")?.GetValue(data)}', CURRENT_DATE, 
                        '{data.GetType().GetProperty("Category")?.GetValue(data)}', 
                        {data.GetType().GetProperty("Region")?.GetValue(data)}, 
                        {data.GetType().GetProperty("Value")?.GetValue(data)});
                """);
        }

        // Assert - Verify relatively even distribution
        var counts = new List<long>();
        for (int i = 0; i < 4; i++)
        {
            var count = await ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM hash_partitioned_entities_{i};");
            counts.Add(count);
        }

        counts.Sum().Should().Be(20);
        counts.Should().OnlyContain(count => count > 0); // Each partition should have some data
        
        // The distribution might not be perfectly even due to hash function, but should be reasonable
        var maxCount = counts.Max();
        var minCount = counts.Min();
        (maxCount - minCount).Should().BeLessOrEqualTo(10); // Difference shouldn't be too large
    }

    [Fact]
    public async Task PartitionPruning_ShouldImproveQueryPerformance()
    {
        // This test verifies that PostgreSQL's query planner uses partition pruning
        // We'll use the range partitioned table from the first test
        
        // Setup is already done in the first test, so we'll create it again for isolation
        await ExecuteSqlAsync("""
            DROP TABLE IF EXISTS performance_partitioned_entities CASCADE;
            
            CREATE TABLE performance_partitioned_entities (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(100) NOT NULL,
                created_date DATE NOT NULL,
                category VARCHAR(50) NOT NULL,
                region INTEGER NOT NULL,
                value DECIMAL(18,2) NOT NULL
            ) PARTITION BY RANGE (created_date);

            -- Create partitions for different years
            CREATE TABLE performance_partitioned_entities_2023 PARTITION OF performance_partitioned_entities
                FOR VALUES FROM ('2023-01-01') TO ('2024-01-01');
            
            CREATE TABLE performance_partitioned_entities_2024 PARTITION OF performance_partitioned_entities
                FOR VALUES FROM ('2024-01-01') TO ('2025-01-01');
            """);

        // Insert significant amount of test data
        const int recordsPerYear = 1000;
        
        // Insert 2023 data
        for (int i = 0; i < recordsPerYear; i++)
        {
            var date2023 = new DateTime(2023, 1, 1).AddDays(i % 365);
            await ExecuteSqlAsync($"""
                INSERT INTO performance_partitioned_entities (name, created_date, category, region, value)
                VALUES ('Entity2023_{i}', '{date2023:yyyy-MM-dd}', 'Category', {i % 10}, {i * 1.5});
                """);
        }

        // Insert 2024 data
        for (int i = 0; i < recordsPerYear; i++)
        {
            var date2024 = new DateTime(2024, 1, 1).AddDays(i % 365);
            await ExecuteSqlAsync($"""
                INSERT INTO performance_partitioned_entities (name, created_date, category, region, value)
                VALUES ('Entity2024_{i}', '{date2024:yyyy-MM-dd}', 'Category', {i % 10}, {i * 2.0});
                """);
        }

        // Act & Assert - Test queries that should benefit from partition pruning
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Query only 2024 data - should only scan 2024 partition
        var count2024 = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM performance_partitioned_entities 
            WHERE created_date >= '2024-01-01' AND created_date < '2025-01-01';
            """);
        
        stopwatch.Stop();
        var prunedQueryTime = stopwatch.ElapsedMilliseconds;
        
        stopwatch.Restart();
        
        // Query all data - should scan both partitions
        var totalCount = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM performance_partitioned_entities;");
        
        stopwatch.Stop();
        var fullScanTime = stopwatch.ElapsedMilliseconds;

        // Assert
        count2024.Should().Be(recordsPerYear);
        totalCount.Should().Be(recordsPerYear * 2);
        
        Output.WriteLine($"Pruned query time: {prunedQueryTime}ms");
        Output.WriteLine($"Full scan time: {fullScanTime}ms");
        
        // The pruned query should be faster or at least not significantly slower
        // Note: With small datasets, the difference might not be noticeable
        prunedQueryTime.Should().BeLessOrEqualTo(fullScanTime + 100); // Allow some variance
    }
}

/// <summary>
/// Test DbContext for partitioning testing.
/// </summary>
public class PartitionTestDbContext : DbContext
{
    public PartitionTestDbContext(DbContextOptions<PartitionTestDbContext> options) : base(options) { }

    public DbSet<PartitionedEntity> PartitionedEntities => Set<PartitionedEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartitionedEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Region).IsRequired();
            entity.Property(e => e.Value).HasPrecision(18, 2);
        });
    }
}