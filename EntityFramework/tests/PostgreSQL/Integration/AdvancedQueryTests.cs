// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL advanced query features.
/// Tests CTEs, recursive queries, materialized views, and window functions.
/// </summary>
public sealed class AdvancedQueryTests : PostgreSqlIntegrationTestBase
{
    public AdvancedQueryTests(PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CommonTableExpressions_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateDbContextOptions<AdvancedQueryTestDbContext>();

        await using var context = new AdvancedQueryTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Insert test data
        var orders = new[]
        {
            new OrderEntity { OrderNumber = "ORD001", OrderDate = DateTime.Today.AddDays(-10), TotalAmount = 150.00m, CustomerId = "CUST001", Status = "Completed" },
            new OrderEntity { OrderNumber = "ORD002", OrderDate = DateTime.Today.AddDays(-8), TotalAmount = 250.50m, CustomerId = "CUST002", Status = "Completed" },
            new OrderEntity { OrderNumber = "ORD003", OrderDate = DateTime.Today.AddDays(-5), TotalAmount = 75.25m, CustomerId = "CUST001", Status = "Pending" },
            new OrderEntity { OrderNumber = "ORD004", OrderDate = DateTime.Today.AddDays(-3), TotalAmount = 320.75m, CustomerId = "CUST003", Status = "Completed" },
            new OrderEntity { OrderNumber = "ORD005", OrderDate = DateTime.Today.AddDays(-1), TotalAmount = 99.99m, CustomerId = "CUST002", Status = "Processing" }
        };

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();

        // Act & Assert - Test CTE query
        var cteResult = await ExecuteScalarAsync<decimal>("""
            WITH customer_totals AS (
                SELECT 
                    customer_id,
                    SUM(total_amount) as total_spent,
                    COUNT(*) as order_count
                FROM order_entities 
                WHERE status = 'Completed'
                GROUP BY customer_id
            ),
            customer_ranks AS (
                SELECT 
                    customer_id,
                    total_spent,
                    order_count,
                    ROW_NUMBER() OVER (ORDER BY total_spent DESC) as rank
                FROM customer_totals
            )
            SELECT total_spent 
            FROM customer_ranks 
            WHERE rank = 1;
            """);

        cteResult.Should().Be(320.75m); // CUST003 has the highest single completed order
    }

    [Fact]
    public async Task RecursiveQueries_ShouldHandleHierarchicalData()
    {
        // Arrange - Create a simple hierarchy table
        await ExecuteSqlAsync("""
            CREATE TABLE IF NOT EXISTS categories (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                parent_id INTEGER REFERENCES categories(id)
            );
            
            TRUNCATE TABLE categories RESTART IDENTITY CASCADE;
            
            INSERT INTO categories (name, parent_id) VALUES 
                ('Electronics', NULL),
                ('Computers', 1),
                ('Laptops', 2),
                ('Gaming Laptops', 3),
                ('Business Laptops', 3),
                ('Mobile Phones', 1),
                ('Smartphones', 6),
                ('Feature Phones', 6);
            """);

        // Act & Assert - Test recursive CTE
        var hierarchyResult = await ExecuteScalarAsync<long>("""
            WITH RECURSIVE category_tree AS (
                -- Base case: root categories
                SELECT id, name, parent_id, 1 as level, name as path
                FROM categories
                WHERE parent_id IS NULL
                
                UNION ALL
                
                -- Recursive case: children of current level
                SELECT c.id, c.name, c.parent_id, ct.level + 1, ct.path || ' -> ' || c.name
                FROM categories c
                INNER JOIN category_tree ct ON c.parent_id = ct.id
            )
            SELECT COUNT(*) FROM category_tree WHERE level <= 3;
            """);

        hierarchyResult.Should().BeGreaterThan(0);
        
        // Test getting full paths
        var pathResult = await ExecuteScalarAsync<string>("""
            WITH RECURSIVE category_tree AS (
                SELECT id, name, parent_id, 1 as level, name as path
                FROM categories
                WHERE parent_id IS NULL
                
                UNION ALL
                
                SELECT c.id, c.name, c.parent_id, ct.level + 1, ct.path || ' -> ' || c.name
                FROM categories c
                INNER JOIN category_tree ct ON c.parent_id = ct.id
            )
            SELECT path FROM category_tree WHERE name = 'Gaming Laptops';
            """);

        pathResult.Should().Be("Electronics -> Computers -> Laptops -> Gaming Laptops");
    }

    [Fact]
    public async Task WindowFunctions_ShouldProvideAnalyticalCapabilities()
    {
        // Arrange
        var options = CreateDbContextOptions<AdvancedQueryTestDbContext>();

        await using var context = new AdvancedQueryTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Create orders with items for window function testing
        var testOrders = new[]
        {
            new OrderEntity { OrderNumber = "ORD001", OrderDate = DateTime.Today.AddDays(-10), TotalAmount = 150.00m, CustomerId = "CUST001", Status = "Completed" },
            new OrderEntity { OrderNumber = "ORD002", OrderDate = DateTime.Today.AddDays(-8), TotalAmount = 250.50m, CustomerId = "CUST001", Status = "Completed" },
            new OrderEntity { OrderNumber = "ORD003", OrderDate = DateTime.Today.AddDays(-5), TotalAmount = 75.25m, CustomerId = "CUST002", Status = "Completed" }
        };

        await context.Orders.AddRangeAsync(testOrders);
        await context.SaveChangesAsync();

        var order1 = testOrders[0];
        var order2 = testOrders[1];
        var order3 = testOrders[2];

        var orderItems = new[]
        {
            new OrderItemEntity { OrderId = order1.Id, ProductName = "Laptop", Quantity = 1, UnitPrice = 150.00m },
            new OrderItemEntity { OrderId = order2.Id, ProductName = "Mouse", Quantity = 2, UnitPrice = 25.00m },
            new OrderItemEntity { OrderId = order2.Id, ProductName = "Keyboard", Quantity = 1, UnitPrice = 200.50m },
            new OrderItemEntity { OrderId = order3.Id, ProductName = "Monitor", Quantity = 1, UnitPrice = 75.25m }
        };

        await context.OrderItems.AddRangeAsync(orderItems);
        await context.SaveChangesAsync();

        // Act & Assert - Test various window functions

        // Test ROW_NUMBER()
        var rowNumberResult = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM (
                SELECT 
                    order_number,
                    total_amount,
                    ROW_NUMBER() OVER (ORDER BY total_amount DESC) as rn
                FROM order_entities
            ) ranked WHERE rn <= 2;
            """);
        rowNumberResult.Should().Be(2);

        // Test RANK() and DENSE_RANK()
        var rankResult = await ExecuteScalarAsync<long>("""
            SELECT COUNT(DISTINCT rnk) FROM (
                SELECT 
                    customer_id,
                    total_amount,
                    RANK() OVER (PARTITION BY customer_id ORDER BY total_amount DESC) as rnk
                FROM order_entities
            ) ranked;
            """);
        rankResult.Should().BeGreaterThan(0);

        // Test aggregate window functions
        var runningTotalResult = await ExecuteScalarAsync<decimal>("""
            SELECT MAX(running_total) FROM (
                SELECT 
                    order_number,
                    total_amount,
                    SUM(total_amount) OVER (ORDER BY order_date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as running_total
                FROM order_entities
                ORDER BY order_date
            ) rt;
            """);
        runningTotalResult.Should().Be(475.75m); // Sum of all orders

        // Test LAG and LEAD functions
        var lagLeadResult = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM (
                SELECT 
                    order_number,
                    total_amount,
                    LAG(total_amount) OVER (ORDER BY order_date) as prev_amount,
                    LEAD(total_amount) OVER (ORDER BY order_date) as next_amount
                FROM order_entities
            ) lagged WHERE prev_amount IS NOT NULL OR next_amount IS NOT NULL;
            """);
        lagLeadResult.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MaterializedViews_ShouldProvidePerformanceOptimization()
    {
        // Arrange
        var options = CreateDbContextOptions<AdvancedQueryTestDbContext>();

        await using var context = new AdvancedQueryTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Insert test data
        var orders = Enumerable.Range(1, 100).Select(i => new OrderEntity
        {
            OrderNumber = $"ORD{i:000}",
            OrderDate = DateTime.Today.AddDays(-i),
            TotalAmount = i * 10.50m,
            CustomerId = $"CUST{i % 10:00}",
            Status = i % 3 == 0 ? "Completed" : "Processing"
        }).ToArray();

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();

        // Create materialized view
        await ExecuteSqlAsync("""
            DROP MATERIALIZED VIEW IF EXISTS customer_summary;
            
            CREATE MATERIALIZED VIEW customer_summary AS
            SELECT 
                customer_id,
                COUNT(*) as order_count,
                SUM(total_amount) as total_spent,
                AVG(total_amount) as avg_order_value,
                MAX(order_date) as last_order_date,
                MIN(order_date) as first_order_date
            FROM order_entities
            GROUP BY customer_id;
            
            CREATE UNIQUE INDEX ON customer_summary (customer_id);
            """);

        // Act - Query the materialized view
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var mvResult = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM customer_summary WHERE total_spent > 500;
            """);
        
        stopwatch.Stop();
        var mvQueryTime = stopwatch.ElapsedMilliseconds;

        // Compare with equivalent query on base table
        stopwatch.Restart();
        
        var baseResult = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM (
                SELECT 
                    customer_id,
                    SUM(total_amount) as total_spent
                FROM order_entities
                GROUP BY customer_id
                HAVING SUM(total_amount) > 500
            ) t;
            """);
        
        stopwatch.Stop();
        var baseQueryTime = stopwatch.ElapsedMilliseconds;

        // Assert
        mvResult.Should().Be(baseResult); // Results should be identical
        mvResult.Should().BeGreaterThan(0);
        
        Console.WriteLine($"Materialized view query time: {mvQueryTime}ms");
        Console.WriteLine($"Base table query time: {baseQueryTime}ms");
        
        // Materialized view should be faster or comparable
        mvQueryTime.Should().BeLessThanOrEqualTo(baseQueryTime + 50); // Allow some variance

        // Test materialized view refresh
        await ExecuteSqlAsync("REFRESH MATERIALIZED VIEW customer_summary;");
        
        var refreshedResult = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM customer_summary WHERE total_spent > 500;
            """);
        
        refreshedResult.Should().Be(mvResult); // Should be same after refresh
    }

    [Fact]
    public async Task ComplexAnalyticalQueries_ShouldCombineMultipleFeatures()
    {
        // Arrange
        var options = CreateDbContextOptions<AdvancedQueryTestDbContext>();

        await using var context = new AdvancedQueryTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Create comprehensive test data
        var orders = Enumerable.Range(1, 50).Select(i => new OrderEntity
        {
            OrderNumber = $"ORD{i:000}",
            OrderDate = DateTime.Today.AddDays(-i),
            TotalAmount = (decimal)(100 + (i * 13.7) % 200), // Varying amounts
            CustomerId = $"CUST{i % 5:00}", // 5 customers
            Status = new[] { "Completed", "Processing", "Cancelled" }[i % 3]
        }).ToArray();

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();

        // Act - Complex analytical query combining CTEs, window functions, and aggregations
        var complexResult = await ExecuteScalarAsync<string>("""
            WITH monthly_stats AS (
                SELECT 
                    DATE_TRUNC('month', order_date) as order_month,
                    customer_id,
                    COUNT(*) as orders_count,
                    SUM(total_amount) as month_total,
                    AVG(total_amount) as month_avg
                FROM order_entities
                WHERE status = 'Completed'
                GROUP BY DATE_TRUNC('month', order_date), customer_id
            ),
            customer_rankings AS (
                SELECT 
                    order_month,
                    customer_id,
                    month_total,
                    ROW_NUMBER() OVER (PARTITION BY order_month ORDER BY month_total DESC) as monthly_rank,
                    SUM(month_total) OVER (PARTITION BY customer_id ORDER BY order_month ROWS UNBOUNDED PRECEDING) as cumulative_total
                FROM monthly_stats
            ),
            top_customers AS (
                SELECT 
                    customer_id,
                    SUM(month_total) as total_spent,
                    COUNT(DISTINCT order_month) as active_months,
                    AVG(monthly_rank) as avg_monthly_rank
                FROM customer_rankings
                GROUP BY customer_id
            )
            SELECT customer_id
            FROM top_customers
            WHERE total_spent = (SELECT MAX(total_spent) FROM top_customers);
            """);

        // Assert
        complexResult.Should().NotBeNullOrEmpty();
        complexResult.Should().StartWith("CUST");

        // Additional validation - ensure the query actually processed data
        var completedOrdersCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM order_entities WHERE status = 'Completed';
            """);
        
        completedOrdersCount.Should().BeGreaterThan(0);
    }
}

/// <summary>
/// Test DbContext for advanced query testing.
/// </summary>
public class AdvancedQueryTestDbContext : DbContext
{
    public AdvancedQueryTestDbContext(DbContextOptions<AdvancedQueryTestDbContext> options) : base(options) { }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            
            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId);
        });

        modelBuilder.Entity<OrderItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(e => e.TotalPrice); // Computed property
        });
    }
}