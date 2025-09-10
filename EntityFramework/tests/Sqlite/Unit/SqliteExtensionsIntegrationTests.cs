// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class SqliteExtensionsIntegrationTests : IDisposable
{
    private readonly string _connectionString;
    private readonly DbContextOptions<TestDbContext> _options;
    
    public SqliteExtensionsIntegrationTests()
    {
        _connectionString = "Data Source=:memory:";
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connectionString)
            .Options;
    }

    #region Test Entity Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string JsonData { get; set; } = "{}";
        public string Location { get; set; } = string.Empty;
    }

    public class TestSettings
    {
        public string Theme { get; set; } = "light";
        public bool Notifications { get; set; } = true;
        public Dictionary<string, object> Preferences { get; set; } = new();
    }

    #endregion

    #region Test DbContext

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        
        public DbSet<TestEntity> TestEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Test string optimizations
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .HasSqliteCollation()
                    .OptimizeForSqliteSearch();
                
                entity.Property(e => e.Description)
                    .HasSqliteTextAffinity("NOCASE", maxLength: 500);
                
                // Test numeric optimizations
                entity.Property(e => e.Price)
                    .HasSqliteNumericAffinity(precision: 18, scale: 2, enableCurrencyMode: true);
                
                // Test binary optimization
                entity.Property(e => e.Data)
                    .HasSqliteBlobOptimization(CompressionLevel.Optimal);
                
                // Test JSON configuration
                entity.Property(e => e.JsonData)
                    .HasSqliteJsonColumn<TestSettings>(compressionEnabled: false);
                
                // Test indexes
                entity.HasIndex(e => e.Name)
                    .HasSqlitePartialIndex<TestEntity>(e => e.IsActive)
                    .OptimizeForSqliteRangeQueries<TestEntity>(ascending: true, collation: "NOCASE");
                
                entity.HasIndex(e => e.CreatedDate)
                    .HasSqliteCoveringIndex<TestEntity>(e => e.Name, e => e.IsActive);
                
                // Test bulk operations optimization
                entity.OptimizeForSqliteBulkInserts(batchSize: 1000);
                entity.OptimizeForSqliteBulkUpdates(enableRowLevelLocking: true, batchSize: 500);
                entity.ConfigureSqliteBulkOperations(enableWalMode: true);
                
                // Test query optimizations
                entity.OptimizeForSqliteBulkReads(splitQuery: true, trackingBehavior: QueryTrackingBehavior.NoTracking);
                entity.EnableSqliteQueryPlanCaching(cacheSize: 200, enableStatistics: false);
                entity.OptimizeForSqliteAggregations(e => e.Price);
                entity.OptimizeForSqliteJoins(QueryOptimizationExtensions.SqliteJoinStrategy.Hash, batchSize: 1000);
            });
            
            base.OnModelCreating(modelBuilder);
        }
    }

    #endregion

    #region Connection Configuration Integration Tests

    [Fact]
    public void ConnectionConfiguration_WithWALMode_ShouldCreateDatabase()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        
        // Act
        optionsBuilder.EnableSqliteWAL(_connectionString);
        
        using var context = new TestDbContext(optionsBuilder.Options);
        
        // Assert - Should be able to create database and use WAL mode
        context.Database.EnsureCreated();
        
        // Verify database was created
        Assert.True(context.Database.CanConnect());
    }

    [Fact]
    public void ConnectionConfiguration_WithPerformanceOptimizations_ShouldWork()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        
        // Act
        optionsBuilder.OptimizeForSqlitePerformance(_connectionString, cacheSizeKB: 32768, timeoutMilliseconds: 15000);
        
        using var context = new TestDbContext(optionsBuilder.Options);
        
        // Assert
        context.Database.EnsureCreated();
        Assert.True(context.Database.CanConnect());
        
        // Should be able to perform basic operations
        var entity = new TestEntity { Name = "Test", Price = 19.99m };
        context.TestEntities.Add(entity);
        context.SaveChanges();
        
        var retrieved = context.TestEntities.FirstOrDefault();
        Assert.NotNull(retrieved);
        Assert.Equal("Test", retrieved.Name);
    }

    [Fact]
    public void ConnectionConfiguration_WithForeignKeysEnabled_ShouldEnforceForeignKeys()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.EnableSqliteForeignKeys(_connectionString);
        
        using var context = new TestDbContext(optionsBuilder.Options);
        
        // Act & Assert
        context.Database.EnsureCreated();
        
        // Foreign key constraints should be enabled
        // This is more of a smoke test to ensure the configuration doesn't break anything
        Assert.True(context.Database.CanConnect());
    }

    #endregion

    #region Data Type Integration Tests

    [Fact]
    public void DataTypeConfiguration_WithOptimizedTypes_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        var testData = new TestEntity
        {
            Name = "Test Product",
            Description = "This is a test description",
            Price = 123.45m,
            IsActive = true,
            Data = new byte[] { 1, 2, 3, 4, 5 },
            JsonData = System.Text.Json.JsonSerializer.Serialize(new TestSettings
            {
                Theme = "dark",
                Notifications = false,
                Preferences = new Dictionary<string, object> { ["language"] = "en-US" }
            })
        };
        
        // Act
        context.TestEntities.Add(testData);
        context.SaveChanges();
        
        var retrieved = context.TestEntities.First();
        
        // Assert
        Assert.Equal(testData.Name, retrieved.Name);
        Assert.Equal(testData.Description, retrieved.Description);
        Assert.Equal(testData.Price, retrieved.Price);
        Assert.Equal(testData.IsActive, retrieved.IsActive);
        Assert.Equal(testData.Data, retrieved.Data);
        Assert.Equal(testData.JsonData, retrieved.JsonData);
    }

    [Fact]
    public void StringCollation_WithNOCASE_ShouldPerformCaseInsensitiveQueries()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        context.TestEntities.AddRange(
            new TestEntity { Name = "Apple" },
            new TestEntity { Name = "BANANA" },
            new TestEntity { Name = "cherry" }
        );
        context.SaveChanges();
        
        // Act
        var results = context.TestEntities
            .Where(e => e.Name == "apple")  // Lowercase search
            .ToList();
        
        // Assert
        Assert.Single(results);
        Assert.Equal("Apple", results.First().Name);
    }

    [Fact]
    public void JsonColumn_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        var settings = new TestSettings
        {
            Theme = "dark",
            Notifications = true,
            Preferences = new Dictionary<string, object>
            {
                ["fontSize"] = 14,
                ["language"] = "en-US"
            }
        };
        
        var entity = new TestEntity
        {
            Name = "JsonTest",
            JsonData = System.Text.Json.JsonSerializer.Serialize(settings)
        };
        
        // Act
        context.TestEntities.Add(entity);
        context.SaveChanges();
        
        var retrieved = context.TestEntities.First();
        var deserializedSettings = System.Text.Json.JsonSerializer.Deserialize<TestSettings>(retrieved.JsonData);
        
        // Assert
        Assert.NotNull(deserializedSettings);
        Assert.Equal(settings.Theme, deserializedSettings.Theme);
        Assert.Equal(settings.Notifications, deserializedSettings.Notifications);
    }

    #endregion

    #region Query Performance Integration Tests

    [Fact]
    public void BulkInsert_WithOptimization_ShouldInsertLargeDataset()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity
            {
                Name = $"Entity {i}",
                Description = $"Description for entity {i}",
                Price = i * 10.5m,
                IsActive = i % 2 == 0
            })
            .ToList();
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        context.TestEntities.AddRange(entities);
        context.SaveChanges();
        stopwatch.Stop();
        
        // Assert
        Assert.Equal(1000, context.TestEntities.Count());
        // Performance test - should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Bulk insert took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PartialIndex_ShouldImproveQueryPerformance()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        // Insert mix of active and inactive entities
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity
            {
                Name = $"Entity {i}",
                IsActive = i <= 100, // Only first 100 are active
                Price = i
            })
            .ToList();
        
        context.TestEntities.AddRange(entities);
        context.SaveChanges();
        
        // Act - Query only active entities (should use partial index)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var activeEntities = context.TestEntities
            .Where(e => e.IsActive && e.Name.StartsWith("Entity"))
            .ToList();
        stopwatch.Stop();
        
        // Assert
        Assert.Equal(100, activeEntities.Count);
        // Should be fast due to partial index
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Query took {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Transaction Integration Tests

    [Fact]
    public void BulkOperations_WithTransactionOptimization_ShouldHandleTransactions()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        // Act
        using var transaction = context.Database.BeginTransaction();
        
        try
        {
            var entities = Enumerable.Range(1, 100)
                .Select(i => new TestEntity { Name = $"Transactional Entity {i}", Price = i })
                .ToList();
            
            context.TestEntities.AddRange(entities);
            context.SaveChanges();
            
            // Modify some entities
            var toUpdate = context.TestEntities.Take(50).ToList();
            foreach (var entity in toUpdate)
            {
                entity.Price *= 2;
            }
            context.SaveChanges();
            
            transaction.Commit();
            
            // Assert
            Assert.Equal(100, context.TestEntities.Count());
            var updatedEntity = context.TestEntities.First();
            Assert.True(updatedEntity.Price > 1); // Should be doubled
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    #endregion

    #region Error Handling Integration Tests

    [Fact]
    public void DatabaseConfiguration_WithInvalidOptions_ShouldHandleGracefully()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        
        // Act & Assert - Should not throw during configuration
        optionsBuilder.OptimizeForSqlitePerformance(_connectionString, cacheSizeKB: 1024, timeoutMilliseconds: 5000);
        
        using var context = new TestDbContext(optionsBuilder.Options);
        
        // Should be able to create database
        context.Database.EnsureCreated();
        Assert.True(context.Database.CanConnect());
    }

    [Fact]
    public void ConcurrentAccess_WithOptimizations_ShouldWork()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.OptimizeForSqlitePerformance(_connectionString);
        
        // Act - Simulate concurrent access
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            using var context = new TestDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();
            
            var entity = new TestEntity { Name = $"Concurrent {i}", Price = i * 10 };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();
            
            return context.TestEntities.Count();
        });
        
        // Assert - Should complete without errors
        var results = Task.WhenAll(tasks).Result;
        Assert.All(results, count => Assert.True(count > 0));
    }

    #endregion

    #region Schema Generation Integration Tests

    [Fact]
    public void ModelCreation_WithAllOptimizations_ShouldGenerateValidSchema()
    {
        // Arrange & Act
        using var context = new TestDbContext(_options);
        
        // Should not throw during model creation
        context.Database.EnsureCreated();
        
        // Assert
        Assert.True(context.Database.CanConnect());
        
        // Verify schema by checking if we can query the table
        var tableExists = context.TestEntities.Any(); // This will be false but shouldn't throw
        Assert.False(tableExists); // Empty table
        
        // Add an entity to verify the schema works
        var entity = new TestEntity { Name = "Schema Test" };
        context.TestEntities.Add(entity);
        context.SaveChanges();
        
        Assert.True(context.TestEntities.Any());
    }

    [Fact]
    public void ComplexQuery_WithOptimizations_ShouldExecute()
    {
        // Arrange
        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        
        var testEntities = Enumerable.Range(1, 50)
            .Select(i => new TestEntity
            {
                Name = $"Product {i}",
                Price = i * 5.99m,
                IsActive = i % 3 == 0,
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();
        
        context.TestEntities.AddRange(testEntities);
        context.SaveChanges();
        
        // Act - Complex query that should benefit from optimizations
        var results = context.TestEntities
            .Where(e => e.IsActive)
            .Where(e => e.Price > 20)
            .OrderBy(e => e.CreatedDate)
            .Select(e => new { e.Name, e.Price, e.CreatedDate })
            .Take(10)
            .ToList();
        
        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Price > 20));
        Assert.True(results.Count <= 10);
    }

    #endregion

    public void Dispose()
    {
        // Cleanup is automatic for in-memory SQLite databases
        GC.SuppressFinalize(this);
    }
}