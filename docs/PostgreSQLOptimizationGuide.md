# PostgreSQL Optimization Guide for Entity Framework Core

This comprehensive guide covers PostgreSQL-specific optimizations available in the Wangkanai.EntityFramework.Postgres package, providing detailed examples and best practices for maximizing performance in .NET applications.

## Table of Contents

1. [Overview](#overview)
2. [JSONB Optimizations](#jsonb-optimizations)
3. [Full-Text Search](#full-text-search)
4. [Array Operations](#array-operations)
5. [Advanced Indexing](#advanced-indexing)
6. [Bulk Operations](#bulk-operations)
7. [Query Optimization](#query-optimization)
8. [Performance Monitoring](#performance-monitoring)
9. [Best Practices](#best-practices)

## Overview

PostgreSQL offers unique features that can significantly improve application performance when used correctly. This guide demonstrates how to leverage these features through Entity Framework Core with our optimized extensions.

### Key Performance Features

- **JSONB**: Binary JSON storage with indexing support
- **Full-Text Search**: Native text search with ranking and highlighting
- **Array Types**: Native array support with specialized operators
- **Advanced Indexing**: GIN, GiST, partial, and expression indexes
- **Bulk Operations**: COPY protocol for high-throughput data operations

## JSONB Optimizations

JSONB provides efficient JSON storage and querying capabilities that often outperform traditional relational approaches for semi-structured data.

### Basic JSONB Configuration

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string Specifications { get; set; }
}

// In your DbContext's OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        // Configure JSONB column with GIN index
        entity.Property(e => e.Specifications)
            .HasJsonbColumn()
            .HasJsonbGinIndex("IX_products_specifications_gin");
    });
}
```

### Advanced JSONB Indexing Strategies

#### 1. Path-Specific Indexing

For queries targeting specific JSON paths:

```csharp
modelBuilder.Entity<Product>(entity =>
{
    // Index for specific JSON path queries
    entity.HasJsonbPathOpsIndex(
        e => e.Specifications,
        "IX_products_specs_path_ops");
    
    // Expression index for nested paths
    entity.HasJsonbExpressionIndex(
        "(specifications -> 'technical' -> 'cpu')",
        "IX_products_cpu_specs");
});

// Optimized query
var highPerformanceCpus = await context.Products
    .Where(p => EF.Functions.JsonExtractPathText(p.Specifications, "technical", "cpu", "cores") == "8")
    .ToListAsync();
```

#### 2. Partial JSONB Indexes

For queries with common filter conditions:

```csharp
modelBuilder.Entity<Product>(entity =>
{
    // Partial index for active products only
    entity.HasJsonbPartialIndex(
        e => e.Specifications,
        "specifications @> '{\"status\": \"active\"}'",
        "IX_products_active_specs");
});

// Efficient query for active products
var activeProducts = await context.Products
    .Where(p => EF.Functions.JsonContains(p.Specifications, "{\"status\": \"active\"}"))
    .ToListAsync();
```

### JSONB Query Patterns

#### Containment Queries

```csharp
// Find products with specific features
var gamingProducts = await context.Products
    .Where(p => EF.Functions.JsonContains(p.Specifications, 
        JsonSerializer.Serialize(new { category = "gaming", features = new[] { "rgb", "wireless" } })))
    .ToListAsync();
```

#### Existence Queries

```csharp
// Find products with warranty information
var warrantyProducts = await context.Products
    .Where(p => EF.Functions.JsonExists(p.Specifications, "warranty"))
    .ToListAsync();
```

#### Path-Based Queries

```csharp
// Find products by nested properties
var premiumProducts = await context.Products
    .Where(p => EF.Functions.JsonExtractPathText(p.Specifications, "tier") == "premium")
    .OrderBy(p => EF.Functions.JsonExtractPath<decimal>(p.Specifications, "price"))
    .ToListAsync();
```

## Full-Text Search

PostgreSQL's full-text search capabilities provide powerful and efficient text searching with ranking, highlighting, and language-specific features.

### Basic Full-Text Search Setup

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    
    [Column(TypeName = "tsvector")]
    public NpgsqlTsVector SearchVector { get; set; }
}

// Configuration
modelBuilder.Entity<Article>(entity =>
{
    // Generated tsvector with weighted content
    entity.Property(e => e.SearchVector)
        .HasGeneratedTsVector(
            new[] { "title", "content" },
            "english",
            new[] { TsVectorWeight.A, TsVectorWeight.D });
    
    // GIN index for fast searching
    entity.Property(e => e.SearchVector)
        .HasTsVectorGinIndex("IX_articles_search_gin");
});
```

### Multi-Column Full-Text Search

```csharp
modelBuilder.Entity<Article>(entity =>
{
    // Combine multiple columns with different weights
    entity.HasMultiColumnFullTextSearch("SearchVector", new[]
    {
        new FullTextColumnConfig("title", TsVectorWeight.A),     // Highest priority
        new FullTextColumnConfig("summary", TsVectorWeight.B),   // High priority
        new FullTextColumnConfig("content", TsVectorWeight.C),   // Medium priority
        new FullTextColumnConfig("tags", TsVectorWeight.D)       // Lowest priority
    });
});
```

### Search Query Types

#### Plain Text Search

```csharp
// Simple text search
var results = await context.Articles
    .Where(a => a.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", "postgresql database")))
    .OrderByDescending(a => a.SearchVector.Rank(EF.Functions.PlainToTsQuery("english", "postgresql database")))
    .Take(20)
    .ToListAsync();
```

#### Boolean Search

```csharp
// Boolean operators: AND (&), OR (|), NOT (!)
var complexSearch = await context.Articles
    .Where(a => a.SearchVector.Matches(EF.Functions.ToTsQuery("english", "postgresql & performance & !mysql")))
    .OrderByDescending(a => a.SearchVector.Rank(EF.Functions.ToTsQuery("english", "postgresql & performance & !mysql")))
    .ToListAsync();
```

#### Phrase Search

```csharp
// Exact phrase matching
var phraseResults = await context.Articles
    .Where(a => a.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", "entity framework core")))
    .ToListAsync();
```

### Search with Highlighting

```csharp
// Configure highlighting
modelBuilder.Entity<Article>(entity =>
{
    entity.Property(e => e.Content)
        .HasSearchHighlighting(new SearchHighlightOptions
        {
            StartTag = "<mark>",
            StopTag = "</mark>",
            MaxWords = 50,
            MinWords = 15,
            MaxFragments = 3
        });
});

// Query with highlighted results
var searchTerm = "postgresql optimization";
var query = EF.Functions.PlainToTsQuery("english", searchTerm);

var highlightedResults = await context.Articles
    .Where(a => a.SearchVector.Matches(query))
    .Select(a => new
    {
        a.Id,
        a.Title,
        HighlightedContent = EF.Functions.TsHeadline("english", a.Content, query, 
            "StartSel=<mark>, StopSel=</mark>, MaxWords=50, MinWords=15"),
        Rank = a.SearchVector.Rank(query)
    })
    .OrderByDescending(a => a.Rank)
    .Take(10)
    .ToListAsync();
```

## Array Operations

PostgreSQL's native array support provides efficient storage and querying for collections of values.

### Array Configuration

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string[] Tags { get; set; }
    public int[] Ratings { get; set; }
    public decimal[] PriceHistory { get; set; }
}

// Index configuration
modelBuilder.Entity<Product>(entity =>
{
    // GIN indexes for array operations
    entity.HasIndex(e => e.Tags)
        .HasMethod("gin")
        .HasDatabaseName("IX_products_tags_gin");
    
    entity.HasIndex(e => e.Ratings)
        .HasMethod("gin")
        .HasDatabaseName("IX_products_ratings_gin");
});
```

### Array Query Operations

#### Contains Operations

```csharp
// Find products with specific tags
var electronicsProducts = await context.Products
    .Where(p => p.Tags.Contains("electronics"))
    .ToListAsync();

// Find products with any of the specified tags
var searchTags = new[] { "gaming", "professional", "budget" };
var relevantProducts = await context.Products
    .Where(p => p.Tags.Any(tag => searchTags.Contains(tag)))
    .ToListAsync();
```

#### Array Aggregations

```csharp
// Products with high average ratings
var highRatedProducts = await context.Products
    .Where(p => p.Ratings.Average() >= 4.0)
    .OrderByDescending(p => p.Ratings.Average())
    .ToListAsync();

// Products with consistent pricing
var stablePricedProducts = await context.Products
    .Where(p => p.PriceHistory.Max() - p.PriceHistory.Min() < 50)
    .ToListAsync();
```

#### Array Length and Position Operations

```csharp
// Products with multiple tags and ratings
var wellDocumentedProducts = await context.Products
    .Where(p => p.Tags.Length >= 3 && p.Ratings.Length >= 10)
    .ToListAsync();
```

## Advanced Indexing

PostgreSQL supports various index types optimized for different query patterns.

### Index Types and Use Cases

#### B-tree Indexes (Default)

```csharp
// Standard indexes for equality and range queries
modelBuilder.Entity<Order>(entity =>
{
    entity.HasIndex(e => e.CreatedAt)
        .HasDatabaseName("IX_orders_created_at");
    
    // Composite index for multiple column queries
    entity.HasIndex(e => new { e.CustomerId, e.Status, e.CreatedAt })
        .HasDatabaseName("IX_orders_customer_status_date");
});
```

#### Partial Indexes

```csharp
// Index only active records to save space and improve performance
modelBuilder.Entity<User>(entity =>
{
    entity.HasIndex(e => e.Email)
        .HasDatabaseName("IX_users_active_email")
        .HasFilter("is_active = true");
    
    entity.HasIndex(e => e.LastLoginAt)
        .HasDatabaseName("IX_users_recent_login")
        .HasFilter("last_login_at >= NOW() - INTERVAL '30 days'");
});
```

#### Expression Indexes

```csharp
// Index on computed expressions
modelBuilder.Entity<User>(entity =>
{
    // Case-insensitive email searches
    entity.HasIndex("lower(email)")
        .HasDatabaseName("IX_users_email_lower");
    
    // Full name search
    entity.HasIndex("first_name || ' ' || last_name")
        .HasDatabaseName("IX_users_full_name");
});
```

#### GiST Indexes for Geometric Data

```csharp
public class Location
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [Column(TypeName = "point")]
    public NpgsqlPoint Coordinates { get; set; }
}

// GiST index for spatial queries
modelBuilder.Entity<Location>(entity =>
{
    entity.HasIndex(e => e.Coordinates)
        .HasMethod("gist")
        .HasDatabaseName("IX_locations_coordinates_gist");
});
```

## Bulk Operations

For high-throughput scenarios, PostgreSQL's COPY protocol provides significant performance improvements over standard INSERT operations.

### Bulk Insert with COPY Protocol

```csharp
public async Task<long> BulkInsertProductsAsync(IEnumerable<Product> products)
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var writer = await connection.BeginBinaryImportAsync(
        "COPY products (name, category, price, specifications, tags) FROM STDIN (FORMAT BINARY)");
    
    foreach (var product in products)
    {
        await writer.StartRowAsync();
        await writer.WriteAsync(product.Name);
        await writer.WriteAsync(product.Category);
        await writer.WriteAsync(product.Price);
        await writer.WriteAsync(product.Specifications, NpgsqlDbType.Jsonb);
        await writer.WriteAsync(product.Tags);
    }
    
    return await writer.CompleteAsync();
}
```

### Bulk Update Strategies

```csharp
// Efficient bulk updates using temporary tables
public async Task BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates)
{
    var tempTableSql = @"
        CREATE TEMPORARY TABLE temp_price_updates (
            product_id INT,
            new_price DECIMAL(10,2)
        ) ON COMMIT DROP";
    
    await context.Database.ExecuteSqlRawAsync(tempTableSql);
    
    // Bulk insert to temp table using COPY
    using var connection = (NpgsqlConnection)context.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
        await connection.OpenAsync();
    
    using var writer = await connection.BeginBinaryImportAsync(
        "COPY temp_price_updates (product_id, new_price) FROM STDIN (FORMAT BINARY)");
    
    foreach (var (productId, price) in priceUpdates)
    {
        await writer.StartRowAsync();
        await writer.WriteAsync(productId);
        await writer.WriteAsync(price);
    }
    await writer.CompleteAsync();
    
    // Bulk update from temp table
    var updateSql = @"
        UPDATE products 
        SET price = temp.new_price
        FROM temp_price_updates temp
        WHERE products.id = temp.product_id";
    
    await context.Database.ExecuteSqlRawAsync(updateSql);
}
```

## Query Optimization

### Query Planning and Analysis

Use PostgreSQL's query analysis tools to optimize performance:

```sql
-- Analyze query execution plan
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM products 
WHERE specifications @> '{"category": "gaming"}' 
ORDER BY price DESC 
LIMIT 20;

-- Check index usage
SELECT schemaname, tablename, attname, n_distinct, correlation 
FROM pg_stats 
WHERE tablename = 'products';
```

### EF Core Query Optimization Patterns

#### Projection for Large Objects

```csharp
// Only select needed columns
var productSummaries = await context.Products
    .Where(p => p.Tags.Contains("featured"))
    .Select(p => new ProductSummary
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        // Don't load large JSONB or text columns unless needed
    })
    .ToListAsync();
```

#### Efficient Pagination

```csharp
// Use cursor-based pagination for large datasets
public async Task<List<Product>> GetProductPageAsync(int lastId, int pageSize = 20)
{
    return await context.Products
        .Where(p => p.Id > lastId)
        .OrderBy(p => p.Id)
        .Take(pageSize)
        .ToListAsync();
}
```

#### Query Splitting for Complex Joins

```csharp
// Configure split queries for multiple includes
var ordersWithDetails = await context.Orders
    .AsSplitQuery()
    .Include(o => o.OrderItems)
    .Include(o => o.Customer)
    .Where(o => o.CreatedAt >= DateTime.Today.AddMonths(-1))
    .ToListAsync();
```

## Performance Monitoring

### Built-in PostgreSQL Statistics

```sql
-- Monitor query performance
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    rows
FROM pg_stat_statements
ORDER BY total_time DESC
LIMIT 10;

-- Monitor index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

### Application-Level Monitoring

```csharp
// Log slow queries in EF Core
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseNpgsql(connectionString)
        .EnableSensitiveDataLogging()
        .LogTo(Console.WriteLine, LogLevel.Information)
        .ConfigureWarnings(warnings =>
            warnings.Log(RelationalEventId.CommandExecuted));
}
```

## Best Practices

### 1. Index Strategy

- **Create indexes for frequent query patterns**
- **Use partial indexes for filtered queries**
- **Monitor index usage and remove unused indexes**
- **Consider composite indexes for multi-column queries**

### 2. JSONB Optimization

- **Use GIN indexes for containment queries**
- **Use expression indexes for specific JSON paths**
- **Avoid deep nesting in frequently queried paths**
- **Consider denormalization for hot paths**

### 3. Full-Text Search

- **Use appropriate text search configurations for your language**
- **Weight different columns by importance**
- **Consider stemming and stop word removal**
- **Implement proper ranking for result relevance**

### 4. Array Operations

- **Use GIN indexes for array containment queries**
- **Consider normalization for complex array queries**
- **Leverage PostgreSQL array operators for efficient queries**

### 5. Connection Management

- **Use connection pooling appropriately**
- **Configure connection pool size based on load**
- **Monitor connection usage and timeouts**
- **Use async operations consistently**

### 6. Query Optimization

- **Profile queries using EXPLAIN ANALYZE**
- **Use projections to limit data transfer**
- **Implement efficient pagination strategies**
- **Consider query splitting for complex scenarios**

### 7. Transaction Management

- **Keep transactions short**
- **Use appropriate isolation levels**
- **Consider read-only transactions for queries**
- **Implement proper error handling and rollback**

## Conclusion

PostgreSQL's advanced features provide significant performance benefits when properly utilized with Entity Framework Core. By implementing these optimization strategies, you can achieve substantial improvements in query performance, data throughput, and overall application responsiveness.

Key takeaways:
- **JSONB** excels for semi-structured data with complex queries
- **Full-text search** provides powerful text searching capabilities
- **Array operations** offer efficient collection handling
- **Advanced indexing** strategies improve query performance
- **Bulk operations** dramatically increase data throughput
- **Proper monitoring** ensures continued optimization

Regular performance monitoring and query analysis will help you identify optimization opportunities and maintain optimal performance as your application grows.