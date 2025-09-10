// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class QueryOptimizationExtensionsTests
{
    #region Test Entity Classes

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<EntityTypeBuilder<T>> CreateMockEntityTypeBuilder<T>() where T : class
    {
        var mockEntityBuilder = new Mock<EntityTypeBuilder<T>>();
        var mockPropertyBuilder = new Mock<PropertyBuilder<object>>();
        var mockIndexBuilder = new Mock<IndexBuilder>();
        
        // Setup HasAnnotation to return the same builder for chaining
        mockEntityBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                        .Returns(mockEntityBuilder.Object);
        
        // Setup Property method
        mockEntityBuilder.Setup(x => x.Property(It.IsAny<Expression<Func<T, object>>>()))
                        .Returns(mockPropertyBuilder.Object);
        
        // Setup HasIndex method
        mockEntityBuilder.Setup(x => x.HasIndex(It.IsAny<Expression<Func<T, object>>>()))
                        .Returns(mockIndexBuilder.Object);
                        
        // Setup property builder methods
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);
                          
        // Setup index builder methods
        mockIndexBuilder.Setup(x => x.HasDatabaseName(It.IsAny<string>()))
                       .Returns(mockIndexBuilder.Object);
        mockIndexBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                       .Returns(mockIndexBuilder.Object);

        return mockEntityBuilder;
    }

    #endregion

    #region OptimizeForSqliteBulkReads Tests

    [Fact]
    public void OptimizeForSqliteBulkReads_WithDefaultParameters_ShouldApplyOptimizations()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkReads();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify default optimizations are applied
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForBulkReads", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QuerySplittingBehavior", QuerySplittingBehavior.SplitQuery), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:DefaultTrackingBehavior", QueryTrackingBehavior.NoTracking), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteBulkReads_WithCustomParameters_ShouldApplyCustomSettings()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkReads(
            splitQuery: false, 
            trackingBehavior: QueryTrackingBehavior.TrackAll);

        // Assert
        Assert.NotNull(result);
        
        // Verify custom settings are applied
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:DefaultTrackingBehavior", QueryTrackingBehavior.TrackAll), Times.Once);
        // When splitQuery is false, the annotation should not be set
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QuerySplittingBehavior", It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void OptimizeForSqliteBulkReads_WithNullEntityBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        EntityTypeBuilder<Product> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.OptimizeForSqliteBulkReads());
    }

    #endregion

    #region EnableSqliteQueryPlanCaching Tests

    [Fact]
    public void EnableSqliteQueryPlanCaching_WithDefaultParameters_ShouldEnableCaching()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteQueryPlanCaching();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify default caching settings
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryPlanCaching", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryPlanCacheSize", 100), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:PreparedStatementCaching", true), Times.Once);
    }

    [Fact]
    public void EnableSqliteQueryPlanCaching_WithCustomCacheSize_ShouldSetCacheSize()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();
        const int cacheSize = 200;

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteQueryPlanCaching(cacheSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryPlanCacheSize", cacheSize), Times.Once);
    }

    [Fact]
    public void EnableSqliteQueryPlanCaching_WithStatisticsEnabled_ShouldEnableStatistics()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteQueryPlanCaching(enableStatistics: true);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryStatistics", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:StatisticsCollection", "Detailed"), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnableSqliteQueryPlanCaching_WithInvalidCacheSize_ShouldThrowArgumentOutOfRangeException(int cacheSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockEntityBuilder.Object.EnableSqliteQueryPlanCaching(cacheSize));
    }

    #endregion

    #region OptimizeForSqliteAggregations Tests

    [Fact]
    public void OptimizeForSqliteAggregations_WithValidProperties_ShouldCreateAggregateOptimizations()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<OrderItem>();
        Expression<Func<OrderItem, object>>[] aggregateProps = 
        {
            o => o.Quantity,
            o => o.UnitPrice,
            o => o.TotalAmount
        };

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteAggregations(aggregateProps);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify aggregation optimizations
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForAggregations", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:ParallelAggregation", true), Times.Once);
        
        // Verify index creation for each property
        mockEntityBuilder.Verify(x => x.HasIndex(It.IsAny<Expression<Func<OrderItem, object>>>()), Times.Exactly(3));
    }

    [Fact]
    public void OptimizeForSqliteAggregations_WithEmptyProperties_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<OrderItem>();
        var emptyProps = Array.Empty<Expression<Func<OrderItem, object>>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.OptimizeForSqliteAggregations(emptyProps));
    }

    [Fact]
    public void OptimizeForSqliteAggregations_WithNullProperties_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<OrderItem>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            mockEntityBuilder.Object.OptimizeForSqliteAggregations(null!));
    }

    #endregion

    #region OptimizeForSqliteJoins Tests

    [Fact]
    public void OptimizeForSqliteJoins_WithDefaultParameters_ShouldApplyJoinOptimizations()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteJoins();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify join optimizations
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinStrategy", "Hash"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinBatchSize", 1000), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizeForeignKeyIndexes", true), Times.Once);
    }

    [Theory]
    [InlineData(QueryOptimizationExtensions.SqliteJoinStrategy.NestedLoop)]
    [InlineData(QueryOptimizationExtensions.SqliteJoinStrategy.Hash)]
    [InlineData(QueryOptimizationExtensions.SqliteJoinStrategy.Auto)]
    public void OptimizeForSqliteJoins_WithDifferentStrategies_ShouldSetStrategy(QueryOptimizationExtensions.SqliteJoinStrategy strategy)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteJoins(strategy);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinStrategy", strategy.ToString()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OptimizeForSqliteJoins_WithInvalidBatchSize_ShouldThrowArgumentOutOfRangeException(int batchSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockEntityBuilder.Object.OptimizeForSqliteJoins(batchSize: batchSize));
    }

    #endregion

    #region OptimizeForSqliteFullTextSearch Tests

    [Fact]
    public void OptimizeForSqliteFullTextSearch_WithValidProperties_ShouldConfigureFTS()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        Expression<Func<Product, object>>[] searchProps = 
        {
            p => p.Name,
            p => p.Description
        };

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteFullTextSearch(searchProps);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify FTS configuration
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "FTS5"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", "porter"), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteFullTextSearch_WithPorterDisabled_ShouldNotSetPorterTokenizer()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        Expression<Func<Product, object>>[] searchProps = { p => p.Name };

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteFullTextSearch(searchProps, enablePorter: false);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void OptimizeForSqliteFullTextSearch_WithEmptyProperties_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        var emptyProps = Array.Empty<Expression<Func<Product, object>>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.OptimizeForSqliteFullTextSearch(emptyProps));
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void QueryOptimizationMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act
        var result = mockEntityBuilder.Object
            .OptimizeForSqliteBulkReads()
            .EnableSqliteQueryPlanCaching()
            .OptimizeForSqliteJoins();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify all methods were called
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForBulkReads", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryPlanCaching", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinStrategy", "Hash"), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CombinedQueryOptimizations_ShouldApplyAllSettings()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        Expression<Func<Product, object>>[] aggregateProps = { p => p.Price };
        Expression<Func<Product, object>>[] searchProps = { p => p.Name };

        // Act
        mockEntityBuilder.Object
            .OptimizeForSqliteBulkReads(splitQuery: true, trackingBehavior: QueryTrackingBehavior.NoTracking)
            .EnableSqliteQueryPlanCaching(cacheSize: 500, enableStatistics: true)
            .OptimizeForSqliteAggregations(aggregateProps)
            .OptimizeForSqliteJoins(QueryOptimizationExtensions.SqliteJoinStrategy.Auto, batchSize: 2000)
            .OptimizeForSqliteFullTextSearch(searchProps, enablePorter: true);

        // Assert - Verify all optimizations were applied
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForBulkReads", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryPlanCacheSize", 500), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryStatistics", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForAggregations", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinStrategy", "Auto"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinBatchSize", 2000), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", "porter"), Times.Once);
    }

    #endregion

    #region Parameter Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void EnableSqliteQueryPlanCaching_WithValidCacheSizes_ShouldSucceed(int cacheSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteQueryPlanCaching(cacheSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:QueryPlanCacheSize", cacheSize), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(5000)]
    public void OptimizeForSqliteJoins_WithValidBatchSizes_ShouldSucceed(int batchSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteJoins(batchSize: batchSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JoinBatchSize", batchSize), Times.Once);
    }

    #endregion
}