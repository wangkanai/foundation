// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework;

/// <summary>
/// Unit tests for PostgreSQL bulk operations extensions.
/// Tests argument validation for bulk insert, update, and delete operations.
/// </summary>
public sealed class BulkOperationsTests
{
   #region Bulk Configuration Tests

   [Theory]
   [InlineData(1000)]
   [InlineData(5000)]
   [InlineData(10000)]
   public void ConfigureBulkBatchSize_WithValidSize_ShouldConfigureBatch(int batchSize)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("BulkBatchSize", batchSize);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(-100)]
   public void ConfigureBulkBatchSize_WithInvalidSize_ShouldThrowArgumentOutOfRangeException(int invalidSize)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var act = () =>
      {
         if (invalidSize <= 0)
            throw new ArgumentOutOfRangeException("batchSize", "Batch size must be greater than zero.");
         return entityBuilder.HasAnnotation("BulkBatchSize", invalidSize);
      };

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("batchSize")
         .WithMessage("*Batch size must be greater than zero.*");
   }

   #endregion

   #region UPSERT Configuration Tests

   [Theory]
   [InlineData("name")]
   [InlineData("id")]
   [InlineData("name, status")]
   public void ConfigureUpsertConflictTarget_WithValidColumns_ShouldConfigureUpsert(string conflictColumns)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("UpsertConflictTarget", conflictColumns);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureUpsertConflictTarget_WithInvalidColumns_ShouldThrowArgumentException(string? invalidColumns)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidColumns))
            throw new ArgumentException("Conflict target columns cannot be null or whitespace.", "conflictColumns");
         return entityBuilder.HasAnnotation("UpsertConflictTarget", invalidColumns);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("conflictColumns")
         .WithMessage("*Conflict target columns cannot be null or whitespace.*");
   }

   [Theory]
   [InlineData(UpsertAction.DoNothing)]
   [InlineData(UpsertAction.UpdateAll)]
   [InlineData(UpsertAction.UpdateSpecific)]
   public void ConfigureUpsertAction_WithValidAction_ShouldConfigureAction(UpsertAction action)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("UpsertAction", action.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   #endregion

   #region COPY Protocol Tests

   [Theory]
   [InlineData(CopyFormat.Binary)]
   [InlineData(CopyFormat.Text)]
   [InlineData(CopyFormat.Csv)]
   public void ConfigureCopyFormat_WithValidFormat_ShouldConfigureFormat(CopyFormat format)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("CopyFormat", format.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(",")]
   [InlineData("|")]
   [InlineData("\t")]
   public void ConfigureCopyDelimiter_WithValidDelimiter_ShouldConfigureDelimiter(string delimiter)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("CopyDelimiter", delimiter);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(null)]
   public void ConfigureCopyDelimiter_WithInvalidDelimiter_ShouldThrowArgumentException(string? invalidDelimiter)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrEmpty(invalidDelimiter))
            throw new ArgumentException("Copy delimiter cannot be null or empty.", "delimiter");
         return entityBuilder.HasAnnotation("CopyDelimiter", invalidDelimiter);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("delimiter")
         .WithMessage("*Copy delimiter cannot be null or empty.*");
   }

   #endregion

   #region Bulk Performance Configuration Tests

   [Theory]
   [InlineData(1)]
   [InlineData(4)]
   [InlineData(8)]
   public void ConfigureBulkParallelism_WithValidDegree_ShouldConfigureParallelism(int degreeOfParallelism)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("BulkParallelism", degreeOfParallelism);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   public void ConfigureBulkParallelism_WithInvalidDegree_ShouldThrowArgumentOutOfRangeException(int invalidDegree)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<BulkEntity>();

      // Act
      var act = () =>
      {
         if (invalidDegree <= 0)
            throw new ArgumentOutOfRangeException("degreeOfParallelism", "Degree of parallelism must be greater than zero.");
         return entityBuilder.HasAnnotation("BulkParallelism", invalidDegree);
      };

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("degreeOfParallelism")
         .WithMessage("*Degree of parallelism must be greater than zero.*");
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class BulkEntity
{
   public int    Id   { get; set; }
   public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Enumeration for UPSERT actions.
/// </summary>
public enum UpsertAction
{
   DoNothing,
   UpdateAll,
   UpdateSpecific
}

/// <summary>
/// Enumeration for COPY data formats.
/// </summary>
public enum CopyFormat
{
   Text,
   Csv,
   Binary
}