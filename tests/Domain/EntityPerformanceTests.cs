// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Domain.Models;

namespace Wangkanai.Domain;

/// <summary>
/// Performance validation tests for the optimized Entity equality implementation.
/// Validates that caching system improves performance without breaking functionality.
/// </summary>
public class EntityPerformanceTests
{
   [Fact]
   public void Entity_TypeCache_Should_Improve_Performance()
   {
      // Arrange: Clear cache to start fresh
      Entity<int>.ClearTypeCache();

      var entity1 = new IntEntity(); // Id = 1
      var entity2 = new IntEntity(); // Id = 1 (same as A)

      // Act: First comparison triggers cache miss for both types, second should hit cache
      var result1 = entity1.Equals(entity2);
      var result2 = entity1.Equals(entity2);

      // Assert: Both should be equal
      Assert.True(result1);
      Assert.True(result2);

      // Assert: Cache should show hits and misses
      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();
      Assert.True(misses   >= 1, $"Expected at least 1 cache miss, got {misses}");
      Assert.True(hits     >= 1, $"Expected at least 1 cache hit, got {hits}");
      Assert.True(hitRatio > 0,  $"Expected positive hit ratio, got {hitRatio}");
   }

   [Fact]
   public void Entity_TypeCache_Should_Handle_Different_Types()
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      Entity<Guid>.ClearTypeCache();

      var intEntity  = new IntEntity();
      var guidEntity = new GuidEntity();

      // Act: Compare different entity types (this should be false)
      var result = intEntity.Equals(guidEntity);

      // Assert
      Assert.False(result);

      // At least one of the caches should have activity
      var intStats  = Entity<int>.GetPerformanceStats();
      var guidStats = Entity<Guid>.GetPerformanceStats();

      var totalActivity = intStats.Hits + intStats.Misses + guidStats.Hits + guidStats.Misses;
      Assert.True(totalActivity > 0, "Expected some cache activity for type comparisons");
   }

   [Fact]
   public void Entity_TypeCache_Should_Be_Thread_Safe()
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();
      var results = new bool[100];

      // Act: Concurrent equality comparisons
      Parallel.For(0, 100, i =>
      {
         results[i] = entity1.Equals(entity2);
      });

      // Assert: All comparisons should succeed
      Assert.All(results, result => Assert.True(result));

      // Cache stats should be consistent
      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();
      Assert.True(hits + misses > 0, "Cache should have recorded operations");
   }

   [Fact]
   public void Entity_ClearTypeCache_Should_Reset_Statistics()
   {
      // Arrange: Perform some operations to generate cache entries
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();
      entity1.Equals(entity2);

      // Verify cache has entries
      var statsBefore = Entity<int>.GetPerformanceStats();
      Assert.True(statsBefore.Hits + statsBefore.Misses > 0);

      // Act: Clear cache
      Entity<int>.ClearTypeCache();

      // Assert: Statistics should be reset
      var statsAfter = Entity<int>.GetPerformanceStats();
      Assert.Equal(0,   statsAfter.Hits);
      Assert.Equal(0,   statsAfter.Misses);
      Assert.Equal(0.0, statsAfter.HitRatio);
   }

   [Fact]
   public void Entity_Performance_Should_Maintain_Correctness()
   {
      // Arrange: Test various equality scenarios
      var entity1   = new IntEntity();          // Id = 1
      var entity2   = new IntEntity();          // Id = 1
      var entity3   = new IntEntity { Id = 2 }; // Id = 2
      var transient = new TransientIntEntity(); // Id = 0

      // Act & Assert: All equality operations should work correctly
      Assert.True(entity1.Equals(entity2));    // Same type, same ID
      Assert.False(entity1.Equals(entity3));   // Same type, different ID
      Assert.False(entity1.Equals(transient)); // Different type
      Assert.False(entity1.Equals(null));      // Null comparison
      Assert.True(entity1.Equals(entity1));    // Self comparison

      // Operator overloads should work consistently
      Assert.True(entity1  == entity2);
      Assert.False(entity1 != entity2);
      Assert.False(entity1 == entity3);
      Assert.True(entity1  != entity3);
   }

   [Theory]
   [InlineData(100)]
   [InlineData(1000)]
   [InlineData(10000)]
   public void Entity_Bulk_Operations_Should_Show_Cache_Benefit(int iterations)
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();

      // Act: Perform bulk equality operations
      for (var i = 0; i < iterations; i++)
         entity1.Equals(entity2);

      // Assert: Hit ratio should improve with more operations
      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();

      // With caching, we should see many more hits than misses after warmup
      Assert.True(hits + misses > 0, "Expected some cache activity");

      if (iterations >= 1000)
      {
         // For large iteration counts, hit ratio should be very high
         Assert.True(hitRatio > 0.8, $"Expected hit ratio > 80% for {iterations} iterations, got {hitRatio:P2}");
      }
   }

   [Fact]
   public void Entity_Cache_Should_Track_Operations()
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();

      // Act: Multiple calls to force different entities through type checking
      for (var i = 0; i < 5; i++)
         entity1.Equals(entity2); // Different objects, same type

      // Assert: Cache should record operations (hits or misses)
      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();
      Assert.True(hits + misses > 0, $"Expected cache activity, got hits: {hits}, misses: {misses}");
   }
}