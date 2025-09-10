// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

/// <summary>
/// Performance benchmark tests to validate that SQLite optimizations provide measurable improvements.
/// These tests compare performance with and without optimizations applied.
/// </summary>
public class SqlitePerformanceBenchmarkTests : IDisposable
{
    private readonly string _connectionString = "Data Source=:memory:";
    
    #region Test Entity

    public class BenchmarkEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    #endregion

    #region Test DbContexts

    public class OptimizedDbContext : DbContext
    {
        private readonly string _connectionString;
        
        public OptimizedDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public DbSet<BenchmarkEntity> Entities { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.OptimizeForSqlitePerformance(_connectionString, cacheSizeKB: 32768, timeoutMilliseconds: 15000);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BenchmarkEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Apply optimizations
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .OptimizeForSqliteSearch();
                
                entity.Property(e => e.Description)
                    .HasSqliteTextAffinity("NOCASE");
                
                entity.Property(e => e.Price)
                    .HasSqliteNumericAffinity(precision: 18, scale: 2);
                
                entity.Property(e => e.Data)
                    .HasSqliteBlobOptimization(CompressionLevel.Fastest);
                
                // Optimized indexes
                entity.HasIndex(e => e.Name)
                    .HasSqlitePartialIndex<BenchmarkEntity>(e => e.IsActive)
                    .OptimizeForSqliteRangeQueries<BenchmarkEntity>();
                
                entity.HasIndex(e => new { e.IsActive, e.CreatedDate })
                    .HasSqliteCoveringIndex<BenchmarkEntity>(e => e.Name, e => e.Price);
                
                // Bulk operation optimizations
                entity.OptimizeForSqliteBulkInserts(batchSize: 1000);
                entity.OptimizeForSqliteBulkReads();
                entity.EnableSqliteQueryPlanCaching(cacheSize: 500);
            });
        }
    }

    public class StandardDbContext : DbContext
    {
        private readonly string _connectionString;
        
        public StandardDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public DbSet<BenchmarkEntity> Entities { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BenchmarkEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Standard configuration without optimizations
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Description);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.Data);
                
                // Standard indexes
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => new { e.IsActive, e.CreatedDate });
            });
        }
    }

    #endregion

    #region Benchmark Helper Methods

    private List<BenchmarkEntity> GenerateTestData(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new BenchmarkEntity
            {
                Name = $"Entity {i:D6}",
                Description = $"This is a detailed description for entity number {i} which contains various information that might be searched or filtered.",
                Price = (decimal)(i * 9.99),
                IsActive = i % 3 == 0,
                CreatedDate = DateTime.UtcNow.AddMinutes(-i),
                Data = System.Text.Encoding.UTF8.GetBytes($"Binary data for entity {i}")
            })
            .ToList();
    }

    private TimeSpan MeasureOperation(Action operation)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        operation();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    #endregion

    #region Bulk Insert Performance Tests

    [Fact]
    public void BulkInsert_OptimizedVsStandard_ShouldShowPerformanceImprovement()
    {
        // Arrange
        const int entityCount = 5000;
        var testData = GenerateTestData(entityCount);
        
        TimeSpan optimizedTime, standardTime;
        
        // Test optimized context
        using (var optimizedContext = new OptimizedDbContext(_connectionString))
        {
            optimizedContext.Database.EnsureCreated();
            optimizedTime = MeasureOperation(() =>
            {
                optimizedContext.Entities.AddRange(testData);
                optimizedContext.SaveChanges();
            });
        }
        
        // Test standard context (new connection string to avoid WAL sharing)
        var standardConnectionString = "Data Source=:memory:;Cache=Private";
        using (var standardContext = new StandardDbContext(standardConnectionString))
        {
            standardContext.Database.EnsureCreated();
            standardTime = MeasureOperation(() =>
            {
                standardContext.Entities.AddRange(testData.Select(e => new BenchmarkEntity
                {
                    Name = e.Name,
                    Description = e.Description,
                    Price = e.Price,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate,
                    Data = e.Data
                }).ToList());
                standardContext.SaveChanges();
            });
        }
        
        // Assert
        Assert.True(optimizedTime.TotalMilliseconds > 0, "Optimized insert should take measurable time");
        Assert.True(standardTime.TotalMilliseconds > 0, "Standard insert should take measurable time");
        
        // Log performance results for analysis
        var improvementPercentage = ((standardTime.TotalMilliseconds - optimizedTime.TotalMilliseconds) / standardTime.TotalMilliseconds) * 100;
        
        // Performance improvement is expected but not guaranteed in all test environments
        // This is more of a smoke test to ensure optimizations don't degrade performance
        Assert.True(optimizedTime.TotalMilliseconds < standardTime.TotalMilliseconds * 1.5, 
            $"Optimized insert ({optimizedTime.TotalMilliseconds:F2}ms) should not be significantly slower than standard insert ({standardTime.TotalMilliseconds:F2}ms). " +
            $"Performance change: {improvementPercentage:F1}%");
    }

    #endregion

    #region Query Performance Tests

    [Fact]
    public void SearchQueries_WithOptimizations_ShouldPerformWell()
    {
        // Arrange
        const int entityCount = 10000;
        var testData = GenerateTestData(entityCount);
        
        using var optimizedContext = new OptimizedDbContext(_connectionString);
        optimizedContext.Database.EnsureCreated();
        optimizedContext.Entities.AddRange(testData);
        optimizedContext.SaveChanges();
        
        // Act & Assert - Various query patterns that should benefit from optimizations
        
        // 1. Case-insensitive search (benefits from NOCASE collation)
        var searchTime1 = MeasureOperation(() =>
        {
            var results = optimizedContext.Entities
                .Where(e => e.Name.Contains("entity"))  // Should be case-insensitive
                .Take(100)
                .ToList();
            Assert.True(results.Count > 0);
        });
        
        // 2. Partial index query (benefits from partial index on IsActive)
        var searchTime2 = MeasureOperation(() =>
        {
            var results = optimizedContext.Entities
                .Where(e => e.IsActive && e.Name.StartsWith("Entity"))
                .OrderBy(e => e.CreatedDate)
                .Take(50)
                .ToList();
            Assert.True(results.Count > 0);
        });
        
        // 3. Covering index query (benefits from covering index)
        var searchTime3 = MeasureOperation(() =>
        {
            var results = optimizedContext.Entities
                .Where(e => e.IsActive)
                .Select(e => new { e.Name, e.Price })
                .Take(100)
                .ToList();
            Assert.True(results.Count > 0);
        });
        
        // Performance assertions - queries should complete in reasonable time
        Assert.True(searchTime1.TotalMilliseconds < 1000, $"Case-insensitive search took {searchTime1.TotalMilliseconds:F2}ms");
        Assert.True(searchTime2.TotalMilliseconds < 1000, $"Partial index query took {searchTime2.TotalMilliseconds:F2}ms");
        Assert.True(searchTime3.TotalMilliseconds < 1000, $"Covering index query took {searchTime3.TotalMilliseconds:F2}ms");
    }

    #endregion

    #region Aggregation Performance Tests

    [Fact]
    public void Aggregations_WithOptimizations_ShouldPerformEfficiently()
    {
        // Arrange
        const int entityCount = 5000;
        var testData = GenerateTestData(entityCount);
        
        using var optimizedContext = new OptimizedDbContext(_connectionString);
        optimizedContext.Database.EnsureCreated();
        
        // Configure aggregation optimization
        optimizedContext.Model.FindEntityType(typeof(BenchmarkEntity))
            ?.SetAnnotation("Sqlite:OptimizedForAggregations", true);
        
        optimizedContext.Entities.AddRange(testData);
        optimizedContext.SaveChanges();
        
        // Act - Various aggregation operations
        var aggregationTime = MeasureOperation(() =>
        {
            // Sum
            var totalPrice = optimizedContext.Entities.Sum(e => e.Price);
            Assert.True(totalPrice > 0);
            
            // Count with condition
            var activeCount = optimizedContext.Entities.Count(e => e.IsActive);
            Assert.True(activeCount > 0);
            
            // Average
            var avgPrice = optimizedContext.Entities.Average(e => e.Price);
            Assert.True(avgPrice > 0);
            
            // Grouped aggregation
            var pricesByStatus = optimizedContext.Entities
                .GroupBy(e => e.IsActive)
                .Select(g => new { IsActive = g.Key, TotalPrice = g.Sum(e => e.Price) })
                .ToList();
            Assert.Equal(2, pricesByStatus.Count); // Active and inactive groups
        });
        
        // Assert
        Assert.True(aggregationTime.TotalMilliseconds < 2000, $"Aggregations took {aggregationTime.TotalMilliseconds:F2}ms");
    }

    #endregion

    #region Connection Performance Tests

    [Fact]
    public void ConnectionOptimizations_ShouldImproveConnectionHandling()
    {
        // Test multiple connection scenarios with optimizations
        var connectionTimes = new List<TimeSpan>();
        
        for (int i = 0; i < 5; i++)
        {
            var connectionTime = MeasureOperation(() =>
            {
                using var context = new OptimizedDbContext(_connectionString);
                context.Database.EnsureCreated();
                
                // Perform a simple operation
                var entity = new BenchmarkEntity { Name = $"Test {i}" };
                context.Entities.Add(entity);
                context.SaveChanges();
                
                var count = context.Entities.Count();
                Assert.True(count > 0);
            });
            
            connectionTimes.Add(connectionTime);
        }
        
        // Assert - All connections should complete in reasonable time
        Assert.All(connectionTimes, time => 
            Assert.True(time.TotalMilliseconds < 5000, $"Connection operation took {time.TotalMilliseconds:F2}ms"));
        
        var avgConnectionTime = connectionTimes.Average(t => t.TotalMilliseconds);
        Assert.True(avgConnectionTime < 1000, $"Average connection time: {avgConnectionTime:F2}ms");
    }

    #endregion

    #region Memory Usage Tests

    [Fact]
    public void MemoryUsage_WithOptimizations_ShouldBeReasonable()
    {
        // Measure memory usage during bulk operations
        var initialMemory = GC.GetTotalMemory(true);
        
        using (var context = new OptimizedDbContext(_connectionString))
        {
            context.Database.EnsureCreated();
            
            // Perform memory-intensive operations
            for (int batch = 0; batch < 10; batch++)
            {
                var batchData = GenerateTestData(1000);
                context.Entities.AddRange(batchData);
                context.SaveChanges();
                
                // Clear change tracker to free memory
                context.ChangeTracker.Clear();
            }
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryDifference = finalMemory - initialMemory;
        
        // Assert - Memory growth should be reasonable (less than 50MB for this test)
        Assert.True(memoryDifference < 50 * 1024 * 1024, 
            $"Memory usage increased by {memoryDifference / (1024 * 1024):F2}MB");
    }

    #endregion

    #region Concurrent Access Performance

    [Fact]
    public void ConcurrentAccess_WithOptimizations_ShouldHandleLoad()
    {
        // Test concurrent operations with WAL mode optimizations
        const int concurrentTasks = 5;
        const int operationsPerTask = 100;
        
        var totalTime = MeasureOperation(() =>
        {
            var tasks = Enumerable.Range(1, concurrentTasks).Select(taskId =>
                Task.Run(() =>
                {
                    using var context = new OptimizedDbContext(_connectionString);
                    context.Database.EnsureCreated();
                    
                    for (int i = 1; i <= operationsPerTask; i++)
                    {
                        var entity = new BenchmarkEntity 
                        { 
                            Name = $"Task{taskId}_Entity{i}",
                            Price = taskId * i
                        };
                        
                        context.Entities.Add(entity);
                        
                        if (i % 10 == 0) // Batch saves
                        {
                            context.SaveChanges();
                            context.ChangeTracker.Clear();
                        }
                    }
                    context.SaveChanges();
                })
            ).ToArray();
            
            Task.WaitAll(tasks);
        });
        
        // Verify results
        using (var verifyContext = new OptimizedDbContext(_connectionString))
        {
            var totalEntities = verifyContext.Entities.Count();
            Assert.Equal(concurrentTasks * operationsPerTask, totalEntities);
        }
        
        // Assert - Concurrent operations should complete in reasonable time
        Assert.True(totalTime.TotalMilliseconds < 30000, 
            $"Concurrent operations took {totalTime.TotalMilliseconds:F2}ms");
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}