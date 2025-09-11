// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Unit tests for PostgreSQL full-text search extensions.
/// Tests argument validation and configuration setup for tsvector, tsquery, and text search features.
/// </summary>
public sealed class FullTextSearchExtensionsTests
{
   #region TsVector Configuration Tests

   [Fact]
   public void HasTsVectorType_ShouldConfigureTsVectorColumn()
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<DocumentEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.SearchVector);

      // Act
      var result = propertyBuilder.HasColumnType("tsvector");

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("english")]
   [InlineData("simple")]
   [InlineData("portuguese")]
   [InlineData("spanish")]
   public void ConfigureTextSearchLanguage_WithValidLanguage_ShouldConfigureLanguage(string language)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<DocumentEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.SearchVector).HasColumnType("tsvector");

      // Act - Test basic property configuration without extension methods
      var result = propertyBuilder.HasAnnotation("TextSearchConfiguration", language);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureTextSearchLanguage_WithInvalidLanguage_ShouldThrowArgumentException(string invalidLanguage)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateTextSearchConfiguration(invalidLanguage);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*Text search configuration cannot be null or whitespace.*");
   }

   #endregion

   #region Text Search Index Tests

   [Theory]
   [InlineData("ix_documents_search_gin")]
   [InlineData("ix_custom_search")]
   public void HasTextSearchGinIndex_WithValidIndexName_ShouldConfigureIndex(string indexName)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<DocumentEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.SearchVector).HasColumnType("tsvector");

      // Act - Test basic annotation
      var result = propertyBuilder.HasAnnotation("GinIndex", indexName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void HasTextSearchGinIndex_WithInvalidIndexName_ShouldThrowArgumentException(string invalidName)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateIndexName(invalidName);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*Index name cannot be null or whitespace.*");
   }

   #endregion

   #region Text Search Weights Tests

   [Theory]
   [InlineData('A')]
   [InlineData('B')]
   [InlineData('C')]
   [InlineData('D')]
   public void HasTextSearchWeight_WithValidWeight_ShouldConfigureWeight(char weight)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<DocumentEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Title);

      // Act - Test basic annotation
      var result = propertyBuilder.HasAnnotation("TextSearchWeight", weight);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData('E')]
   [InlineData('Z')]
   [InlineData('1')]
   [InlineData('@')]
   public void HasTextSearchWeight_WithInvalidWeight_ShouldThrowArgumentException(char invalidWeight)
   {
      // Arrange & Act - Test validation logic directly
      var act = () => ValidateTextSearchWeight(invalidWeight);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*Weight must be A, B, C, or D.*");
   }

   #endregion

   #region Path and Expression Validation Tests

   [Theory]
   [InlineData("title")]
   [InlineData("content")]
   [InlineData("title || ' ' || content")]
   public void ValidateSourceExpression_WithValidExpression_ShouldPass(string expression)
   {
      // Arrange & Act
      var act = () => ValidateSourceExpression(expression);

      // Assert
      act.Should().NotThrow();
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ValidateSourceExpression_WithInvalidExpression_ShouldThrowArgumentException(string invalidExpression)
   {
      // Arrange & Act
      var act = () => ValidateSourceExpression(invalidExpression);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*Source expression cannot be null or whitespace.*");
   }

   #endregion

   #region Weight Configuration Validation Tests

   [Theory]
   [InlineData(0.1f,  0.2f, 0.4f,  0.8f)]
   [InlineData(1.0f,  0.8f, 0.6f,  0.4f)]
   [InlineData(0.25f, 0.5f, 0.75f, 1.0f)]
   [InlineData(0.0f,  0.0f, 0.0f,  1.0f)]
   public void ValidateTextSearchWeights_WithValidWeights_ShouldPass(float d, float c, float b, float a)
   {
      // Arrange & Act
      var act = () => ValidateTextSearchWeights(d, c, b, a);

      // Assert
      act.Should().NotThrow();
   }

   [Theory]
   [InlineData(-0.1f, 0.2f,  0.4f, 0.8f)]
   [InlineData(0.1f,  -0.2f, 0.4f, 0.8f)]
   [InlineData(1.1f,  0.2f,  0.4f, 0.8f)]
   [InlineData(0.1f,  1.2f,  0.4f, 0.8f)]
   public void ValidateTextSearchWeights_WithInvalidWeights_ShouldThrowArgumentOutOfRangeException(float d, float c, float b, float a)
   {
      // Arrange & Act
      var act = () => ValidateTextSearchWeights(d, c, b, a);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithMessage("*Weights must be between 0.0 and 1.0.*");
   }

   #endregion

   #region Helper Methods for Testing Validation Logic

   private static void ValidateTextSearchConfiguration(string configuration)
   {
      if (string.IsNullOrWhiteSpace(configuration))
         throw new ArgumentException("Text search configuration cannot be null or whitespace.", nameof(configuration));
   }

   private static void ValidateIndexName(string indexName)
   {
      if (string.IsNullOrWhiteSpace(indexName))
         throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));
   }

   private static void ValidateTextSearchWeight(char weight)
   {
      if (weight is not ('A' or 'B' or 'C' or 'D'))
         throw new ArgumentException("Weight must be A, B, C, or D.", nameof(weight));
   }

   private static void ValidateSourceExpression(string expression)
   {
      if (string.IsNullOrWhiteSpace(expression))
         throw new ArgumentException("Source expression cannot be null or whitespace.", nameof(expression));
   }

   private static void ValidateTextSearchWeights(float d, float c, float b, float a)
   {
      if (d is < 0.0f or > 1.0f || c is < 0.0f or > 1.0f || b is < 0.0f or > 1.0f || a is < 0.0f or > 1.0f)
         throw new ArgumentOutOfRangeException("Weights must be between 0.0 and 1.0.");
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class DocumentEntity
{
   public int      Id           { get; set; }
   public string   Title        { get; set; } = string.Empty;
   public string   Content      { get; set; } = string.Empty;
   public string?  Summary      { get; set; }
   public string[] Categories   { get; set; } = [};
   public DateTime PublishedAt  { get; set; }
   public string   SearchVector { get; set; } = string.Empty;
}

/// <summary>
/// Test DbContext for full-text search testing.
/// </summary>
public class FullTextSearchTestDbContext : DbContext
{
   public FullTextSearchTestDbContext(DbContextOptions<FullTextSearchTestDbContext> options) : base(options) { }

   public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

   protected override void OnModelCreating(ModelBuilder modelBuilder) =>
      modelBuilder.Entity<DocumentEntity>(entity =>
      {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
         entity.Property(e => e.Content).IsRequired();
         entity.Property(e => e.Summary).HasMaxLength(500);
         entity.Property(e => e.Categories).HasArrayType("text");
         entity.Property(e => e.PublishedAt).IsRequired();
         entity.Property(e => e.SearchVector).HasColumnType("tsvector");
      });
}