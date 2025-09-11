// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Unit tests for PostgreSQL array configuration extensions.
/// Tests argument validation and configuration setup for array types.
/// </summary>
public sealed class ArrayConfigurationExtensionsTests
{
   #region Array Operators Optimization Tests

   [Theory]
   [InlineData(ArrayOperators.Contains)]
   [InlineData(ArrayOperators.Overlap)]
   [InlineData(ArrayOperators.Any)]
   [InlineData(ArrayOperators.Contains | ArrayOperators.Overlap)]
   [InlineData(ArrayOperators.Common)]
   public void OptimizeForArrayOperators_WithValidOperators_ShouldConfigureOptimization(ArrayOperators operators)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var result = propertyBuilder.OptimizeForArrayOperators(operators);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   #endregion

   #region Array Aggregation Tests

   [Theory]
   [InlineData(ArrayAggregationFunctions.ArrayAgg)]
   [InlineData(ArrayAggregationFunctions.Unnest)]
   [InlineData(ArrayAggregationFunctions.ArrayLength)]
   [InlineData(ArrayAggregationFunctions.ArrayAgg | ArrayAggregationFunctions.Unnest)]
   [InlineData(ArrayAggregationFunctions.All)]
   public void EnableArrayAggregation_WithValidFunctions_ShouldConfigureAggregation(ArrayAggregationFunctions functions)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values).HasArrayType("integer");

      // Act
      var result = propertyBuilder.EnableArrayAggregation(functions);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   #endregion

   #region HasArrayType Tests

   [Fact]
   public void HasArrayType_WithInferredType_ShouldConfigureArrayColumn()
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values);

      // Act
      var result = propertyBuilder.HasArrayType();

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("integer")]
   [InlineData("text")]
   [InlineData("decimal")]
   [InlineData("uuid")]
   public void HasArrayType_WithExplicitType_ShouldConfigureArrayColumn(string arrayType)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values);

      // Act
      var result = propertyBuilder.HasArrayType(arrayType);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   #endregion

   #region HasMultiDimensionalArray Tests

   [Theory]
   [InlineData(2)]
   [InlineData(3)]
   [InlineData(4)]
   public void HasMultiDimensionalArray_WithValidDimensions_ShouldConfigureArray(int dimensions)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values);

      // Act
      var result = propertyBuilder.HasMultiDimensionalArray(dimensions, "integer");

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(-5)]
   public void HasMultiDimensionalArray_WithInvalidDimensions_ShouldThrowArgumentException(int invalidDimensions)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values);

      // Act
      var act = () => propertyBuilder.HasMultiDimensionalArray(invalidDimensions, "integer");

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("dimensions")
         .WithMessage("*Dimensions must be at least 1.*");
   }

   #endregion

   #region Array Constraints Tests

   [Theory]
   [InlineData(5,  1)]
   [InlineData(10, 2)]
   [InlineData(3,  0)]
   public void HasArrayConstraints_WithValidParameters_ShouldConfigureConstraints(int maxLength, int minLength)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var result = propertyBuilder.HasArrayConstraints(maxLength, minLength, false);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Fact]
   public void HasArrayConstraints_WithNegativeMinLength_ShouldThrowArgumentException()
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var act = () => propertyBuilder.HasArrayConstraints(minLength: -1);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("minLength")
         .WithMessage("*Minimum length cannot be negative.*");
   }

   [Fact]
   public void HasArrayConstraints_WithMinGreaterThanMax_ShouldThrowArgumentException()
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var act = () => propertyBuilder.HasArrayConstraints(5, 10);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("minLength")
         .WithMessage("*Minimum length cannot be greater than maximum length.*");
   }

   #endregion

   #region Array Default Value Tests

   [Theory]
   [InlineData("ARRAY['default', 'new']")]
   [InlineData("'{}'::text[]")]
   [InlineData("ARRAY[1, 2, 3]")]
   public void HasArrayDefaultValue_WithValidValue_ShouldConfigureDefault(string defaultValue)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var result = propertyBuilder.HasArrayDefaultValue(defaultValue);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void HasArrayDefaultValue_WithInvalidValue_ShouldThrowArgumentException(string? invalidValue)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var act = () => propertyBuilder.HasArrayDefaultValue(invalidValue);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("defaultArray")
         .WithMessage("*Default array value cannot be null or whitespace.*");
   }

   #endregion

   #region HasTypedArray Tests

   [Theory]
   [InlineData("integer")]
   [InlineData("text")]
   [InlineData("uuid")]
   [InlineData("decimal")]
   public void HasTypedArray_WithValidType_ShouldConfigureTypedArray(string pgTypeName)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values);

      // Act
      var result = propertyBuilder.HasTypedArray(pgTypeName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void HasTypedArray_WithInvalidType_ShouldThrowArgumentException(string? invalidType)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values);

      // Act
      var act = () => propertyBuilder.HasTypedArray(invalidType);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("pgTypeName")
         .WithMessage("*PostgreSQL type name cannot be null or whitespace.*");
   }

   #endregion

   #region Array Index Configuration Tests

   [Theory]
   [InlineData("ix_test_gin")]
   [InlineData("ix_custom_name")]
   public void HasArrayGinIndex_WithValidIndexName_ShouldConfigureIndex(string indexName)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var result = propertyBuilder.HasArrayGinIndex(indexName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("ix_test_gist")]
   [InlineData("ix_custom_gist")]
   public void HasArrayGistIndex_WithValidIndexName_ShouldConfigureIndex(string indexName)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Values).HasArrayType("integer");

      // Act
      var result = propertyBuilder.HasArrayGistIndex(indexName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(propertyBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void HasArrayGinIndex_WithInvalidIndexName_ShouldThrowArgumentException(string? invalidName)
   {
      // Arrange
      var builder         = new ModelBuilder();
      var entityBuilder   = builder.Entity<ArrayEntity>();
      var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

      // Act
      var act = () => propertyBuilder.HasArrayGinIndex(invalidName);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("indexName")
         .WithMessage("*Index name cannot be null or whitespace.*");
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class ArrayEntity
{
   public int      Id     { get; set; }
   public int[]    Values { get; set; } = [];
   public string[] Tags   { get; set; } = [];
}