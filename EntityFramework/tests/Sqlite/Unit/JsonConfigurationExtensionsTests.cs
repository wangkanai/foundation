// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class JsonConfigurationExtensionsTests
{
    #region Test Entity Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public UserSettings Settings { get; set; } = new();
        public string Configuration { get; set; } = "{}";
        public string UserEmail { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    public class UserSettings
    {
        public string Theme { get; set; } = "light";
        public bool Notifications { get; set; } = true;
        public UserProfile User { get; set; } = new();
    }

    public class UserProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Id { get; set; }
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<PropertyBuilder<T>> CreateMockPropertyBuilder<T>()
    {
        var mockPropertyBuilder = new Mock<PropertyBuilder<T>>();
        
        // Setup fluent interface methods
        mockPropertyBuilder.Setup(x => x.HasColumnType(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);
        
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);
                          
        mockPropertyBuilder.Setup(x => x.HasConversion(It.IsAny<Func<T, string>>(), It.IsAny<Func<string, T>>()))
                          .Returns(mockPropertyBuilder.Object);

        return mockPropertyBuilder;
    }

    private static Mock<EntityTypeBuilder<T>> CreateMockEntityTypeBuilder<T>() where T : class
    {
        var mockEntityBuilder = new Mock<EntityTypeBuilder<T>>();
        var mockIndexBuilder = new Mock<IndexBuilder>();
        
        // Setup index creation
        mockEntityBuilder.Setup(x => x.HasIndex(It.IsAny<string>()))
                        .Returns(mockIndexBuilder.Object);
        
        mockIndexBuilder.Setup(x => x.HasDatabaseName(It.IsAny<string>()))
                       .Returns(mockIndexBuilder.Object);
                       
        mockIndexBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                       .Returns(mockIndexBuilder.Object);
                       
        mockIndexBuilder.Setup(x => x.IsUnique())
                       .Returns(mockIndexBuilder.Object);

        return mockEntityBuilder;
    }

    #endregion

    #region HasSqliteJsonColumn Tests

    [Fact]
    public void HasSqliteJsonColumn_WithValidPropertyBuilder_ShouldConfigureJsonColumn()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<UserSettings>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteJsonColumn<UserSettings>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify JSON column configuration
        mockPropertyBuilder.Verify(x => x.HasColumnType("JSON"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:JsonColumn", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:JsonCompression", false), Times.Once);
    }

    [Fact]
    public void HasSqliteJsonColumn_WithCompressionEnabled_ShouldSetCompressionAnnotation()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<UserSettings>();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteJsonColumn<UserSettings>(compressionEnabled: true);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:JsonCompression", true), Times.Once);
    }

    [Fact]
    public void HasSqliteJsonColumn_WithNullPropertyBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        PropertyBuilder<UserSettings> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBuilder.HasSqliteJsonColumn<UserSettings>());
    }

    [Fact]
    public void HasSqliteJsonColumn_ShouldConfigureJsonSerialization()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<UserSettings>();

        // Act
        mockPropertyBuilder.Object.HasSqliteJsonColumn<UserSettings>();

        // Assert
        mockPropertyBuilder.Verify(x => x.HasConversion(
            It.IsAny<Func<UserSettings, string>>(),
            It.IsAny<Func<string, UserSettings>>()), Times.Once);
    }

    #endregion

    #region HasSqliteJsonPath Tests

    [Fact]
    public void HasSqliteJsonPath_WithValidParameters_ShouldCreateJsonPathIndex()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        const string indexName = "IX_Test_UserEmail";
        const string propertyName = "Settings";
        const string jsonPath = "$.user.email";

        // Act
        var result = mockEntityBuilder.Object.HasSqliteJsonPath(indexName, propertyName, jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify index creation with proper expression
        var expectedExpression = $"json_extract(\"{propertyName}\", '{jsonPath}')";
        mockEntityBuilder.Verify(x => x.HasIndex(expectedExpression), Times.Once);
    }

    [Fact]
    public void HasSqliteJsonPath_WithUniqueIndex_ShouldSetUniqueConstraint()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var mockIndexBuilder = new Mock<IndexBuilder>();
        
        mockEntityBuilder.Setup(x => x.HasIndex(It.IsAny<string>()))
                        .Returns(mockIndexBuilder.Object);
        mockIndexBuilder.Setup(x => x.HasDatabaseName(It.IsAny<string>()))
                       .Returns(mockIndexBuilder.Object);
        mockIndexBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                       .Returns(mockIndexBuilder.Object);
        mockIndexBuilder.Setup(x => x.IsUnique())
                       .Returns(mockIndexBuilder.Object);

        // Act
        mockEntityBuilder.Object.HasSqliteJsonPath("IX_Unique", "Settings", "$.id", isUnique: true);

        // Assert
        mockIndexBuilder.Verify(x => x.IsUnique(), Times.Once);
    }

    [Theory]
    [InlineData(null, "property", "$.path")]
    [InlineData("", "property", "$.path")]
    [InlineData("  ", "property", "$.path")]
    public void HasSqliteJsonPath_WithInvalidIndexName_ShouldThrowArgumentException(string? indexName, string propertyName, string jsonPath)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonPath(indexName!, propertyName, jsonPath));
    }

    [Theory]
    [InlineData("index", null, "$.path")]
    [InlineData("index", "", "$.path")]
    [InlineData("index", "  ", "$.path")]
    public void HasSqliteJsonPath_WithInvalidPropertyName_ShouldThrowArgumentException(string indexName, string? propertyName, string jsonPath)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonPath(indexName, propertyName!, jsonPath));
    }

    [Theory]
    [InlineData("index", "property", null)]
    [InlineData("index", "property", "")]
    [InlineData("index", "property", "  ")]
    public void HasSqliteJsonPath_WithInvalidJsonPath_ShouldThrowArgumentException(string indexName, string propertyName, string? jsonPath)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonPath(indexName, propertyName, jsonPath!));
    }

    [Fact]
    public void HasSqliteJsonPath_WithNullEntityBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        EntityTypeBuilder<TestEntity> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.HasSqliteJsonPath("index", "property", "$.path"));
    }

    #endregion

    #region HasSqliteJsonExtraction Tests

    [Fact]
    public void HasSqliteJsonExtraction_WithValidParameters_ShouldCreateComputedColumn()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();
        
        // Setup Property method to return our mock property builder
        mockEntityBuilder.Setup(x => x.Property(It.IsAny<Expression<Func<TestEntity, string>>>()))
                        .Returns(mockPropertyBuilder.Object);
        
        // Setup computed column configuration
        mockPropertyBuilder.Setup(x => x.HasComputedColumnSql(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);
        mockPropertyBuilder.Setup(x => x.HasColumnType(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);

        Expression<Func<TestEntity, string>> expression = e => e.UserEmail;
        const string jsonProperty = "Settings";
        const string jsonPath = "$.user.email";
        const string sqliteType = "TEXT";

        // Act
        var result = mockEntityBuilder.Object.HasSqliteJsonExtraction(expression, jsonProperty, jsonPath, sqliteType);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify computed column configuration
        var expectedSql = $"json_extract(\"{jsonProperty}\", '{jsonPath}')";
        mockPropertyBuilder.Verify(x => x.HasComputedColumnSql(expectedSql), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasColumnType(sqliteType), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:JsonExtraction", true), Times.Once);
    }

    [Theory]
    [InlineData("TEXT")]
    [InlineData("INTEGER")]
    [InlineData("REAL")]
    [InlineData("BLOB")]
    [InlineData("NUMERIC")]
    public void HasSqliteJsonExtraction_WithValidSqliteTypes_ShouldSucceed(string sqliteType)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var mockPropertyBuilder = CreateMockPropertyBuilder<string>();
        
        mockEntityBuilder.Setup(x => x.Property(It.IsAny<Expression<Func<TestEntity, string>>>()))
                        .Returns(mockPropertyBuilder.Object);
        mockPropertyBuilder.Setup(x => x.HasComputedColumnSql(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);
        mockPropertyBuilder.Setup(x => x.HasColumnType(It.IsAny<string>()))
                          .Returns(mockPropertyBuilder.Object);

        Expression<Func<TestEntity, string>> expression = e => e.UserEmail;

        // Act & Assert - Should not throw
        var result = mockEntityBuilder.Object.HasSqliteJsonExtraction(expression, "Settings", "$.path", sqliteType);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("VARCHAR")]
    [InlineData("DATETIME")]
    [InlineData("")]
    public void HasSqliteJsonExtraction_WithInvalidSqliteType_ShouldThrowArgumentException(string sqliteType)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        Expression<Func<TestEntity, string>> expression = e => e.UserEmail;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonExtraction(expression, "Settings", "$.path", sqliteType));
    }

    #endregion

    #region HasSqliteJsonPaths Tests

    [Fact]
    public void HasSqliteJsonPaths_WithValidConfigurations_ShouldCreateMultipleIndexes()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var mockIndexBuilder = new Mock<IndexBuilder>();
        
        mockEntityBuilder.Setup(x => x.HasIndex(It.IsAny<string>()))
                        .Returns(mockIndexBuilder.Object);
        mockIndexBuilder.Setup(x => x.HasDatabaseName(It.IsAny<string>()))
                       .Returns(mockIndexBuilder.Object);
        mockIndexBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                       .Returns(mockIndexBuilder.Object);

        var pathConfigurations = new Dictionary<string, string>
        {
            { "IX_User_Email", "$.user.email" },
            { "IX_User_Name", "$.user.name" },
            { "IX_Settings_Theme", "$.settings.theme" }
        };

        // Act
        var result = mockEntityBuilder.Object.HasSqliteJsonPaths("UserData", pathConfigurations);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify that HasIndex was called for each path configuration
        mockEntityBuilder.Verify(x => x.HasIndex(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public void HasSqliteJsonPaths_WithEmptyConfigurations_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var emptyConfigurations = new Dictionary<string, string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonPaths("Property", emptyConfigurations));
    }

    [Fact]
    public void HasSqliteJsonPaths_WithNullConfigurations_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonPaths("Property", null!));
    }

    #endregion

    #region HasSqliteJsonFullTextSearch Tests

    [Fact]
    public void HasSqliteJsonFullTextSearch_WithValidParameters_ShouldConfigureFTS()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        const string ftsTableName = "UserSearch";
        const string jsonProperty = "Profile";
        var searchPaths = new[] { "$.name", "$.bio", "$.skills[*]" };

        // Act
        var result = mockEntityBuilder.Object.HasSqliteJsonFullTextSearch(ftsTableName, jsonProperty, searchPaths);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify FTS annotations
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsTable", ftsTableName), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsJsonProperty", jsonProperty), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:FtsSearchPaths", searchPaths), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void HasSqliteJsonFullTextSearch_WithInvalidFtsTableName_ShouldThrowArgumentException(string? ftsTableName)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var searchPaths = new[] { "$.name" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonFullTextSearch(ftsTableName!, "property", searchPaths));
    }

    [Fact]
    public void HasSqliteJsonFullTextSearch_WithEmptySearchPaths_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var emptySearchPaths = Array.Empty<string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.HasSqliteJsonFullTextSearch("FtsTable", "property", emptySearchPaths));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void JsonExtensions_MethodChaining_ShouldWork()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();
        var mockPropertyBuilder = CreateMockPropertyBuilder<UserSettings>();
        
        mockEntityBuilder.Setup(x => x.Property(It.IsAny<Expression<Func<TestEntity, UserSettings>>>()))
                        .Returns(mockPropertyBuilder.Object);

        // Act & Assert - Method chaining should work without throwing
        var result = mockEntityBuilder.Object
            .HasSqliteJsonPath("IX_Settings", "Settings", "$.theme");
        
        Assert.NotNull(result);
    }

    [Fact]
    public void JsonColumnConfiguration_WithAllOptions_ShouldApplyAllSettings()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder<UserSettings>();

        // Act
        mockPropertyBuilder.Object.HasSqliteJsonColumn<UserSettings>(compressionEnabled: true);

        // Assert - Verify all expected configurations
        mockPropertyBuilder.Verify(x => x.HasColumnType("JSON"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:JsonColumn", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:JsonCompression", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasConversion(
            It.IsAny<Func<UserSettings, string>>(),
            It.IsAny<Func<string, UserSettings>>()), Times.Once);
    }

    #endregion

    #region Common JSON Path Patterns Tests

    [Theory]
    [InlineData("$.id")]
    [InlineData("$.user.name")]
    [InlineData("$.settings.theme")]
    [InlineData("$[0].id")]
    [InlineData("$.items[*].name")]
    public void HasSqliteJsonPath_WithCommonJsonPaths_ShouldSucceed(string jsonPath)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<TestEntity>();

        // Act & Assert - Should not throw with common JSON path patterns
        var result = mockEntityBuilder.Object.HasSqliteJsonPath("IX_Test", "TestProperty", jsonPath);
        Assert.NotNull(result);
    }

    #endregion
}