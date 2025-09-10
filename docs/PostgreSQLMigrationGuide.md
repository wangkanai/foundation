# PostgreSQL Migration Guide for Entity Framework Core

This comprehensive guide provides step-by-step instructions for migrating existing Entity Framework Core applications to leverage PostgreSQL-specific optimizations, including migration from other database providers and upgrading existing PostgreSQL implementations.

## Table of Contents

1. [Migration Overview](#migration-overview)
2. [Pre-Migration Assessment](#pre-migration-assessment)
3. [Migration from SQL Server](#migration-from-sql-server)
4. [Migration from MySQL](#migration-from-mysql)
5. [Migration from SQLite](#migration-from-sqlite)
6. [Upgrading Existing PostgreSQL](#upgrading-existing-postgresql)
7. [Data Migration Strategies](#data-migration-strategies)
8. [Testing and Validation](#testing-and-validation)
9. [Performance Optimization](#performance-optimization)
10. [Production Deployment](#production-deployment)

## Migration Overview

PostgreSQL offers unique features that can significantly improve application performance and functionality. This guide covers migration strategies for different scenarios:

- **Cross-platform migration**: Moving from SQL Server, MySQL, or SQLite
- **PostgreSQL optimization**: Enhancing existing PostgreSQL implementations
- **Data transformation**: Converting existing data to PostgreSQL-optimized formats
- **Performance tuning**: Implementing PostgreSQL-specific optimizations

### Key Migration Benefits

| Feature | Before | After PostgreSQL Optimization |
|---------|--------|-------------------------------|
| Semi-structured data | Multiple tables/columns | JSONB with indexing |
| Text search | LIKE patterns | Full-text search with ranking |
| Collections | Normalized tables | Native array types |
| Bulk operations | Row-by-row inserts | COPY protocol |
| Advanced indexing | Basic B-tree only | GIN, GiST, partial, expression indexes |

## Pre-Migration Assessment

Before starting migration, assess your current application to identify optimization opportunities and potential challenges.

### Current Architecture Analysis

```csharp
// Assessment tool for existing Entity Framework models
public class MigrationAssessment
{
    public static void AnalyzeDbContext(DbContext context)
    {
        var model = context.Model;
        var report = new AssessmentReport();
        
        foreach (var entityType in model.GetEntityTypes())
        {
            AnalyzeEntity(entityType, report);
        }
        
        GenerateReport(report);
    }
    
    private static void AnalyzeEntity(IEntityType entityType, AssessmentReport report)
    {
        // Identify JSONB candidates
        var jsonCandidates = entityType.GetProperties()
            .Where(p => p.ClrType == typeof(string) && 
                       (p.Name.Contains("Json") || p.Name.Contains("Metadata") || 
                        p.Name.Contains("Settings") || p.Name.Contains("Config")))
            .ToList();
        
        if (jsonCandidates.Any())
        {
            report.JsonbCandidates.Add(new JsonbCandidate
            {
                EntityType = entityType.Name,
                Properties = jsonCandidates.Select(p => p.Name).ToList()
            });
        }
        
        // Identify full-text search candidates
        var textSearchCandidates = entityType.GetProperties()
            .Where(p => p.ClrType == typeof(string) && 
                       (p.Name.Contains("Content") || p.Name.Contains("Description") ||
                        p.Name.Contains("Title") || p.Name.Contains("Body")))
            .ToList();
        
        if (textSearchCandidates.Any())
        {
            report.FullTextSearchCandidates.Add(new FullTextSearchCandidate
            {
                EntityType = entityType.Name,
                Properties = textSearchCandidates.Select(p => p.Name).ToList()
            });
        }
        
        // Identify array candidates (comma-separated values, etc.)
        var arrayCandidates = entityType.GetProperties()
            .Where(p => p.ClrType == typeof(string) && 
                       (p.Name.Contains("Tags") || p.Name.Contains("Categories") ||
                        p.Name.Contains("List") || p.Name.EndsWith("s")))
            .ToList();
        
        if (arrayCandidates.Any())
        {
            report.ArrayCandidates.Add(new ArrayCandidate
            {
                EntityType = entityType.Name,
                Properties = arrayCandidates.Select(p => p.Name).ToList()
            });
        }
    }
}

public class AssessmentReport
{
    public List<JsonbCandidate> JsonbCandidates { get; set; } = new();
    public List<FullTextSearchCandidate> FullTextSearchCandidates { get; set; } = new();
    public List<ArrayCandidate> ArrayCandidates { get; set; } = new();
    public List<string> RecommendedIndexes { get; set; } = new();
    public Dictionary<string, string> PerformanceImprovements { get; set; } = new();
}
```

### Performance Baseline

Establish performance baselines before migration to measure improvements:

```csharp
public class PerformanceBaseline
{
    public async Task<BaselineReport> GenerateBaselineAsync(DbContext context)
    {
        var report = new BaselineReport();
        var stopwatch = Stopwatch.StartNew();
        
        // Common query patterns
        await MeasureQueryAsync("Simple Select", () => 
            context.Set<Product>().Take(100).ToListAsync(), report);
        
        await MeasureQueryAsync("Filtered Query", () =>
            context.Set<Product>().Where(p => p.Category == "Electronics").ToListAsync(), report);
        
        await MeasureQueryAsync("Text Search", () =>
            context.Set<Product>().Where(p => p.Description.Contains("gaming")).ToListAsync(), report);
        
        await MeasureQueryAsync("Join Query", () =>
            context.Set<Order>()
                .Include(o => o.OrderItems)
                .Where(o => o.CreatedAt >= DateTime.Today.AddDays(-30))
                .ToListAsync(), report);
        
        return report;
    }
    
    private async Task MeasureQueryAsync<T>(string queryName, Func<Task<T>> query, BaselineReport report)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await query();
        stopwatch.Stop();
        
        report.QueryPerformance[queryName] = stopwatch.ElapsedMilliseconds;
    }
}

public class BaselineReport
{
    public Dictionary<string, long> QueryPerformance { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
```

## Migration from SQL Server

### 1. Package Updates

```xml
<!-- Remove SQL Server packages -->
<!-- <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" /> -->

<!-- Add PostgreSQL packages -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Wangkanai.EntityFramework.Postgres" Version="1.0.0" />
```

### 2. Connection String Migration

```csharp
// From SQL Server
// "Server=localhost;Database=MyApp;Trusted_Connection=true;"

// To PostgreSQL
"Host=localhost;Database=myapp;Username=myuser;Password=mypassword;Include Error Detail=true;"
```

### 3. DbContext Configuration

```csharp
// Before (SQL Server)
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer(connectionString);
}

// After (PostgreSQL)
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseNpgsql(connectionString, options =>
        {
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            options.EnableRetryOnFailure(maxRetryCount: 3);
        })
        .UseSnakeCaseNamingConvention(); // Optional: PostgreSQL naming conventions
}
```

### 4. Data Type Mapping

```csharp
public class SqlServerToPostgreSqlMigration
{
    public static void ConfigureModelConversion(ModelBuilder modelBuilder)
    {
        // NVARCHAR(MAX) -> TEXT
        modelBuilder.Entity<Article>()
            .Property(e => e.Content)
            .HasColumnType("text"); // Instead of nvarchar(max)
        
        // UNIQUEIDENTIFIER -> UUID
        modelBuilder.Entity<User>()
            .Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");
        
        // DATETIME2 -> TIMESTAMPTZ
        modelBuilder.Entity<Order>()
            .Property(e => e.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");
        
        // XML data to JSONB
        modelBuilder.Entity<Product>()
            .Property(e => e.Specifications)
            .HasColumnType("jsonb");
        
        // Comma-separated values to arrays
        modelBuilder.Entity<Product>()
            .Property(e => e.Tags)
            .HasConversion(
                v => v.ToArray(),
                v => v.ToList());
    }
}
```

### 5. Index Migration

```csharp
// SQL Server indexes to PostgreSQL equivalents
public static void MigrateIndexes(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        // Standard B-tree indexes remain the same
        entity.HasIndex(e => e.CategoryId);
        
        // Full-text indexes -> PostgreSQL full-text search
        entity.Property(e => e.SearchVector)
            .HasComputedColumnSql(
                "to_tsvector('english', coalesce(name, '') || ' ' || coalesce(description, ''))",
                stored: true);
        
        entity.HasIndex(e => e.SearchVector)
            .HasMethod("gin");
        
        // JSON data -> JSONB with GIN index
        entity.HasIndex(e => e.Specifications)
            .HasMethod("gin");
        
        // Filtered indexes -> Partial indexes
        entity.HasIndex(e => e.CreatedAt)
            .HasFilter("is_active = true");
    });
}
```

## Migration from MySQL

### 1. Package and Configuration Updates

```csharp
// Before (MySQL)
optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

// After (PostgreSQL)
optionsBuilder.UseNpgsql(connectionString, options =>
{
    options.EnableRetryOnFailure();
    options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
});
```

### 2. Character Set and Collation

```csharp
public class MySqlToPostgreSqlMigration
{
    public static void ConfigureTextHandling(ModelBuilder modelBuilder)
    {
        // MySQL utf8mb4 -> PostgreSQL UTF8 (default)
        modelBuilder.Entity<Article>()
            .Property(e => e.Title)
            .HasColumnType("text"); // PostgreSQL handles UTF8 by default
        
        // Case sensitivity handling
        modelBuilder.Entity<User>()
            .HasIndex("lower(email)")
            .HasDatabaseName("IX_users_email_lower")
            .IsUnique(); // Case-insensitive unique constraint
    }
}
```

### 3. Auto-increment to Serial/Identity

```csharp
// MySQL AUTO_INCREMENT -> PostgreSQL SERIAL/IDENTITY
modelBuilder.Entity<Product>()
    .Property(e => e.Id)
    .UseIdentityByDefaultColumn(); // PostgreSQL IDENTITY

// Alternative using SERIAL
modelBuilder.Entity<Category>()
    .Property(e => e.Id)
    .HasColumnType("serial");
```

### 4. JSON to JSONB Migration

```csharp
public async Task MigrateJsonToJsonb(DbContext context)
{
    // Step 1: Add new JSONB column
    await context.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE products 
        ADD COLUMN specifications_jsonb JSONB;
    ");
    
    // Step 2: Convert existing JSON data to JSONB
    await context.Database.ExecuteSqlRawAsync(@"
        UPDATE products 
        SET specifications_jsonb = specifications::jsonb 
        WHERE specifications IS NOT NULL;
    ");
    
    // Step 3: Create GIN index on JSONB column
    await context.Database.ExecuteSqlRawAsync(@"
        CREATE INDEX CONCURRENTLY IX_products_specifications_jsonb_gin 
        ON products USING gin (specifications_jsonb);
    ");
    
    // Step 4: Update application to use new column
    // Step 5: Drop old column (in separate migration)
}
```

## Migration from SQLite

### 1. Schema Differences

```csharp
public class SqliteToPostgreSqlMigration
{
    public static void HandleSchemaDifferences(ModelBuilder modelBuilder)
    {
        // SQLite dynamic typing -> PostgreSQL strong typing
        modelBuilder.Entity<FlexibleData>()
            .Property(e => e.Value)
            .HasColumnType("jsonb"); // Store dynamic data as JSONB
        
        // SQLite date strings -> PostgreSQL timestamps
        modelBuilder.Entity<Event>()
            .Property(e => e.OccurredAt)
            .HasColumnType("timestamptz")
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        
        // Case sensitivity
        modelBuilder.Entity<User>()
            .HasIndex("lower(username)")
            .HasDatabaseName("IX_users_username_lower")
            .IsUnique();
    }
}
```

### 2. Data Migration Strategy

```csharp
public class SqliteDataMigrator
{
    public async Task MigrateDataAsync(string sqliteConnectionString, string postgresConnectionString)
    {
        using var sourceContext = new SqliteDbContext(sqliteConnectionString);
        using var targetContext = new PostgreSqlDbContext(postgresConnectionString);
        
        // Batch size for memory management
        const int batchSize = 1000;
        
        // Migrate users
        await MigrateTable(
            sourceContext.Users,
            targetContext.Users,
            batchSize,
            user => TransformUser(user)
        );
        
        // Migrate products with JSON transformation
        await MigrateTable(
            sourceContext.Products,
            targetContext.Products,
            batchSize,
            product => TransformProduct(product)
        );
    }
    
    private async Task MigrateTable<TSource, TTarget>(
        DbSet<TSource> source,
        DbSet<TTarget> target,
        int batchSize,
        Func<TSource, TTarget> transform)
        where TSource : class
        where TTarget : class
    {
        var totalCount = await source.CountAsync();
        var processed = 0;
        
        while (processed < totalCount)
        {
            var batch = await source
                .Skip(processed)
                .Take(batchSize)
                .ToListAsync();
            
            var transformedBatch = batch.Select(transform).ToList();
            
            target.AddRange(transformedBatch);
            await target.SaveChangesAsync();
            
            processed += batch.Count;
            Console.WriteLine($"Migrated {processed}/{totalCount} records");
        }
    }
    
    private PostgreSqlProduct TransformProduct(SqliteProduct source)
    {
        return new PostgreSqlProduct
        {
            Id = source.Id,
            Name = source.Name,
            Category = source.Category,
            Price = source.Price,
            
            // Transform comma-separated tags to array
            Tags = string.IsNullOrEmpty(source.TagsString) 
                ? Array.Empty<string>() 
                : source.TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries),
            
            // Transform loose properties to JSONB
            Specifications = JsonSerializer.Serialize(new
            {
                weight = source.Weight,
                dimensions = source.Dimensions,
                color = source.Color,
                customAttributes = ParseCustomAttributes(source.CustomAttributesString)
            }),
            
            CreatedAt = DateTime.SpecifyKind(source.CreatedAt, DateTimeKind.Utc)
        };
    }
}
```

## Upgrading Existing PostgreSQL

If you're already using PostgreSQL but want to leverage optimization features:

### 1. Identify Optimization Opportunities

```csharp
public class PostgreSqlOptimizationAnalyzer
{
    public async Task<OptimizationReport> AnalyzeAsync(NpgsqlConnection connection)
    {
        var report = new OptimizationReport();
        
        // Find tables with JSON columns that aren't JSONB
        var jsonColumns = await connection.QueryAsync<TableColumn>(@"
            SELECT table_name, column_name, data_type
            FROM information_schema.columns
            WHERE data_type = 'json'
        ");
        
        report.JsonToJsonbCandidates.AddRange(jsonColumns.Select(c => 
            $"{c.TableName}.{c.ColumnName}"));
        
        // Find text columns suitable for full-text search
        var textColumns = await connection.QueryAsync<TableColumn>(@"
            SELECT t.table_name, c.column_name, pg_column_size(c.column_name) as avg_size
            FROM information_schema.tables t
            JOIN information_schema.columns c ON t.table_name = c.table_name
            WHERE c.data_type IN ('text', 'varchar')
            AND c.column_name ILIKE ANY(ARRAY['%content%', '%description%', '%body%', '%text%'])
        ");
        
        report.FullTextSearchCandidates.AddRange(textColumns.Select(c =>
            $"{c.TableName}.{c.ColumnName}"));
        
        // Find missing indexes on commonly queried columns
        var missingIndexes = await FindMissingIndexesAsync(connection);
        report.MissingIndexes.AddRange(missingIndexes);
        
        return report;
    }
}
```

### 2. Gradual Migration Approach

```csharp
public class GradualMigrationStrategy
{
    // Phase 1: Add optimized columns alongside existing ones
    public async Task Phase1_AddOptimizedColumns(DbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
            -- Add JSONB column alongside existing JSON
            ALTER TABLE products 
            ADD COLUMN specifications_v2 JSONB;
            
            -- Add tsvector for full-text search
            ALTER TABLE articles 
            ADD COLUMN search_vector TSVECTOR;
            
            -- Add array column for tags
            ALTER TABLE products 
            ADD COLUMN tags_array TEXT[];
        ");
        
        await CreateOptimizedIndexes(context);
    }
    
    // Phase 2: Populate new columns with transformed data
    public async Task Phase2_PopulateOptimizedColumns(DbContext context)
    {
        // Convert JSON to JSONB
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE products 
            SET specifications_v2 = specifications::jsonb 
            WHERE specifications IS NOT NULL 
            AND specifications_v2 IS NULL;
        ");
        
        // Generate tsvectors
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE articles 
            SET search_vector = setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                               setweight(to_tsvector('english', coalesce(content, '')), 'D')
            WHERE search_vector IS NULL;
        ");
        
        // Convert comma-separated tags to arrays
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE products 
            SET tags_array = string_to_array(tags, ',')
            WHERE tags IS NOT NULL 
            AND tags_array IS NULL;
        ");
    }
    
    // Phase 3: Update application code to use optimized columns
    public void Phase3_UpdateApplicationCode()
    {
        // This phase involves updating Entity Framework models
        // and query code to use the new optimized columns
    }
    
    // Phase 4: Remove old columns (after verification)
    public async Task Phase4_RemoveOldColumns(DbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE products DROP COLUMN specifications;
            ALTER TABLE products DROP COLUMN tags;
            ALTER TABLE products RENAME COLUMN specifications_v2 TO specifications;
            ALTER TABLE products RENAME COLUMN tags_array TO tags;
        ");
    }
}
```

### 3. Index Optimization

```csharp
public class IndexOptimizer
{
    public async Task OptimizeIndexesAsync(NpgsqlConnection connection)
    {
        // Create GIN indexes for JSONB columns
        await connection.ExecuteAsync(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_products_specifications_gin 
            ON products USING gin (specifications);
        ");
        
        // Create partial indexes for common filters
        await connection.ExecuteAsync(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_products_active_created_at 
            ON products (created_at) 
            WHERE is_active = true;
        ");
        
        // Create expression indexes for computed values
        await connection.ExecuteAsync(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_email_lower 
            ON users (lower(email));
        ");
        
        // Create composite indexes for common query patterns
        await connection.ExecuteAsync(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_orders_customer_status_date 
            ON orders (customer_id, status, created_at);
        ");
    }
}
```

## Data Migration Strategies

### 1. Zero-Downtime Migration

```csharp
public class ZeroDowntimeMigrator
{
    public async Task ExecuteZeroDowntimeMigration()
    {
        // Step 1: Setup read replica with PostgreSQL
        var replicaConnection = await SetupPostgreSqlReplicaAsync();
        
        // Step 2: Implement dual-write pattern
        await EnableDualWriteAsync();
        
        // Step 3: Migrate historical data in batches
        await MigrateHistoricalDataAsync();
        
        // Step 4: Verify data consistency
        await VerifyDataConsistencyAsync();
        
        // Step 5: Switch reads to PostgreSQL
        await SwitchReadsToPostgreSqlAsync();
        
        // Step 6: Switch writes to PostgreSQL
        await SwitchWritesToPostgreSqlAsync();
        
        // Step 7: Cleanup old database
        await CleanupOldDatabaseAsync();
    }
    
    private async Task EnableDualWriteAsync()
    {
        // Implement application logic to write to both databases
        // Use message queues or event sourcing for reliability
    }
}
```

### 2. Batch Migration with Checkpointing

```csharp
public class CheckpointedMigrator
{
    private readonly ILogger<CheckpointedMigrator> _logger;
    private readonly string _checkpointFile;
    
    public async Task MigrateWithCheckpoints<T>(
        IQueryable<T> source,
        Func<T, Task> processor,
        int batchSize = 1000) where T : class, IIdentifiable
    {
        var checkpoint = await LoadCheckpointAsync();
        var lastProcessedId = checkpoint?.LastProcessedId ?? 0;
        
        while (true)
        {
            var batch = await source
                .Where(x => x.Id > lastProcessedId)
                .OrderBy(x => x.Id)
                .Take(batchSize)
                .ToListAsync();
            
            if (!batch.Any()) break;
            
            foreach (var item in batch)
            {
                try
                {
                    await processor(item);
                    lastProcessedId = item.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process item {Id}", item.Id);
                    await SaveCheckpointAsync(new MigrationCheckpoint 
                    { 
                        LastProcessedId = lastProcessedId,
                        LastError = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                    throw;
                }
            }
            
            await SaveCheckpointAsync(new MigrationCheckpoint 
            { 
                LastProcessedId = lastProcessedId,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Processed up to ID {LastId}", lastProcessedId);
        }
    }
}
```

## Testing and Validation

### 1. Data Integrity Validation

```csharp
public class MigrationValidator
{
    public async Task<ValidationReport> ValidateDataIntegrityAsync(
        DbContext sourceContext, 
        DbContext targetContext)
    {
        var report = new ValidationReport();
        
        // Validate record counts
        await ValidateRecordCounts(sourceContext, targetContext, report);
        
        // Validate data consistency with sampling
        await ValidateDataConsistency(sourceContext, targetContext, report);
        
        // Validate relationships
        await ValidateRelationships(sourceContext, targetContext, report);
        
        // Validate transformed data
        await ValidateTransformations(sourceContext, targetContext, report);
        
        return report;
    }
    
    private async Task ValidateRecordCounts(
        DbContext source, 
        DbContext target, 
        ValidationReport report)
    {
        var entityTypes = source.Model.GetEntityTypes();
        
        foreach (var entityType in entityTypes)
        {
            var sourceCount = await GetRecordCountAsync(source, entityType);
            var targetCount = await GetRecordCountAsync(target, entityType);
            
            if (sourceCount != targetCount)
            {
                report.Errors.Add($"Record count mismatch for {entityType.Name}: " +
                                 $"Source={sourceCount}, Target={targetCount}");
            }
        }
    }
    
    private async Task ValidateDataConsistency(
        DbContext source, 
        DbContext target, 
        ValidationReport report)
    {
        // Sample-based validation for large datasets
        var sampleSize = 1000;
        var random = new Random(42); // Fixed seed for reproducibility
        
        var sourceProducts = await source.Set<Product>()
            .OrderBy(p => p.Id)
            .Skip(random.Next(0, 10000))
            .Take(sampleSize)
            .ToListAsync();
        
        foreach (var sourceProduct in sourceProducts)
        {
            var targetProduct = await target.Set<Product>()
                .FirstOrDefaultAsync(p => p.Id == sourceProduct.Id);
            
            if (targetProduct == null)
            {
                report.Errors.Add($"Missing product {sourceProduct.Id} in target");
                continue;
            }
            
            ValidateProductEquality(sourceProduct, targetProduct, report);
        }
    }
}
```

### 2. Performance Validation

```csharp
public class PerformanceValidator
{
    public async Task<PerformanceComparisonReport> ComparePerformanceAsync(
        DbContext oldContext, 
        DbContext newContext)
    {
        var report = new PerformanceComparisonReport();
        
        // Common query patterns
        var queries = new Dictionary<string, Func<DbContext, Task<object>>>
        {
            ["Simple Select"] = ctx => ctx.Set<Product>().Take(100).ToListAsync(),
            ["Filtered Query"] = ctx => ctx.Set<Product>().Where(p => p.IsActive).ToListAsync(),
            ["Text Search"] = ctx => ctx.Set<Product>().Where(p => p.Description.Contains("gaming")).ToListAsync(),
            ["Complex Join"] = ctx => ctx.Set<Order>().Include(o => o.OrderItems).Take(50).ToListAsync()
        };
        
        foreach (var (queryName, queryFunc) in queries)
        {
            var oldTime = await MeasureQueryTimeAsync(oldContext, queryFunc);
            var newTime = await MeasureQueryTimeAsync(newContext, queryFunc);
            
            report.QueryComparisons[queryName] = new PerformanceComparison
            {
                OldTime = oldTime,
                NewTime = newTime,
                Improvement = (double)oldTime / newTime
            };
        }
        
        return report;
    }
    
    private async Task<TimeSpan> MeasureQueryTimeAsync(
        DbContext context, 
        Func<DbContext, Task<object>> query)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Warm-up run
        await query(context);
        
        // Actual measurement
        stopwatch.Restart();
        await query(context);
        stopwatch.Stop();
        
        return stopwatch.Elapsed;
    }
}
```

## Performance Optimization

### 1. Post-Migration Optimization

```csharp
public class PostMigrationOptimizer
{
    public async Task OptimizeAsync(NpgsqlConnection connection)
    {
        // Update table statistics
        await connection.ExecuteAsync("ANALYZE;");
        
        // Rebuild indexes for optimal performance
        await connection.ExecuteAsync(@"
            REINDEX TABLE products;
            REINDEX TABLE orders;
            REINDEX TABLE users;
        ");
        
        // Optimize PostgreSQL configuration
        await OptimizePostgreSqlConfiguration(connection);
        
        // Create additional indexes based on query patterns
        await CreateOptimizedIndexes(connection);
    }
    
    private async Task OptimizePostgreSqlConfiguration(NpgsqlConnection connection)
    {
        // These should be done at the PostgreSQL server level
        var optimizations = new[]
        {
            "ALTER SYSTEM SET shared_buffers = '25% of RAM';",
            "ALTER SYSTEM SET effective_cache_size = '75% of RAM';",
            "ALTER SYSTEM SET maintenance_work_mem = '2GB';",
            "ALTER SYSTEM SET checkpoint_completion_target = 0.9;",
            "ALTER SYSTEM SET wal_buffers = '16MB';",
            "ALTER SYSTEM SET default_statistics_target = 100;",
            "SELECT pg_reload_conf();"
        };
        
        foreach (var sql in optimizations)
        {
            try
            {
                await connection.ExecuteAsync(sql);
            }
            catch (Exception ex)
            {
                // Log but don't fail migration
                Console.WriteLine($"Configuration optimization failed: {ex.Message}");
            }
        }
    }
}
```

### 2. Query Optimization

```csharp
public class QueryOptimizer
{
    public static void ConfigureOptimizedQueries(ModelBuilder modelBuilder)
    {
        // Configure query filters for soft deletes
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => !p.IsDeleted);
        
        // Configure default ordering
        modelBuilder.Entity<Article>()
            .HasIndex(a => a.PublishedAt)
            .HasDatabaseName("IX_articles_published_at_desc")
            .IsDescending();
        
        // Configure query splitting for complex includes
        modelBuilder.Entity<Order>()
            .Navigation(o => o.OrderItems)
            .EnableLazyLoading(false); // Explicit loading for better performance
    }
    
    public static void OptimizeDbContextConfiguration(DbContextOptionsBuilder options)
    {
        options
            .UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                npgsqlOptions.CommandTimeout(30);
            })
            .EnableServiceProviderCaching()
            .EnableSensitiveDataLogging(false) // Disable in production
            .ConfigureWarnings(warnings =>
                warnings.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning));
    }
}
```

## Production Deployment

### 1. Deployment Checklist

```csharp
public class DeploymentChecklist
{
    public async Task<DeploymentReadinessReport> AssessReadinessAsync(
        string connectionString)
    {
        var report = new DeploymentReadinessReport();
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Check PostgreSQL version
        var version = await connection.QuerySingleAsync<string>("SELECT version();");
        report.PostgreSqlVersion = version;
        
        // Check required extensions
        var extensions = await CheckRequiredExtensionsAsync(connection);
        report.RequiredExtensions = extensions;
        
        // Check connection pool configuration
        var poolInfo = await GetConnectionPoolInfoAsync(connection);
        report.ConnectionPoolInfo = poolInfo;
        
        // Validate indexes
        var indexStatus = await ValidateIndexesAsync(connection);
        report.IndexValidation = indexStatus;
        
        // Check table statistics
        var statsInfo = await CheckTableStatisticsAsync(connection);
        report.StatisticsInfo = statsInfo;
        
        return report;
    }
    
    private async Task<List<ExtensionStatus>> CheckRequiredExtensionsAsync(
        NpgsqlConnection connection)
    {
        var requiredExtensions = new[] { "pg_trgm", "btree_gin", "btree_gist", "uuid-ossp" };
        var status = new List<ExtensionStatus>();
        
        foreach (var extension in requiredExtensions)
        {
            var exists = await connection.QuerySingleOrDefaultAsync<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM pg_extension WHERE extname = @extension
                )", new { extension });
            
            status.Add(new ExtensionStatus
            {
                Name = extension,
                Installed = exists,
                Required = true
            });
        }
        
        return status;
    }
}
```

### 2. Monitoring Setup

```csharp
public class ProductionMonitoring
{
    public static void ConfigureMonitoring(IServiceCollection services)
    {
        // Add performance counters
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<IPerformanceMonitor, PostgreSqlPerformanceMonitor>();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole()
                   .AddFile("logs/efcore-{Date}.txt")
                   .SetMinimumLevel(LogLevel.Warning);
        });
        
        // Add health checks
        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql")
            .AddCheck<DatabasePerformanceCheck>("database-performance");
    }
}

public class DatabasePerformanceCheck : IHealthCheck
{
    private readonly NpgsqlConnection _connection;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simple connectivity check
            await _connection.ExecuteScalarAsync("SELECT 1", cancellationToken);
            
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Database response time is slow: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            return HealthCheckResult.Healthy(
                $"Database is healthy. Response time: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database is unreachable", ex);
        }
    }
}
```

### 3. Rollback Strategy

```csharp
public class RollbackStrategy
{
    public async Task PrepareRollbackAsync(string backupPath)
    {
        // Create database backup before migration
        var backupCommand = $"pg_dump -h localhost -U postgres -d myapp -f {backupPath}";
        await ExecuteCommandAsync(backupCommand);
        
        // Store application configuration snapshot
        await StoreConfigurationSnapshotAsync();
        
        // Create rollback scripts
        await GenerateRollbackScriptsAsync();
    }
    
    public async Task ExecuteRollbackAsync(string backupPath)
    {
        // Stop application
        await StopApplicationAsync();
        
        // Restore database from backup
        var restoreCommand = $"psql -h localhost -U postgres -d myapp -f {backupPath}";
        await ExecuteCommandAsync(restoreCommand);
        
        // Restore application configuration
        await RestoreConfigurationAsync();
        
        // Start application with previous version
        await StartApplicationAsync();
        
        // Verify rollback success
        await VerifyRollbackAsync();
    }
}
```

## Conclusion

This migration guide provides comprehensive strategies for moving to PostgreSQL-optimized Entity Framework Core implementations. Key success factors include:

### Migration Best Practices
1. **Thorough assessment** before starting migration
2. **Phased approach** to minimize risks
3. **Comprehensive testing** at each stage
4. **Performance validation** to ensure improvements
5. **Robust rollback planning** for risk mitigation

### Expected Benefits
- **Performance improvements**: 2-25x faster queries with proper optimization
- **Enhanced functionality**: Full-text search, JSONB, arrays
- **Better scalability**: Optimized indexing and bulk operations
- **Reduced complexity**: Native support for semi-structured data

### Post-Migration Considerations
- **Continuous monitoring** to identify optimization opportunities
- **Regular maintenance** of indexes and statistics
- **Performance testing** as data volumes grow
- **Documentation updates** for new query patterns and optimization strategies

The migration to PostgreSQL-optimized Entity Framework Core provides significant performance and functionality benefits when executed systematically with proper planning and validation.