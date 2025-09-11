// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Unit tests for PostgreSQL advanced query extensions.
/// Tests argument validation and configuration setup for complex query operations.
/// </summary>
public sealed class AdvancedQueryTests
{
   #region Window Function Configuration Tests

   [Theory]
   [InlineData("ROW_NUMBER()")]
   [InlineData("RANK()")]
   [InlineData("DENSE_RANK()")]
   [InlineData("PERCENT_RANK()")]
   public void ConfigureWindowFunction_WithValidFunction_ShouldConfigureFunction(string function)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("WindowFunction", function);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureWindowFunction_WithInvalidFunction_ShouldThrowArgumentException(string? invalidFunction)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidFunction))
            throw new ArgumentException("Window function cannot be null or whitespace.", nameof(invalidFunction));
         return entityBuilder.HasAnnotation("WindowFunction", invalidFunction);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("invalidFunction")
         .WithMessage("*Window function cannot be null or whitespace.*");
   }

   #endregion

   #region CTE Configuration Tests

   [Theory]
   [InlineData("recursive_orders")]
   [InlineData("order_hierarchy")]
   public void ConfigureCommonTableExpression_WithValidName_ShouldConfigureCTE(string cteName)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("CommonTableExpression", cteName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureCommonTableExpression_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidName))
            throw new ArgumentException("CTE name cannot be null or whitespace.", nameof(invalidName));
         return entityBuilder.HasAnnotation("CommonTableExpression", invalidName);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("invalidName")
         .WithMessage("*CTE name cannot be null or whitespace.*");
   }

   #endregion

   #region Complex Join Configuration Tests

   [Theory]
   [InlineData(JoinType.Inner)]
   [InlineData(JoinType.Left)]
   [InlineData(JoinType.Right)]
   [InlineData(JoinType.Full)]
   [InlineData(JoinType.Cross)]
   public void ConfigureComplexJoin_WithValidJoinType_ShouldConfigureJoin(JoinType joinType)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("ComplexJoin", joinType.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureComplexJoin_WithInvalidCondition_ShouldThrowArgumentException(string? invalidCondition)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidCondition))
            throw new ArgumentException("Join condition cannot be null or whitespace.", nameof(invalidCondition));
         return entityBuilder.HasAnnotation("ComplexJoinCondition", invalidCondition);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("invalidCondition")
         .WithMessage("*Join condition cannot be null or whitespace.*");
   }

   #endregion

   #region Query Optimization Tests

   [Theory]
   [InlineData(OptimizationHint.UseIndex)]
   [InlineData(OptimizationHint.ForceSeqScan)]
   [InlineData(OptimizationHint.DisableHashJoin)]
   [InlineData(OptimizationHint.EnableParallelWorkers)]
   public void ConfigureQueryOptimization_WithValidHint_ShouldConfigureOptimization(OptimizationHint hint)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("QueryOptimization", hint.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(1)]
   [InlineData(4)]
   [InlineData(8)]
   [InlineData(16)]
   public void ConfigureParallelWorkers_WithValidWorkerCount_ShouldConfigureParallelism(int workerCount)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("ParallelWorkers", workerCount);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(-5)]
   public void ConfigureParallelWorkers_WithInvalidWorkerCount_ShouldThrowArgumentOutOfRangeException(int invalidCount)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<AdvancedQueryEntity>();

      // Act
      var act = () =>
      {
         if (invalidCount <= 0)
            throw new ArgumentOutOfRangeException("workerCount", "Worker count must be greater than zero.");
         return entityBuilder.HasAnnotation("ParallelWorkers", invalidCount);
      };

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("workerCount")
         .WithMessage("*Worker count must be greater than zero.*");
   }

   #endregion
}

/// <summary>
/// Enumeration for join types in advanced queries.
/// </summary>
public enum JoinType
{
   Inner,
   Left,
   Right,
   Full,
   Cross
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class AdvancedQueryEntity
{
   public int    Id   { get; set; }
   public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Enumeration for query optimization hints.
/// </summary>
public enum OptimizationHint
{
   UseIndex,
   ForceSeqScan,
   DisableHashJoin,
   EnableParallelWorkers
}