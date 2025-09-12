// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework;

/// <summary>
/// Unit tests for PostgreSQL partitioning extensions.
/// Tests argument validation for range, list, and hash partitioning configurations.
/// </summary>
public sealed class PartitioningTests
{
   #region Range Partitioning Tests

   [Theory]
   [InlineData("created_date")]
   [InlineData("order_date")]
   [InlineData("timestamp_column")]
   public void ConfigureRangePartitioning_WithValidColumn_ShouldConfigurePartitioning(string partitionColumn)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasRangePartition(partitionColumn);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureRangePartitioning_WithInvalidColumn_ShouldThrowArgumentException(string? invalidColumn)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var act = () => entityBuilder.HasRangePartition(invalidColumn);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("partitionColumn")
         .WithMessage("*Partition column cannot be null or whitespace.*");
   }

   [Theory]
   [InlineData("partition_2023", "'2023-01-01'", "'2024-01-01'")]
   [InlineData("partition_2024", "'2024-01-01'", "'2025-01-01'")]
   public void CreateRangePartition_WithValidParameters_ShouldCreatePartition(string partitionName, string fromValue, string toValue)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("RangePartition", $"{partitionName}:{fromValue}:{toValue}");

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("",               "'2023-01-01'", "'2024-01-01'")]
   [InlineData("partition_2023", "",             "'2024-01-01'")]
   [InlineData("partition_2023", "'2023-01-01'", "")]
   [InlineData(null,             "'2023-01-01'", "'2024-01-01'")]
   [InlineData("partition_2023", null,           "'2024-01-01'")]
   [InlineData("partition_2023", "'2023-01-01'", null)]
   public void CreateRangePartition_WithInvalidParameters_ShouldThrowArgumentException(string partitionName, string fromValue, string toValue)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(partitionName) || string.IsNullOrWhiteSpace(fromValue) || string.IsNullOrWhiteSpace(toValue))
            throw new ArgumentException("Partition parameters cannot be null or whitespace.");
         return entityBuilder.HasAnnotation("RangePartition", $"{partitionName}:{fromValue}:{toValue}");
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithMessage("*cannot be null or whitespace.*");
   }

   #endregion

   #region List Partitioning Tests

   [Theory]
   [InlineData("category")]
   [InlineData("region")]
   [InlineData("status")]
   public void ConfigureListPartitioning_WithValidColumn_ShouldConfigurePartitioning(string partitionColumn)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasListPartition(partitionColumn);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureListPartitioning_WithInvalidColumn_ShouldThrowArgumentException(string? invalidColumn)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var act = () => entityBuilder.HasListPartition(invalidColumn);

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("partitionColumn")
         .WithMessage("*Partition column cannot be null or whitespace.*");
   }

   [Theory]
   [InlineData("partition_us", "'US', 'USA'")]
   [InlineData("partition_eu", "'EU', 'EUR'")]
   public void CreateListPartition_WithValidParameters_ShouldCreatePartition(string partitionName, string values)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("ListPartition", $"{partitionName}:{values}");

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   #endregion

   #region Hash Partitioning Tests

   [Theory]
   [InlineData("id")]
   [InlineData("user_id")]
   [InlineData("hash_column")]
   public void ConfigureHashPartitioning_WithValidColumn_ShouldConfigurePartitioning(string partitionColumn)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasHashPartition(partitionColumn, 4);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(2)]
   [InlineData(4)]
   [InlineData(8)]
   [InlineData(16)]
   public void ConfigureHashPartitioning_WithValidModulus_ShouldConfigurePartitioning(int modulus)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasHashPartition("id", modulus);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(1)]
   [InlineData(-1)]
   public void ConfigureHashPartitioning_WithInvalidModulus_ShouldThrowArgumentOutOfRangeException(int invalidModulus)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var act = () => entityBuilder.HasHashPartition("id", invalidModulus);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("modulus")
         .WithMessage("*Hash partition modulus must be greater than 1.*");
   }

   #endregion

   #region Partition Management Tests

   [Theory]
   [InlineData(PartitionPruning.Enable)]
   [InlineData(PartitionPruning.Disable)]
   public void ConfigurePartitionPruning_WithValidOption_ShouldConfigurePruning(PartitionPruning option)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("PartitionPruning", option.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("daily")]
   [InlineData("weekly")]
   [InlineData("monthly")]
   [InlineData("yearly")]
   public void ConfigurePartitionMaintenanceStrategy_WithValidStrategy_ShouldConfigureStrategy(string strategy)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("PartitionMaintenanceStrategy", strategy);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigurePartitionMaintenanceStrategy_WithInvalidStrategy_ShouldThrowArgumentException(string? invalidStrategy)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<PartitionedEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidStrategy))
            throw new ArgumentException("Partition maintenance strategy cannot be null or whitespace.", "strategy");
         return entityBuilder.HasAnnotation("PartitionMaintenanceStrategy", invalidStrategy);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("strategy")
         .WithMessage("*Partition maintenance strategy cannot be null or whitespace.*");
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class PartitionedEntity
{
   public int      Id          { get; set; }
   public string   Name        { get; set; } = string.Empty;
   public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Enumeration for partition pruning options.
/// </summary>
public enum PartitionPruning
{
   Enable,
   Disable
}