// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class IndexConfigurationExtensionsTests
{
    #region Test Entity Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsActive { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<IndexBuilder<T>> CreateMockIndexBuilder<T>() where T : class
    {
        var mockIndexBuilder = new Mock<IndexBuilder<T>>();
        
        mockIndexBuilder.Setup(x => x.HasFilter(It.IsAny<string>()))
                       .Returns(mockIndexBuilder.Object);
        
        mockIndexBuilder.Setup(x => x.IncludeProperties(It.IsAny<string[]>()))
                       .Returns(mockIndexBuilder.Object);
                       
        mockIndexBuilder.Setup(x => x.IsDescending(It.IsAny<bool>()))
                       .Returns(mockIndexBuilder.Object);
                       
        mockIndexBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                       .Returns(mockIndexBuilder.Object);

        return mockIndexBuilder;
    }

    private static Mock<EntityTypeBuilder<T>> CreateMockEntityTypeBuilder<T>() where T : class
    {
        var mockEntityBuilder = new Mock<EntityTypeBuilder<T>>();
        var mockIndexBuilder = new Mock<IndexBuilder>();
        
        mockEntityBuilder.Setup(x => x.HasIndex(It.IsAny<Expression<Func<T, object>>>(), It.IsAny<string>()))
                        .Returns(mockIndexBuilder.Object);
                        
        mockIndexBuilder.Setup(x => x.HasDatabaseName(It.IsAny<string>()))
                       .Returns(mockIndexBuilder.Object);
                       
        mockIndexBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                       .Returns(mockIndexBuilder.Object);

        return mockEntityBuilder;
    }

    #endregion

    #region HasSqlitePartialIndex Tests

    [Fact]
    public void HasSqlitePartialIndex_WithValidFilter_ShouldApplyFilter()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<TestEntity>();
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;

        // Act
        var result = mockIndexBuilder.Object.HasSqlitePartialIndex(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockIndexBuilder.Object, result);
        
        // Verify filter was applied (the exact SQL would be complex to verify in unit tests)
        mockIndexBuilder.Verify(x => x.HasFilter(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void HasSqlitePartialIndex_WithNullIndexBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        IndexBuilder<TestEntity> nullBuilder = null!;
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBuilder.HasSqlitePartialIndex(filter));
    }

    [Fact]
    public void HasSqlitePartialIndex_WithNullFilter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            mockIndexBuilder.Object.HasSqlitePartialIndex(null!));
    }

    #endregion

    #region HasSqliteCoveringIndex Tests

    [Fact]
    public void HasSqliteCoveringIndex_WithValidProperties_ShouldIncludeProperties()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();
        Expression<Func<Order, object>>[] includeProps = 
        {
            o => o.OrderDate,
            o => o.TotalAmount
        };

        // Act
        var result = mockIndexBuilder.Object.HasSqliteCoveringIndex(includeProps);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockIndexBuilder.Object, result);
        
        // Verify properties were included
        mockIndexBuilder.Verify(x => x.IncludeProperties(It.IsAny<string[]>()), Times.Once);
    }

    [Fact]
    public void HasSqliteCoveringIndex_WithEmptyProperties_ShouldThrowArgumentException()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();
        var emptyProps = Array.Empty<Expression<Func<Order, object>>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockIndexBuilder.Object.HasSqliteCoveringIndex(emptyProps));
    }

    [Fact]
    public void HasSqliteCoveringIndex_WithNullProperties_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            mockIndexBuilder.Object.HasSqliteCoveringIndex(null!));
    }

    #endregion

    #region HasSqliteExpressionIndex Tests

    [Fact]
    public void HasSqliteExpressionIndex_WithValidExpression_ShouldCreateExpressionIndex()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Customer>();
        Expression<Func<Customer, object>> expression = c => c.Email.ToLower();
        const string indexName = "IX_Customer_Email_Lower";

        // Act
        var result = mockEntityBuilder.Object.HasSqliteExpressionIndex(expression, indexName);

        // Assert
        Assert.NotNull(result);
        
        // Verify index creation
        mockEntityBuilder.Verify(x => x.HasIndex(expression, indexName), Times.Once);
    }

    [Fact]
    public void HasSqliteExpressionIndex_WithNullExpression_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Customer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            mockEntityBuilder.Object.HasSqliteExpressionIndex<Customer>(null!, "IndexName"));
    }

    [Fact]
    public void HasSqliteExpressionIndex_WithoutIndexName_ShouldGenerateName()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Customer>();
        Expression<Func<Customer, object>> expression = c => c.Email;

        // Act
        var result = mockEntityBuilder.Object.HasSqliteExpressionIndex(expression);

        // Assert
        Assert.NotNull(result);
        
        // Verify index was created (name would be auto-generated)
        mockEntityBuilder.Verify(x => x.HasIndex(expression, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region OptimizeForSqliteRangeQueries Tests

    [Fact]
    public void OptimizeForSqliteRangeQueries_WithAscendingOrder_ShouldConfigureAscending()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();

        // Act
        var result = mockIndexBuilder.Object.OptimizeForSqliteRangeQueries<Order>(ascending: true);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockIndexBuilder.Object, result);
        
        mockIndexBuilder.Verify(x => x.IsDescending(false), Times.Once);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForRangeQueries", true), Times.Once);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Sqlite:IndexType", "RANGE_OPTIMIZED"), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteRangeQueries_WithDescendingOrder_ShouldConfigureDescending()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();

        // Act
        var result = mockIndexBuilder.Object.OptimizeForSqliteRangeQueries<Order>(ascending: false);

        // Assert
        Assert.NotNull(result);
        mockIndexBuilder.Verify(x => x.IsDescending(true), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteRangeQueries_WithCollation_ShouldSetCollation()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();
        const string collation = "NOCASE";

        // Act
        var result = mockIndexBuilder.Object.OptimizeForSqliteRangeQueries<Order>(collation: collation);

        // Assert
        Assert.NotNull(result);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Relational:Collation", collation), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteRangeQueries_WithNullIndexBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        IndexBuilder<Order> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.OptimizeForSqliteRangeQueries<Order>());
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void IndexMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<TestEntity>();
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;
        Expression<Func<TestEntity, object>>[] includeProps = { e => e.Name, e => e.Email };

        // Act
        var result = mockIndexBuilder.Object
            .HasSqlitePartialIndex(filter)
            .HasSqliteCoveringIndex(includeProps)
            .OptimizeForSqliteRangeQueries<TestEntity>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockIndexBuilder.Object, result);
        
        // Verify all methods were called
        mockIndexBuilder.Verify(x => x.HasFilter(It.IsAny<string>()), Times.Once);
        mockIndexBuilder.Verify(x => x.IncludeProperties(It.IsAny<string[]>()), Times.Once);
        mockIndexBuilder.Verify(x => x.IsDescending(false), Times.Once); // Default ascending
    }

    #endregion

    #region Integration Style Tests

    [Fact]
    public void CombinedIndexOptimizations_ShouldApplyAllSettings()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();
        Expression<Func<Order, bool>> filter = o => o.IsActive;
        Expression<Func<Order, object>>[] includeProps = { o => o.TotalAmount };

        // Act
        mockIndexBuilder.Object
            .HasSqlitePartialIndex(filter)
            .HasSqliteCoveringIndex(includeProps)
            .OptimizeForSqliteRangeQueries<Order>(ascending: false, collation: "NOCASE");

        // Assert - Verify all optimizations were applied
        mockIndexBuilder.Verify(x => x.HasFilter(It.IsAny<string>()), Times.Once);
        mockIndexBuilder.Verify(x => x.IncludeProperties(It.IsAny<string[]>()), Times.Once);
        mockIndexBuilder.Verify(x => x.IsDescending(true), Times.Once); // Descending
        mockIndexBuilder.Verify(x => x.HasAnnotation("Relational:Collation", "NOCASE"), Times.Once);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForRangeQueries", true), Times.Once);
    }

    #endregion

    #region Expression Validation Tests

    [Theory]
    [InlineData(true)] // e => e.IsActive
    [InlineData(false)] // Static boolean expressions would be tested differently
    public void HasSqlitePartialIndex_WithBooleanExpressions_ShouldWork(bool expectedValue)
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<TestEntity>();
        
        // Different types of boolean expressions
        Expression<Func<TestEntity, bool>> filter = expectedValue 
            ? e => e.IsActive 
            : e => !e.IsActive;

        // Act
        var result = mockIndexBuilder.Object.HasSqlitePartialIndex(filter);

        // Assert
        Assert.NotNull(result);
        mockIndexBuilder.Verify(x => x.HasFilter(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void HasSqliteCoveringIndex_WithSingleProperty_ShouldWork()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();
        Expression<Func<Order, object>>[] singleProp = { o => o.OrderDate };

        // Act
        var result = mockIndexBuilder.Object.HasSqliteCoveringIndex(singleProp);

        // Assert
        Assert.NotNull(result);
        mockIndexBuilder.Verify(x => x.IncludeProperties(It.IsAny<string[]>()), Times.Once);
    }

    #endregion

    #region Performance Optimization Tests

    [Fact]
    public void OptimizeForSqliteRangeQueries_WithAllOptimizations_ShouldSetAllAnnotations()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();
        const string collation = "BINARY";

        // Act
        mockIndexBuilder.Object.OptimizeForSqliteRangeQueries<Order>(
            ascending: true, 
            collation: collation);

        // Assert - Verify all performance annotations
        mockIndexBuilder.Verify(x => x.IsDescending(false), Times.Once);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Relational:Collation", collation), Times.Once);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedForRangeQueries", true), Times.Once);
        mockIndexBuilder.Verify(x => x.HasAnnotation("Sqlite:IndexType", "RANGE_OPTIMIZED"), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteRangeQueries_WithoutCollation_ShouldNotSetCollationAnnotation()
    {
        // Arrange
        var mockIndexBuilder = CreateMockIndexBuilder<Order>();

        // Act
        mockIndexBuilder.Object.OptimizeForSqliteRangeQueries<Order>();

        // Assert
        mockIndexBuilder.Verify(x => x.HasAnnotation("Relational:Collation", It.IsAny<string>()), Times.Never);
    }

    #endregion
}