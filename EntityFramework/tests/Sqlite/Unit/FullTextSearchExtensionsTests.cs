// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class FullTextSearchExtensionsTests
{
    #region Test Entity Classes

    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }

    public class Document
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<PropertyBuilder<string>> CreateMockPropertyBuilder()
    {
        var mockPropertyBuilder = new Mock<PropertyBuilder<string>>();
        
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);

        return mockPropertyBuilder;
    }

    private static Mock<EntityTypeBuilder<T>> CreateMockEntityTypeBuilder<T>() where T : class
    {
        var mockEntityBuilder = new Mock<EntityTypeBuilder<T>>();
        
        mockEntityBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                        .Returns(mockEntityBuilder.Object);

        return mockEntityBuilder;
    }

    #endregion

    #region HasSqliteFullTextSearch Tests

    [Fact]
    public void HasSqliteFullTextSearch_WithDefaultFtsVersion_ShouldConfigureFTS5()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFullTextSearch<Article>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify FTS configuration with default FTS5
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts5"), Times.Once);
    }

    [Theory]
    [InlineData(FtsVersion.FTS3, "fts3")]
    [InlineData(FtsVersion.FTS4, "fts4")]
    [InlineData(FtsVersion.FTS5, "fts5")]
    public void HasSqliteFullTextSearch_WithSpecificFtsVersion_ShouldConfigureCorrectVersion(FtsVersion version, string expectedVersionString)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFullTextSearch<Article>(version);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", expectedVersionString), Times.Once);
    }

    [Fact]
    public void HasSqliteFullTextSearch_WithNullPropertyBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        PropertyBuilder<string> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.HasSqliteFullTextSearch<Article>());
    }

    #endregion

    #region HasSqliteFullTextIndex Tests

    [Fact]
    public void HasSqliteFullTextIndex_WithValidParameters_ShouldCreateMultiColumnIndex()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        const string indexName = "ProductSearchIndex";
        Expression<Func<Product, string>>[] textProps = 
        {
            p => p.Name,
            p => p.Description,
            p => p.Category
        };

        // Act
        var result = mockEntityBuilder.Object.HasSqliteFullTextIndex(indexName, textProps);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify FTS index configuration
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextIndex", indexName), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts5"), Times.Once);
        
        // Verify property names are extracted and stored
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsColumns", 
            It.Is<string[]>(cols => cols.Contains("Name") && cols.Contains("Description") && cols.Contains("Category"))), Times.Once);
    }

    [Fact]
    public void HasSqliteFullTextIndex_WithCustomFtsVersion_ShouldUseSpecifiedVersion()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        const string indexName = "ProductIndex";
        Expression<Func<Product, string>>[] textProps = { p => p.Name };

        // Act
        var result = mockEntityBuilder.Object.HasSqliteFullTextIndex(indexName, textProps, FtsVersion.FTS4);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts4"), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasSqliteFullTextIndex_WithInvalidIndexName_ShouldThrowArgumentException(string? indexName)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        Expression<Func<Product, string>>[] textProps = { p => p.Name };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteFullTextIndex(indexName!, textProps));
    }

    [Fact]
    public void HasSqliteFullTextIndex_WithEmptyTextProperties_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        const string indexName = "TestIndex";
        var emptyProps = Array.Empty<Expression<Func<Product, string>>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteFullTextIndex(indexName, emptyProps));
    }

    [Fact]
    public void HasSqliteFullTextIndex_WithNullTextProperties_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        const string indexName = "TestIndex";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            mockEntityBuilder.Object.HasSqliteFullTextIndex(indexName, null!));
    }

    #endregion

    #region HasSqliteFtsTokenizer Tests

    [Fact]
    public void HasSqliteFtsTokenizer_WithValidTokenizer_ShouldSetTokenizer()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string tokenizerName = "porter";

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFtsTokenizer<Article>(tokenizerName);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", tokenizerName), Times.Once);
    }

    [Fact]
    public void HasSqliteFtsTokenizer_WithTokenizerOptions_ShouldSetBothTokenizerAndOptions()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string tokenizerName = "unicode61";
        const string tokenizerOptions = "casefold=1";

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFtsTokenizer<Article>(tokenizerName, tokenizerOptions);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", tokenizerName), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizerOptions", tokenizerOptions), Times.Once);
    }

    [Fact]
    public void HasSqliteFtsTokenizer_WithEmptyOptions_ShouldNotSetOptionsAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string tokenizerName = "ascii";

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFtsTokenizer<Article>(tokenizerName, string.Empty);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", tokenizerName), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizerOptions", It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasSqliteFtsTokenizer_WithInvalidTokenizerName_ShouldThrowArgumentException(string? tokenizerName)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockPropertyBuilder.Object.HasSqliteFtsTokenizer<Article>(tokenizerName!));
    }

    [Fact]
    public void HasSqliteFtsTokenizer_WithNullPropertyBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        PropertyBuilder<string> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.HasSqliteFtsTokenizer<Article>("porter"));
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void FullTextSearchMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object
            .HasSqliteFullTextSearch<Article>(FtsVersion.FTS5)
            .HasSqliteFtsTokenizer<Article>("porter", "stemmer=1");

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify both methods were called
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts5"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", "porter"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizerOptions", "stemmer=1"), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CombinedFullTextSearchConfiguration_ShouldApplyAllSettings()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Article>();
        Expression<Func<Article, string>>[] searchProps = 
        {
            a => a.Title,
            a => a.Content,
            a => a.Summary
        };

        // Act - Configure both property-level and entity-level FTS
        mockPropertyBuilder.Object
            .HasSqliteFullTextSearch<Article>(FtsVersion.FTS5)
            .HasSqliteFtsTokenizer<Article>("unicode61", "casefold=1 remove_diacritics=1");

        mockEntityBuilder.Object
            .HasSqliteFullTextIndex("ArticleSearchIndex", searchProps, FtsVersion.FTS5);

        // Assert - Verify comprehensive FTS configuration
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextSearch", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts5"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", "unicode61"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizerOptions", "casefold=1 remove_diacritics=1"), Times.Once);

        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FullTextIndex", "ArticleSearchIndex"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts5"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsColumns", 
            It.Is<string[]>(cols => cols.Length == 3 && cols.Contains("Title") && cols.Contains("Content") && cols.Contains("Summary"))), Times.Once);
    }

    #endregion

    #region Tokenizer Configuration Tests

    [Theory]
    [InlineData("porter")]
    [InlineData("unicode61")]
    [InlineData("ascii")]
    [InlineData("simple")]
    public void HasSqliteFtsTokenizer_WithCommonTokenizers_ShouldSucceed(string tokenizerName)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFtsTokenizer<Article>(tokenizerName);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizer", tokenizerName), Times.Once);
    }

    [Theory]
    [InlineData("casefold=1")]
    [InlineData("remove_diacritics=1")]
    [InlineData("categories='L* N* Co'")]
    [InlineData("casefold=1 remove_diacritics=1")]
    public void HasSqliteFtsTokenizer_WithCommonOptions_ShouldSetOptions(string tokenizerOptions)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFtsTokenizer<Article>("unicode61", tokenizerOptions);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTokenizerOptions", tokenizerOptions), Times.Once);
    }

    #endregion

    #region FTS Version Tests

    [Fact]
    public void FtsVersionEnum_ShouldHaveCorrectValues()
    {
        // Verify the enum values are correctly defined
        Assert.Equal(0, (int)FtsVersion.FTS3);
        Assert.Equal(1, (int)FtsVersion.FTS4);
        Assert.Equal(2, (int)FtsVersion.FTS5);
    }

    [Fact]
    public void HasSqliteFullTextSearch_WithInvalidFtsVersionCast_ShouldDefaultToFTS5()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        var invalidFtsVersion = (FtsVersion)999; // Invalid enum value

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteFullTextSearch<Article>(invalidFtsVersion);

        // Assert
        Assert.NotNull(result);
        // Should default to FTS5 for invalid enum values
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsVersion", "fts5"), Times.Once);
    }

    #endregion

    #region Property Name Extraction Tests

    [Fact]
    public void HasSqliteFullTextIndex_WithComplexPropertyExpressions_ShouldExtractCorrectNames()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        Expression<Func<Product, string>>[] complexProps = 
        {
            p => p.Name,           // Simple property
            p => p.Description,    // Another simple property
            p => p.Tags            // Yet another simple property
        };

        // Act
        var result = mockEntityBuilder.Object.HasSqliteFullTextIndex("ComplexIndex", complexProps);

        // Assert
        Assert.NotNull(result);
        
        // Verify that all property names are correctly extracted
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsColumns", 
            It.Is<string[]>(cols => 
                cols.Length == 3 && 
                cols.Contains("Name") && 
                cols.Contains("Description") && 
                cols.Contains("Tags"))), Times.Once);
    }

    #endregion
}