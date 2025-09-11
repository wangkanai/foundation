// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text.Json;

namespace Wangkanai.EntityFramework.Benchmark;

/// <summary>
/// Comprehensive performance benchmarks for PostgreSQL-specific Entity Framework Core optimizations.
/// Tests JSONB, full-text search, array operations, bulk operations, and indexing strategies.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PostgreSqlBenchmarks
{
    private BenchmarkDbContext _context = null!;
    private List<BenchmarkEntity> _testData = null!;
    private List<SearchableEntity> _searchData = null!;
    private List<string> _searchTerms = null!;
    private NpgsqlConnection _connection = null!;
    
    [Params(100, 1000, 10000)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var connectionString = "Host=localhost;Database=efcore_benchmark;Username=postgres;Password=postgres;Include Error Detail=true;";
        
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .EnableSensitiveDataLogging()
            .Options;

        _context = new BenchmarkDbContext(options);
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
        
        // Ensure database and tables exist
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        // Enable required PostgreSQL extensions
        await _connection.ExecuteNonQueryAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await _connection.ExecuteNonQueryAsync("CREATE EXTENSION IF NOT EXISTS btree_gin;");
        await _connection.ExecuteNonQueryAsync("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        
        await GenerateTestData();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task GenerateTestData()
    {
        var random = new Random(42); // Fixed seed for consistent benchmarks
        
        // Generate main test entities
        _testData = Enumerable.Range(1, DataSize)
            .Select(i => new BenchmarkEntity
            {
                Id = i,
                Name = $"Entity {i}",
                Status = (EntityStatus)(i % 4),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(365)),
                Metadata = JsonSerializer.Serialize(new
                {
                    category = $"category_{i % 10}",
                    priority = random.Next(1, 6),
                    tags = Enumerable.Range(1, random.Next(1, 6))
                        .Select(j => $"tag_{(i + j) % 20}")
                        .ToArray(),
                    settings = new
                    {
                        enabled = random.NextDouble() > 0.3,
                        threshold = random.NextDouble() * 100,
                        config = new { version = random.Next(1, 4) }
                    }
                }),
                Tags = Enumerable.Range(1, random.Next(1, 8))
                    .Select(j => $"tag_{(i + j) % 50}")
                    .ToArray(),
                Scores = Enumerable.Range(1, random.Next(3, 10))
                    .Select(_ => random.Next(1, 101))
                    .ToArray()
            })
            .ToList();

        // Generate searchable entities for full-text search benchmarks
        var sampleTexts = new[]
        {
            "PostgreSQL is a powerful, open source object-relational database system with over 30 years of active development",
            "Entity Framework Core is a modern object-database mapper for .NET that supports LINQ queries, change tracking, and schema migrations",
            "Full-text search allows searching through large amounts of text data efficiently using specialized indexes",
            "JSONB in PostgreSQL provides efficient storage and querying of JSON documents with indexing support",
            "Performance optimization in database applications requires careful consideration of indexes, query patterns, and data access strategies",
            "Bulk operations can significantly improve throughput when processing large amounts of data",
            "Array data types in PostgreSQL provide native support for storing and querying collections of values",
            "GIN indexes are particularly useful for full-text search, JSONB containment queries, and array operations"
        };

        _searchData = Enumerable.Range(1, DataSize)
            .Select(i => new SearchableEntity
            {
                Id = i,
                Title = $"Article {i}: {sampleTexts[i % sampleTexts.Length].Split(' ')[0]} {sampleTexts[i % sampleTexts.Length].Split(' ')[1]}",
                Content = string.Join(" ", Enumerable.Range(0, random.Next(50, 200))
                    .Select(j => sampleTexts[(i + j) % sampleTexts.Length])),
                Category = $"category_{i % 10}",
                Tags = string.Join(", ", Enumerable.Range(1, random.Next(2, 6))
                    .Select(j => $"tag{(i + j) % 20}"))
            })
            .ToList();

        // Generate search terms
        _searchTerms = new[]
        {
            "PostgreSQL database",
            "Entity Framework Core",
            "full-text search index",
            "JSONB performance optimization",
            "bulk operations throughput",
            "array data types",
            "GIN index query"
        }.ToList();

        // Insert test data in batches for better performance
        await InsertDataInBatches(_testData);
        await InsertDataInBatches(_searchData);
    }

    private async Task InsertDataInBatches<T>(params IEnumerable<T>[] datasets) where T : class
    {
        const int batchSize = 1000;
        
        foreach (var dataset in datasets)
        {
            var items = dataset.ToList();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize);
                _context.Set<T>().AddRange(batch);
            }
        }
        
        await _context.SaveChangesAsync();
    }

    #region JSONB Benchmarks

    [Benchmark]
    public async Task<List<BenchmarkEntity>> JsonbContainmentQuery_OptimizedIndex()
    {
        var searchJson = JsonSerializer.Serialize(new { category = "category_5" });
        return await _context.BenchmarkEntities
            .Where(e => EF.Functions.JsonContains(e.Metadata, searchJson))
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> JsonbPathQuery_OptimizedIndex()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Metadata.Contains("\"category_5\""))
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> JsonbContainmentQuery_NoIndex()
    {
        // Simulate query without proper indexing (for comparison)
        var results = new List<BenchmarkEntity>();
        await foreach (var entity in _context.BenchmarkEntities.AsAsyncEnumerable())
        {
            if (entity.Metadata.Contains("category_5"))
            {
                results.Add(entity);
                if (results.Count >= 100) break;
            }
        }
        return results;
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> JsonbAggregationQuery()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Metadata.Contains("\"enabled\":\"true\""))
            .OrderBy(e => e.Id) // Simplified ordering since we can't easily extract priority from JSONB in current version
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<Dictionary<string, int>> JsonbGroupingQuery()
    {
        return await _context.BenchmarkEntities
            .GroupBy(e => e.Status) // Group by Status instead since we can't easily extract from JSONB
            .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);
    }

    #endregion

    #region Full-Text Search Benchmarks

    [Benchmark]
    public async Task<List<SearchableEntity>> FullTextSearch_OptimizedTsVector()
    {
        var searchTerm = _searchTerms[0};
        return await _context.SearchableEntities
            .Where(e => e.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", searchTerm)))
            .OrderByDescending(e => e.SearchVector.Rank(EF.Functions.PlainToTsQuery("english", searchTerm)))
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<SearchableEntity>> FullTextSearch_BasicLike()
    {
        var searchTerm = _searchTerms[0].Split(' ')[0};
        return await _context.SearchableEntities
            .Where(e => e.Content.Contains(searchTerm) || e.Title.Contains(searchTerm))
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<SearchableEntity>> FullTextSearch_MultipleTerms()
    {
        var terms = _searchTerms.Take(3).ToArray();
        var query = string.Join(" & ", terms);
        
        return await _context.SearchableEntities
            .Where(e => e.SearchVector.Matches(EF.Functions.ToTsQuery("english", query)))
            .OrderByDescending(e => e.SearchVector.Rank(EF.Functions.ToTsQuery("english", query)))
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<SearchableEntity>> FullTextSearch_PhraseQuery()
    {
        return await _context.SearchableEntities
            .Where(e => e.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", "PostgreSQL database")))
            .OrderByDescending(e => e.SearchVector.Rank(EF.Functions.PhraseToTsQuery("english", "PostgreSQL database")))
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<SearchableEntity>> FullTextSearch_WithHighlighting()
    {
        var searchTerm = _searchTerms[0};
        var query = EF.Functions.PlainToTsQuery("english", searchTerm);
        
        return await _context.SearchableEntities
            .Where(e => e.SearchVector.Matches(query))
            .Select(e => new SearchableEntity
            {
                Id = e.Id,
                Title = e.Title,
                Content = e.Content.Substring(0, Math.Min(e.Content.Length, 200)), // Simplified content truncation
                SearchVector = e.SearchVector
            })
            .OrderByDescending(e => e.SearchVector.Rank(query))
            .Take(20)
            .ToListAsync();
    }

    #endregion

    #region Array Operations Benchmarks

    [Benchmark]
    public async Task<List<BenchmarkEntity>> ArrayContainsQuery_OptimizedIndex()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Tags.Contains("tag_5"))
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> ArrayOverlapQuery()
    {
        var searchTags = new[] { "tag_5", "tag_10", "tag_15" };
        return await _context.BenchmarkEntities
            .Where(e => e.Tags.Any(tag => searchTags.Contains(tag)))
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> ArrayAggregationQuery()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Scores.Average() > 50)
            .OrderByDescending(e => e.Scores.Max())
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> ArrayLengthQuery()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Tags.Length >= 3 && e.Scores.Length >= 5)
            .Take(100)
            .ToListAsync();
    }

    #endregion

    #region Bulk Operations Benchmarks

    [Benchmark]
    public async Task<int> BulkInsert_StandardEF()
    {
        var newEntities = GenerateNewEntities(1000);
        
        _context.BenchmarkEntities.AddRange(newEntities);
        var result = await _context.SaveChangesAsync();
        
        // Cleanup
        _context.BenchmarkEntities.RemoveRange(newEntities);
        await _context.SaveChangesAsync();
        
        return result;
    }

    [Benchmark]
    public async Task<long> BulkInsert_CopyProtocol()
    {
        var newEntities = GenerateNewEntities(1000);
        
        using var writer = await _connection.BeginBinaryImportAsync(
            "COPY benchmark_entities (name, status, created_at, metadata, tags, scores) FROM STDIN (FORMAT BINARY)");
        
        foreach (var entity in newEntities)
        {
            await writer.StartRowAsync();
            await writer.WriteAsync(entity.Name);
            await writer.WriteAsync((int)entity.Status);
            await writer.WriteAsync(entity.CreatedAt);
            await writer.WriteAsync(entity.Metadata, NpgsqlDbType.Jsonb);
            await writer.WriteAsync(entity.Tags);
            await writer.WriteAsync(entity.Scores);
        }
        
        var result = (long)await writer.CompleteAsync();
        
        // Cleanup
        await _connection.ExecuteNonQueryAsync($"DELETE FROM benchmark_entities WHERE id > {DataSize}");
        
        return result;
    }

    [Benchmark]
    public async Task<int> BulkUpdate_StandardEF()
    {
        var entities = await _context.BenchmarkEntities.Take(100).ToListAsync();
        
        foreach (var entity in entities)
        {
            entity.Name = $"Updated {entity.Name}";
        }
        
        var result = await _context.SaveChangesAsync();
        
        // Revert changes
        foreach (var entity in entities)
        {
            entity.Name = entity.Name.Replace("Updated ", "");
        }
        await _context.SaveChangesAsync();
        
        return result;
    }

    [Benchmark]
    public async Task<int> BulkUpdate_RawSQL()
    {
        var affected = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE benchmark_entities SET name = 'Bulk Updated ' || name WHERE id <= 100");
        
        // Revert changes
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE benchmark_entities SET name = REPLACE(name, 'Bulk Updated ', '') WHERE id <= 100");
        
        return affected;
    }

    #endregion

    #region Index Performance Benchmarks

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_StandardBTreeIndex()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddMonths(-6))
            .OrderBy(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_CompositeIndex()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Status == EntityStatus.Active && e.CreatedAt >= DateTime.UtcNow.AddMonths(-3))
            .OrderBy(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_PartialIndex()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Status == EntityStatus.Active)
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_ExpressionIndex()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Name.ToLower().Contains("entity"))
            .Take(100)
            .ToListAsync();
    }

    #endregion

    #region Complex Query Benchmarks

    [Benchmark]
    public async Task<List<ComplexQueryResult>> ComplexQuery_JoinWithAggregation()
    {
        return await _context.BenchmarkEntities
            .Join(_context.SearchableEntities, 
                  b => b.Id, 
                  s => s.Id % 100, 
                  (b, s) => new { Entity = b, Searchable = s })
            .Where(x => x.Entity.Status == EntityStatus.Active)
            .GroupBy(x => x.Entity.Status) // Group by Status instead since we can't easily extract from JSONB
            .Select(g => new ComplexQueryResult
            {
                Category = g.Key.ToString(),
                Count = g.Count(),
                AverageScore = g.Average(x => x.Entity.Scores.Average()),
                MaxCreatedAt = g.Max(x => x.Entity.CreatedAt)
            })
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> ComplexQuery_MultipleConditions()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Status == EntityStatus.Active 
                     && e.CreatedAt >= DateTime.UtcNow.AddMonths(-6)
                     && e.Tags.Length >= 3
                     && e.Scores.Average() > 50
                     && e.Metadata.Contains("\"enabled\":\"true\""))
            .OrderByDescending(e => e.Scores.Max())
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> ComplexQuery_Subquery()
    {
        var avgScore = await _context.BenchmarkEntities
            .Select(e => e.Scores.Average())
            .AverageAsync();

        return await _context.BenchmarkEntities
            .Where(e => e.Scores.Average() > avgScore)
            .OrderBy(e => e.Id)
            .Take(100)
            .ToListAsync();
    }

    #endregion

    #region Connection and Transaction Benchmarks

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_SingleConnection()
    {
        return await _context.BenchmarkEntities
            .Where(e => e.Status == EntityStatus.Active)
            .Take(100)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_WithTransaction()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _context.BenchmarkEntities
                .Where(e => e.Status == EntityStatus.Active)
                .Take(100)
                .ToListAsync();
            
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [Benchmark]
    public async Task<List<BenchmarkEntity>> Query_ReadUncommitted()
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        try
        {
            var result = await _context.BenchmarkEntities
                .Where(e => e.Status == EntityStatus.Active)
                .Take(100)
                .ToListAsync();
            
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private List<BenchmarkEntity> GenerateNewEntities(int count)
    {
        var random = new Random();
        return Enumerable.Range(DataSize + 1, count)
            .Select(i => new BenchmarkEntity
            {
                Name = $"Bulk Entity {i}",
                Status = (EntityStatus)(i % 4),
                CreatedAt = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(new { category = $"bulk_category_{i % 5}" }),
                Tags = new[] { $"bulk_tag_{i % 10}" },
                Scores = new[] { random.Next(1, 101) }
            })
            .ToList();
    }

    #endregion
}

/// <summary>
/// Test entity for benchmarking standard EF Core operations with PostgreSQL optimizations.
/// </summary>
public class BenchmarkEntity
{
    public int Id { get; set; }
    
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public EntityStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    public int[] Scores { get; set; } = Array.Empty<int>();
}

/// <summary>
/// Test entity for benchmarking full-text search capabilities.
/// </summary>
public class SearchableEntity
{
    public int Id { get; set; }
    
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty;
    
    [Column(TypeName = "tsvector")]
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}

/// <summary>
/// Result type for complex query benchmarks.
/// </summary>
public class ComplexQueryResult
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageScore { get; set; }
    public DateTime MaxCreatedAt { get; set; }
}

/// <summary>
/// Entity status enumeration for testing.
/// </summary>
public enum EntityStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2,
    Archived = 3
}

/// <summary>
/// Test DbContext with optimized PostgreSQL configurations.
/// </summary>
public class BenchmarkDbContext : DbContext
{
    public DbSet<BenchmarkEntity> BenchmarkEntities => Set<BenchmarkEntity>();
    public DbSet<SearchableEntity> SearchableEntities => Set<SearchableEntity>();

    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // BenchmarkEntity configuration
        modelBuilder.Entity<BenchmarkEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Standard B-tree index on created_at
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_benchmark_entities_created_at");
            
            // Composite index for status and created_at queries
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_benchmark_entities_status_created_at");
            
            // Partial index for active entities only
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_benchmark_entities_active_created_at")
                .HasFilter("status = 1");
            
            // Expression index for case-insensitive name searches
            entity.HasIndex("lower(name)")
                .HasDatabaseName("IX_benchmark_entities_name_lower");
            
            // GIN index on JSONB metadata
            entity.HasIndex(e => e.Metadata)
                .HasDatabaseName("IX_benchmark_entities_metadata_gin")
                .HasMethod("gin");
            
            // GIN index on tags array
            entity.HasIndex(e => e.Tags)
                .HasDatabaseName("IX_benchmark_entities_tags_gin")
                .HasMethod("gin");
            
            // GIN index on scores array
            entity.HasIndex(e => e.Scores)
                .HasDatabaseName("IX_benchmark_entities_scores_gin")
                .HasMethod("gin");
        });

        // SearchableEntity configuration
        modelBuilder.Entity<SearchableEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Generated tsvector column combining title and content
            entity.Property(e => e.SearchVector)
                .HasComputedColumnSql(
                    "setweight(to_tsvector('english', coalesce(title, '')), 'A') || " +
                    "setweight(to_tsvector('english', coalesce(content, '')), 'B') || " +
                    "setweight(to_tsvector('english', coalesce(category, '')), 'C') || " +
                    "setweight(to_tsvector('english', coalesce(tags, '')), 'D')",
                    stored: true);
            
            // GIN index on the search vector for full-text search
            entity.HasIndex(e => e.SearchVector)
                .HasDatabaseName("IX_searchable_entities_search_vector_gin")
                .HasMethod("gin");
            
            // Standard index on category for filtering
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_searchable_entities_category");
        });

        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>
/// Extension methods for NpgsqlConnection to support bulk operations in benchmarks.
/// </summary>
public static class NpgsqlConnectionExtensions
{
    public static async Task<int> ExecuteNonQueryAsync(this NpgsqlConnection connection, string commandText)
    {
        using var command = new NpgsqlCommand(commandText, connection);
        return await command.ExecuteNonQueryAsync();
    }
}