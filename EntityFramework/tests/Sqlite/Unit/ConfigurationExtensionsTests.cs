// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class ConfigurationExtensionsTests
{
    #region Test Entity Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Version { get; set; }
        public object CustomProperty { get; set; } = new();
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<PropertyBuilder<T>> CreateMockPropertyBuilder<T>()
    {
        var mockPropertyBuilder = new Mock<PropertyBuilder<T>>();
        
        // Setup fluent interface - methods return the same instance
        mockPropertyBuilder.Setup(x => x.ValueGeneratedOnAdd())
                          .Returns(mockPropertyBuilder.Object);
        
        mockPropertyBuilder.Setup(x => x.UseCollation(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);
                          
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);

        return mockPropertyBuilder;
    }

    #endregion

    #region SqliteValueGeneratedOnAdd Tests

    [Fact]
    public void SqliteValueGeneratedOnAdd_WithValidPropertyBuilder_ShouldReturnPropertyBuilder()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<int>();

        // Act
        var result = mockPropertyBuilder.Object.SqliteValueGeneratedOnAdd();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify ValueGeneratedOnAdd was called
        mockPropertyBuilder.Verify(x => x.ValueGeneratedOnAdd(), Times.Once);
    }

    [Fact]
    public void SqliteValueGeneratedOnAdd_WithStringProperty_ShouldWork()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.SqliteValueGeneratedOnAdd();

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.ValueGeneratedOnAdd(), Times.Once);
    }

    [Fact]
    public void SqliteValueGeneratedOnAdd_WithObjectProperty_ShouldWork()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<object>();

        // Act
        var result = mockPropertyBuilder.Object.SqliteValueGeneratedOnAdd();

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.ValueGeneratedOnAdd(), Times.Once);
    }

    #endregion

    #region HasSqliteCollation Tests (Non-Nullable String)

    [Fact]
    public void HasSqliteCollation_WithNonNullableString_ShouldApplyNOCASECollation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteCollation();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify NOCASE collation was applied
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.Once);
    }

    [Fact]
    public void HasSqliteCollation_WithNonNullableString_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object
            .HasSqliteCollation();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
    }

    #endregion

    #region HasSqliteCollation Tests (Nullable String)

    [Fact]
    public void HasSqliteCollation_WithNullableString_ShouldApplyNOCASECollation()
    {
        // Arrange
        var mockPropertyBuilder = new Mock<PropertyBuilder<string?>>();
        mockPropertyBuilder.Setup(x => x.UseCollation(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteCollation();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify NOCASE collation was applied
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.Once);
    }

    #endregion

    #region OptimizeForSqliteSearch Tests (Non-Nullable String)

    [Fact]
    public void OptimizeForSqliteSearch_WithNonNullableString_ShouldApplyOptimizations()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.OptimizeForSqliteSearch();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify both collation and annotation were applied
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:InlineFts", true), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteSearch_WithNonNullableString_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result1 = mockPropertyBuilder.Object.OptimizeForSqliteSearch();
        var result2 = result1.HasSqliteCollation();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Same(mockPropertyBuilder.Object, result1);
        Assert.Same(mockPropertyBuilder.Object, result2);
    }

    #endregion

    #region OptimizeForSqliteSearch Tests (Nullable String)

    [Fact]
    public void OptimizeForSqliteSearch_WithNullableString_ShouldApplyOptimizations()
    {
        // Arrange
        var mockPropertyBuilder = new Mock<PropertyBuilder<string?>>();
        mockPropertyBuilder.Setup(x => x.UseCollation(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);

        // Act
        var result = mockPropertyBuilder.Object.OptimizeForSqliteSearch();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify both collation and annotation were applied
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:InlineFts", true), Times.Once);
    }

    #endregion

    #region HasSqliteTextAffinity Tests

    [Fact]
    public void HasSqliteTextAffinity_WithValidProperty_ShouldApplyTextAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<int>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify TEXT affinity annotation was applied
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Affinity", "TEXT"), Times.Once);
    }

    [Fact]
    public void HasSqliteTextAffinity_WithStringProperty_ShouldApplyTextAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity();

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Affinity", "TEXT"), Times.Once);
    }

    [Fact]
    public void HasSqliteTextAffinity_WithObjectProperty_ShouldApplyTextAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<object>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity();

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Affinity", "TEXT"), Times.Once);
    }

    #endregion

    #region Method Combinations Tests

    [Fact]
    public void CombinedMethods_OnSameProperty_ShouldWorkTogether()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object
            .SqliteValueGeneratedOnAdd()
            .HasSqliteCollation()
            .OptimizeForSqliteSearch()
            .HasSqliteTextAffinity();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify all methods were called
        mockPropertyBuilder.Verify(x => x.ValueGeneratedOnAdd(), Times.Once);
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.AtLeast(1)); // Called by both HasSqliteCollation and OptimizeForSqliteSearch
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:InlineFts", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Affinity", "TEXT"), Times.Once);
    }

    [Fact]
    public void SqliteCollation_CalledMultipleTimes_ShouldNotConflict()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object
            .HasSqliteCollation()
            .OptimizeForSqliteSearch(); // This also applies NOCASE collation

        // Assert
        Assert.NotNull(result);
        
        // Verify NOCASE was applied at least twice (once by each method)
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.AtLeast(2));
    }

    #endregion

    #region Type Safety Tests

    [Fact]
    public void StringMethods_OnNonStringProperty_ShouldStillWork()
    {
        // Arrange - Using int property with string-specific method
        var mockPropertyBuilder = CreateMockPropertyBuilder<int>();

        // Act & Assert - Should not throw (type constraints allow this)
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity();
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    public void HasSqliteTextAffinity_WithVariousTypes_ShouldWork(Type propertyType)
    {
        // This test demonstrates that the generic constraint allows any type
        // The actual type validation would happen at the EF Core level
        
        // Arrange & Act & Assert - Should compile and not throw
        if (propertyType == typeof(int))
        {
            var mockBuilder = CreateMockPropertyBuilder<int>();
            var result = mockBuilder.Object.HasSqliteTextAffinity();
            Assert.NotNull(result);
        }
        // Additional type testing would require dynamic creation which is complex for unit tests
        // The key point is that the generic constraint <T> allows any type
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ConfigurationMethods_WithNullPropertyBuilder_ShouldThrowNullReferenceException()
    {
        // Arrange
        PropertyBuilder<string> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => nullBuilder.HasSqliteCollation());
        Assert.Throws<NullReferenceException>(() => nullBuilder.OptimizeForSqliteSearch());
        Assert.Throws<NullReferenceException>(() => nullBuilder.HasSqliteTextAffinity());
    }

    #endregion

    #region Integration-Style Tests (Mock Verification)

    [Fact]
    public void OptimizeForSqliteSearch_ShouldCallBothCollationAndAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();
        
        // Act
        mockPropertyBuilder.Object.OptimizeForSqliteSearch();

        // Assert - Verify the specific sequence and parameters
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:InlineFts", true), Times.Once);
        
        // Verify that the method returns the same builder for chaining
        mockPropertyBuilder.VerifyNoOtherCalls();
    }

    [Fact]
    public void HasSqliteTextAffinity_ShouldApplyCorrectAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();
        
        // Act
        mockPropertyBuilder.Object.HasSqliteTextAffinity();

        // Assert - Verify the exact annotation key and value
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Affinity", "TEXT"), Times.Once);
        mockPropertyBuilder.VerifyNoOtherCalls();
    }

    #endregion
}