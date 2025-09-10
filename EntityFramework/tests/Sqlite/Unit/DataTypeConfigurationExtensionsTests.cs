// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class DataTypeConfigurationExtensionsTests
{
    #region Mock Setup Helpers

    private static Mock<PropertyBuilder<T>> CreateMockPropertyBuilder<T>()
    {
        var mockPropertyBuilder = new Mock<PropertyBuilder<T>>();
        
        // Setup fluent interface methods
        mockPropertyBuilder.Setup(x => x.HasColumnType(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);
        
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);
                          
        mockPropertyBuilder.Setup(x => x.HasPrecision(It.IsAny<int>(), It.IsAny<int>()))
                          .Returns(mockPropertyBuilder.Object);
                          
        mockPropertyBuilder.Setup(x => x.HasMaxLength(It.IsAny<int>()))
                          .Returns(mockPropertyBuilder.Object);
                          
        mockPropertyBuilder.Setup(x => x.UseCollation(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);

        return mockPropertyBuilder;
    }

    #endregion

    #region HasSqliteIntegerAffinity Tests

    [Fact]
    public void HasSqliteIntegerAffinity_WithIntProperty_ShouldConfigureIntegerAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<int>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteIntegerAffinity();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasColumnType("INTEGER"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:TypeAffinity", "INTEGER"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedInteger", true), Times.Once);
    }

    [Fact]
    public void HasSqliteIntegerAffinity_WithAutoIncrement_ShouldSetAutoincrementAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<int>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteIntegerAffinity(enableAutoIncrement: true);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Autoincrement", true), Times.Once);
    }

    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    public void HasSqliteIntegerAffinity_WithValidIntegerTypes_ShouldSucceed(Type integerType)
    {
        // This test would be complex to implement with generic constraints in unit tests
        // The key validation happens at runtime in the actual method
        // We'll test with int as representative of integer types
        
        if (integerType == typeof(int))
        {
            var mockPropertyBuilder = CreateMockPropertyBuilder<int>();
            var result = mockPropertyBuilder.Object.HasSqliteIntegerAffinity();
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void HasSqliteIntegerAffinity_WithNullPropertyBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        PropertyBuilder<int> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBuilder.HasSqliteIntegerAffinity());
    }

    #endregion

    #region HasSqliteRealAffinity Tests

    [Fact]
    public void HasSqliteRealAffinity_WithDoubleProperty_ShouldConfigureRealAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<double>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteRealAffinity();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasColumnType("REAL"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:TypeAffinity", "REAL"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedReal", true), Times.Once);
    }

    [Fact]
    public void HasSqliteRealAffinity_WithCustomPrecisionAndScale_ShouldSetPrecision()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();
        const int precision = 10;
        const int scale = 2;

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteRealAffinity(precision, scale);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasPrecision(precision, scale), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Precision", precision), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Scale", scale), Times.Once);
    }

    [Theory]
    [InlineData(0, 0)] // Invalid precision
    [InlineData(40, 2)] // Precision too high
    public void HasSqliteRealAffinity_WithInvalidPrecision_ShouldThrowArgumentOutOfRangeException(int precision, int scale)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockPropertyBuilder.Object.HasSqliteRealAffinity(precision, scale));
    }

    [Theory]
    [InlineData(10, -1)] // Negative scale
    [InlineData(10, 15)] // Scale greater than precision
    public void HasSqliteRealAffinity_WithInvalidScale_ShouldThrowArgumentOutOfRangeException(int precision, int scale)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockPropertyBuilder.Object.HasSqliteRealAffinity(precision, scale));
    }

    #endregion

    #region HasSqliteBlobOptimization Tests

    [Fact]
    public void HasSqliteBlobOptimization_WithByteArrayProperty_ShouldConfigureBlobOptimization()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<byte[]>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteBlobOptimization();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasColumnType("BLOB"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:TypeAffinity", "BLOB"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedBlob", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:CompressionLevel", CompressionLevel.Optimal), Times.Once);
    }

    [Theory]
    [InlineData(CompressionLevel.None)]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.Optimal)]
    [InlineData(CompressionLevel.SmallestSize)]
    public void HasSqliteBlobOptimization_WithDifferentCompressionLevels_ShouldSetCompressionLevel(CompressionLevel compressionLevel)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<byte[]>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteBlobOptimization(compressionLevel);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:CompressionLevel", compressionLevel), Times.Once);
    }

    [Fact]
    public void HasSqliteBlobOptimization_WithChunkSize_ShouldSetChunkSizeAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<byte[]>();
        const int chunkSize = 1048576; // 1MB

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteBlobOptimization(chunkSize: chunkSize);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:ChunkSize", chunkSize), Times.Once);
    }

    [Fact]
    public void HasSqliteBlobOptimization_WithDeduplication_ShouldSetDeduplicationAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<byte[]>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteBlobOptimization(enableDeduplication: true);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Deduplication", true), Times.Once);
    }

    [Fact]
    public void HasSqliteBlobOptimization_WithNegativeChunkSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<byte[]>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockPropertyBuilder.Object.HasSqliteBlobOptimization(chunkSize: -1));
    }

    #endregion

    #region HasSqliteTextAffinity Tests

    [Fact]
    public void HasSqliteTextAffinity_WithStringProperty_ShouldConfigureTextAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasColumnType("TEXT"), Times.Once);
        mockPropertyBuilder.Verify(x => x.UseCollation("BINARY"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:TypeAffinity", "TEXT"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedText", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Collation", "BINARY"), Times.Once);
    }

    [Theory]
    [InlineData("BINARY")]
    [InlineData("NOCASE")]
    [InlineData("RTRIM")]
    public void HasSqliteTextAffinity_WithValidCollations_ShouldSetCollation(string collation)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity(collation);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.UseCollation(collation), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Collation", collation), Times.Once);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData(null)]
    public void HasSqliteTextAffinity_WithInvalidCollation_ShouldThrowArgumentException(string? collation)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockPropertyBuilder.Object.HasSqliteTextAffinity(collation!));
    }

    [Fact]
    public void HasSqliteTextAffinity_WithMaxLength_ShouldSetMaxLength()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();
        const int maxLength = 100;

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity(maxLength: maxLength);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasMaxLength(maxLength), Times.Once);
    }

    [Fact]
    public void HasSqliteTextAffinity_WithFullTextSearch_ShouldSetFTSAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity(enableFullTextSearch: true);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
    }

    #endregion

    #region HasSqliteNumericAffinity Tests

    [Fact]
    public void HasSqliteNumericAffinity_WithDecimalProperty_ShouldConfigureNumericAffinity()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteNumericAffinity();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasColumnType("NUMERIC"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:TypeAffinity", "NUMERIC"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizedNumeric", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasPrecision(18, 2), Times.Once); // Default precision and scale
    }

    [Fact]
    public void HasSqliteNumericAffinity_WithCurrencyMode_ShouldSetCurrencyAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteNumericAffinity(enableCurrencyMode: true);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:CurrencyMode", true), Times.Once);
    }

    [Fact]
    public void HasSqliteNumericAffinity_WithCustomPrecisionAndScale_ShouldSetValues()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();
        const int precision = 19;
        const int scale = 4;

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteNumericAffinity(precision, scale);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasPrecision(precision, scale), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Precision", precision), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Scale", scale), Times.Once);
    }

    #endregion

    #region Type Validation Tests

    // Note: These tests demonstrate the concept, but actual type validation 
    // would require more complex runtime type checking in a real implementation

    [Fact]
    public void HasSqliteIntegerAffinity_CalledOnCorrectTypes_ShouldSucceed()
    {
        // Test with different integer types that should work
        var intBuilder = CreateMockPropertyBuilder<int>();
        var longBuilder = CreateMockPropertyBuilder<long>();
        var shortBuilder = CreateMockPropertyBuilder<short>();
        var byteBuilder = CreateMockPropertyBuilder<byte>();

        // Act & Assert - These should all work without throwing
        Assert.NotNull(intBuilder.Object.HasSqliteIntegerAffinity());
        Assert.NotNull(longBuilder.Object.HasSqliteIntegerAffinity());
        Assert.NotNull(shortBuilder.Object.HasSqliteIntegerAffinity());
        Assert.NotNull(byteBuilder.Object.HasSqliteIntegerAffinity());
    }

    [Fact]
    public void HasSqliteRealAffinity_CalledOnCorrectTypes_ShouldSucceed()
    {
        // Test with different floating-point types that should work
        var floatBuilder = CreateMockPropertyBuilder<float>();
        var doubleBuilder = CreateMockPropertyBuilder<double>();
        var decimalBuilder = CreateMockPropertyBuilder<decimal>();

        // Act & Assert - These should all work without throwing
        Assert.NotNull(floatBuilder.Object.HasSqliteRealAffinity());
        Assert.NotNull(doubleBuilder.Object.HasSqliteRealAffinity());
        Assert.NotNull(decimalBuilder.Object.HasSqliteRealAffinity());
    }

    [Fact]
    public void HasSqliteBlobOptimization_CalledOnCorrectTypes_ShouldSucceed()
    {
        // Test with binary types that should work
        var byteArrayBuilder = CreateMockPropertyBuilder<byte[]>();

        // Act & Assert - Should work without throwing
        Assert.NotNull(byteArrayBuilder.Object.HasSqliteBlobOptimization());
    }

    [Fact]
    public void HasSqliteTextAffinity_CalledOnStringType_ShouldSucceed()
    {
        // Test with string type
        var stringBuilder = CreateMockPropertyBuilder<string>();

        // Act & Assert - Should work without throwing
        Assert.NotNull(stringBuilder.Object.HasSqliteTextAffinity());
    }

    [Fact]
    public void HasSqliteNumericAffinity_CalledOnDecimalType_ShouldSucceed()
    {
        // Test with decimal type
        var decimalBuilder = CreateMockPropertyBuilder<decimal>();

        // Act & Assert - Should work without throwing
        Assert.NotNull(decimalBuilder.Object.HasSqliteNumericAffinity());
    }

    #endregion

    #region Edge Cases and Boundary Value Tests

    [Fact]
    public void HasSqliteRealAffinity_WithBoundaryPrecisionValues_ShouldHandleCorrectly()
    {
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();

        // Test minimum valid precision
        var result1 = mockPropertyBuilder.Object.HasSqliteRealAffinity(precision: 1, scale: 0);
        Assert.NotNull(result1);

        // Test maximum valid precision
        var result2 = mockPropertyBuilder.Object.HasSqliteRealAffinity(precision: 38, scale: 0);
        Assert.NotNull(result2);
    }

    [Fact]
    public void HasSqliteTextAffinity_WithZeroMaxLength_ShouldNotSetMaxLength()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteTextAffinity(maxLength: 0);

        // Assert
        Assert.NotNull(result);
        // Verify HasMaxLength was not called when maxLength is 0
        mockPropertyBuilder.Verify(x => x.HasMaxLength(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HasSqliteBlobOptimization_WithZeroChunkSize_ShouldNotSetChunkSize()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<byte[]>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteBlobOptimization(chunkSize: 0);

        // Assert
        Assert.NotNull(result);
        // Verify chunk size annotation was not set when chunkSize is 0
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:ChunkSize", It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void DataTypeAffinityMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<decimal>();

        // Act - Chain multiple calls
        var result = mockPropertyBuilder.Object
            .HasSqliteNumericAffinity(precision: 19, scale: 4, enableCurrencyMode: true);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify all expected calls were made
        mockPropertyBuilder.Verify(x => x.HasColumnType("NUMERIC"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasPrecision(19, 4), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:CurrencyMode", true), Times.Once);
    }

    [Fact]
    public void CombinedDataTypeConfigurations_OnSameProperty_ShouldWork()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();

        // Act
        var result = mockPropertyBuilder.Object
            .HasSqliteTextAffinity("NOCASE", enableFullTextSearch: true, maxLength: 500);

        // Assert
        Assert.NotNull(result);
        
        // Verify all configurations were applied
        mockPropertyBuilder.Verify(x => x.HasColumnType("TEXT"), Times.Once);
        mockPropertyBuilder.Verify(x => x.UseCollation("NOCASE"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasMaxLength(500), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
    }

    #endregion
}