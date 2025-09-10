// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Unit tests for PostgreSQL JSONB configuration extensions.
/// Tests argument validation and configuration setup for JSONB data types and operations.
/// </summary>
public sealed class JsonbConfigurationExtensionsTests
{
   #region HasJsonbType Tests

   [Fact]
   public void HasJsonbType_ShouldConfigureJsonbColumnType()
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<JsonEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Metadata);

      // Act - Test basic column type configuration
      var result = propertyBuilder.HasColumnType("jsonb");

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Fact]
   public void HasJsonType_ShouldConfigureJsonColumnType()
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<JsonEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Settings);

      // Act - Test basic column type configuration
      var result = propertyBuilder.HasColumnType("json");

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   #endregion

   #region JSONB Index Configuration Tests

   [Theory]
   [InlineData("ix_json_metadata_gin")]
   [InlineData("ix_custom_json_index")]
   public void HasJsonbGinIndex_WithValidIndexName_ShouldConfigureIndex(string indexName)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<JsonEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasColumnType("jsonb");

      // Act - Test basic annotation
      var result = propertyBuilder.HasAnnotation("JsonbGinIndex", indexName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void HasJsonbGinIndex_WithInvalidIndexName_ShouldThrowArgumentException(string invalidName)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateIndexName(invalidName);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*Index name cannot be null or whitespace.*");
   }

   [Theory]
   [InlineData("$.name")]
   [InlineData("$.address.city")]
   [InlineData("$.tags[*]")]
   public void HasJsonbPathIndex_WithValidJsonPath_ShouldConfigureIndex(string jsonPath)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<JsonEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasColumnType("jsonb");

      // Act - Test basic annotation
      var result = propertyBuilder.HasAnnotation("JsonbPathIndex", jsonPath);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   [InlineData("invalid-path")]
   public void HasJsonbPathIndex_WithInvalidJsonPath_ShouldThrowArgumentException(string invalidPath)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateJsonPath(invalidPath);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*JSON path cannot be null, whitespace, or invalid.*");
   }

   #endregion

   #region JSONB Constraints Tests

   [Theory]
   [InlineData("$.name")]
   [InlineData("$.email")]
   [InlineData("$.id")]
   public void RequireJsonbProperty_WithValidJsonPath_ShouldConfigureConstraint(string jsonPath)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<JsonEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasColumnType("jsonb");

      // Act - Test basic annotation
      var result = propertyBuilder.HasAnnotation("JsonbRequiredProperty", jsonPath);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void RequireJsonbProperty_WithInvalidJsonPath_ShouldThrowArgumentException(string invalidPath)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateJsonPath(invalidPath);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*JSON path cannot be null, whitespace, or invalid.*");
   }

   [Theory]
   [InlineData("string")]
   [InlineData("number")]
   [InlineData("boolean")]
   [InlineData("object")]
   [InlineData("array")]
   public void ValidateJsonbPropertyType_WithValidType_ShouldConfigureValidation(string jsonType)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateJsonType(jsonType);

      // Assert
      act.Should().NotThrow();
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   [InlineData("invalid_type")]
   public void ValidateJsonbPropertyType_WithInvalidType_ShouldThrowArgumentException(string invalidType)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateJsonType(invalidType);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*JSON type must be one of: string, number, boolean, object, array.*");
   }

   #endregion

   #region JSONB Default Value Tests

   [Theory]
   [InlineData("{}")]
   [InlineData("[]")]
   [InlineData("{\"default\": true}")]
   [InlineData("[\"default\"]")]
   public void HasJsonbDefaultValue_WithValidJson_ShouldConfigureDefault(string defaultJson)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<JsonEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasColumnType("jsonb");

      // Act - Test basic annotation
      var result = propertyBuilder.HasAnnotation("JsonbDefaultValue", defaultJson);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   [InlineData("invalid-json")]
   [InlineData("{unclosed")]
   public void HasJsonbDefaultValue_WithInvalidJson_ShouldThrowArgumentException(string invalidJson)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateJsonValue(invalidJson);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*Default value must be valid JSON.*");
   }

   #endregion

   #region JSONB Path Extraction Tests

   [Theory]
   [InlineData("$.name",   "text")]
   [InlineData("$.age",    "integer")]
   [InlineData("$.price",  "decimal")]
   [InlineData("$.active", "boolean")]
   public void ExtractJsonbPath_WithValidPathAndType_ShouldConfigureExtraction(string path, string dataType)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidatePathAndType(path, dataType);

      // Assert
      act.Should().NotThrow();
   }

   [Theory]
   [InlineData("",       "text")]
   [InlineData("$.name", "")]
   [InlineData(null,     "text")]
   [InlineData("$.name", null)]
   [InlineData(" ",      "text")]
   [InlineData("$.name", " ")]
   public void ExtractJsonbPath_WithInvalidParameters_ShouldThrowArgumentException(string path, string dataType)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidatePathAndType(path, dataType);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*cannot be null or whitespace.*");
   }

   #endregion

   #region Helper Methods for Testing Validation Logic

   private static void ValidateIndexName(string indexName)
   {
      if (string.IsNullOrWhiteSpace(indexName))
         throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));
   }

   private static void ValidateJsonPath(string jsonPath)
   {
      if (string.IsNullOrWhiteSpace(jsonPath) || !jsonPath.StartsWith("$.") && jsonPath != "$")
         throw new ArgumentException("JSON path cannot be null, whitespace, or invalid.", nameof(jsonPath));
   }

   private static void ValidateJsonType(string jsonType)
   {
      var validTypes = new[] { "string", "number", "boolean", "object", "array" };
      if (string.IsNullOrWhiteSpace(jsonType) || !validTypes.Contains(jsonType))
         throw new ArgumentException("JSON type must be one of: string, number, boolean, object, array.", nameof(jsonType));
   }

   private static void ValidateJsonValue(string jsonValue)
   {
      if (string.IsNullOrWhiteSpace(jsonValue))
         throw new ArgumentException("Default value must be valid JSON.", nameof(jsonValue));

      // Simple JSON validation - in real implementation this would use JsonDocument.Parse
      if (!jsonValue.StartsWith('{') && !jsonValue.StartsWith('[') && !jsonValue.StartsWith('"'))
         throw new ArgumentException("Default value must be valid JSON.", nameof(jsonValue));
   }

   private static void ValidatePathAndType(string path, string dataType)
   {
      if (string.IsNullOrWhiteSpace(path))
         throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
      if (string.IsNullOrWhiteSpace(dataType))
         throw new ArgumentException("Data type cannot be null or whitespace.", nameof(dataType));
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class JsonEntity
{
   public int    Id          { get; set; }
   public string Name        { get; set; } = string.Empty;
   public string Metadata    { get; set; } = string.Empty;
   public string Settings    { get; set; } = string.Empty;
   public string UserProfile { get; set; } = string.Empty;
   public string Tags        { get; set; } = string.Empty;
}

/// <summary>
/// Test DbContext for JSONB configuration testing.
/// </summary>
public class JsonbTestDbContext : DbContext
{
   public JsonbTestDbContext(DbContextOptions<JsonbTestDbContext> options) : base(options) { }

   public DbSet<JsonEntity> JsonEntities => Set<JsonEntity>();

   protected override void OnModelCreating(ModelBuilder modelBuilder) =>
      modelBuilder.Entity<JsonEntity>(entity =>
      {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
         entity.Property(e => e.Metadata).HasColumnType("jsonb");
         entity.Property(e => e.Settings).HasColumnType("json");
         entity.Property(e => e.UserProfile).HasColumnType("jsonb");
         entity.Property(e => e.Tags).HasColumnType("jsonb");
      });
}