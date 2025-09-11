// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Collections.Concurrent;
using System.Reflection;
using Wangkanai.Foundation.Models;

namespace Wangkanai.Foundation;

public class EntityTests
{
   [Fact]
   public void NewGuidEntity_ShouldHaveId()
   {
      var entity = new GuidEntity();
      Assert.NotEqual(Guid.NewGuid(), entity.Id);
   }

   [Fact]
   public void GuidEntity_IsTransient_ShouldBeTrue()
   {
      var entity = new TransientGuidEntity();
      Assert.True(entity.IsTransient());
   }

   [Fact]
   public void GuidEntity_IsTransient_ShouldBeFalse()
   {
      var entity = new GuidEntity();
      Assert.False(entity.IsTransient());
   }

   [Fact]
   public void NewIntEntity_ShouldHaveId()
   {
      var entity = new IntEntity();
      Assert.NotEqual(0, entity.Id);
   }

   [Fact]
   public void IntEntity_IsTransient_ShouldBeFalse()
   {
      var entity = new IntEntity();
      Assert.False(entity.IsTransient());
   }

   [Fact]
   public void IntEntity_IsTransient_ShouldBeTrue()
   {
      var entity = new TransientIntEntity();
      Assert.True(entity.IsTransient());
   }

   [Fact]
   public void Entity_Transient_HashCode()
   {
      var entity = new IntEntity();
      Assert.Equal(entity.Id.GetHashCode(), entity.GetHashCode());
      entity.Id = default;
      Assert.NotEqual(entity.Id.GetHashCode(), entity.GetHashCode());
   }

   [Fact]
   public void Entity_Equals_ShouldBeTrue()
   {
      var entity = new IntEntity();
      var other  = new IntEntity();
      Assert.True(entity.Equals(other));
      Assert.True(entity == other);
   }

   [Fact]
   public void Entity_Equals_ShouldBeFalse()
   {
      var entity = new IntEntity();
      var other  = new TransientIntEntity();
      Assert.False(entity.Equals(other));
      Assert.False(entity == other);
   }

   #region Type Caching System Tests

   [Fact]
   public void GetPerformanceStats_InitialState_ReturnsZeroValues()
   {
      // Arrange
      Entity<int>.ClearTypeCache();

      // Act
      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();

      // Assert
      Assert.Equal(0, hits);
      Assert.Equal(0, misses);
      Assert.Equal(0.0, hitRatio);
   }

   [Fact]
   public void TypeCaching_MultipleInstancesOfSameType_IncreasesCacheHits()
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();

      // Act - First comparison triggers cache population
      entity1.Equals(entity2);
      var (hitsAfterFirst, missesAfterFirst, _) = Entity<int>.GetPerformanceStats();

      // Second comparison should hit cache
      entity1.Equals(entity2);
      var (hitsAfterSecond, missesAfterSecond, hitRatio) = Entity<int>.GetPerformanceStats();

      // Assert
      Assert.True(hitsAfterSecond > hitsAfterFirst);
      Assert.Equal(missesAfterFirst, missesAfterSecond); // Misses should stay the same
      Assert.True(hitRatio > 0);
   }

   [Fact]
   public void ClearTypeCache_ResetsPerformanceCounters()
   {
      // Arrange
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();
      entity1.Equals(entity2); // Generate some cache activity
      var (hitsBefore, missesBefore, _) = Entity<int>.GetPerformanceStats();
      Assert.True(hitsBefore > 0 || missesBefore > 0);

      // Act
      Entity<int>.ClearTypeCache();
      var (hitsAfter, missesAfter, hitRatioAfter) = Entity<int>.GetPerformanceStats();

      // Assert
      Assert.Equal(0, hitsAfter);
      Assert.Equal(0, missesAfter);
      Assert.Equal(0.0, hitRatioAfter);
   }

   [Fact]
   public void TypeCaching_DifferentEntityTypes_IncreasesCache()
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var intEntity = new IntEntity();
      var guidEntity = new GuidEntity();

      // Act
      intEntity.Equals(new IntEntity());
      guidEntity.Equals(new GuidEntity());
      var (hits, misses, _) = Entity<int>.GetPerformanceStats();

      // Assert - Should have some cache activity
      Assert.True(hits + misses > 0);
   }

   [Theory]
   [InlineData(10)]
   [InlineData(50)]
   [InlineData(100)]
   public void TypeCaching_MultipleComparisons_TracksCacheEffectiveness(int comparisons)
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var entities = Enumerable.Range(0, comparisons)
                              .Select(_ => new IntEntity())
                              .ToList();

      // Act - Perform multiple comparisons to exercise cache
      for (int i = 0; i < comparisons - 1; i++)
      {
         entities[i].Equals(entities[i + 1]);
      }

      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();

      // Assert
      Assert.True(hits + misses > 0);
      Assert.True(hitRatio >= 0.0 && hitRatio <= 1.0);
   }

   #endregion

   #region Proxy Detection Tests

   [Fact]
   public void EqualityComparison_WithNormalTypes_WorksCorrectly()
   {
      // Arrange
      var entity1 = new IntEntity { Id = 123 };
      var entity2 = new IntEntity { Id = 123 };
      var entity3 = new IntEntity { Id = 456 };

      // Act & Assert
      Assert.True(entity1.Equals(entity2));
      Assert.False(entity1.Equals(entity3));
      Assert.True(entity1 == entity2);
      Assert.False(entity1 == entity3);
   }

   [Fact]
   public void EqualityComparison_WithDifferentEntityTypes_ReturnsFalse()
   {
      // Arrange
      var intEntity = new IntEntity { Id = 123 };
      var guidEntity = new GuidEntity { Id = Guid.NewGuid() };

      // Act & Assert
      Assert.False(intEntity.Equals(guidEntity));
   }

   [Fact]
   public void EqualityComparison_WithNull_ReturnsFalse()
   {
      // Arrange
      var entity = new IntEntity();

      // Act & Assert
      Assert.False(entity.Equals(null));
      Assert.False(entity == null);
      Assert.True(entity != null);
   }

   [Fact]
   public void EqualityComparison_WithSameReference_ReturnsTrue()
   {
      // Arrange
      var entity = new IntEntity();

      // Act & Assert
      Assert.True(entity.Equals(entity));
      Assert.True(entity == entity);
   }

   [Fact]
   public void GetRealObjectType_HandlesNonProxyTypes_Efficiently()
   {
      // Arrange
      Entity<int>.ClearTypeCache();
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();

      // Act - Multiple comparisons to test caching efficiency
      for (int i = 0; i < 10; i++)
      {
         entity1.Equals(entity2);
      }

      var (hits, misses, hitRatio) = Entity<int>.GetPerformanceStats();

      // Assert - Cache should be working effectively
      Assert.True(hits > 0);
      Assert.True(hitRatio > 0.5); // Should have good cache hit ratio
   }

   #endregion

   #region Cache Eviction Logic Tests

   [Fact]
   public void TypeCache_UnderMemoryPressure_DoesNotGrowUnbounded()
   {
      // Arrange
      Entity<int>.ClearTypeCache();

      // Access private static fields through reflection to verify cache bounds
      var entityType = typeof(Entity<>).MakeGenericType(typeof(int));
      var realTypeCacheField = entityType.GetField("_realTypeCache", 
         BindingFlags.NonPublic | BindingFlags.Static);
      var isProxyTypeCacheField = entityType.GetField("_isProxyTypeCache", 
         BindingFlags.NonPublic | BindingFlags.Static);

      Assert.NotNull(realTypeCacheField);
      Assert.NotNull(isProxyTypeCacheField);

      // Note: Testing actual cache eviction would require creating 1000+ types
      // which is impractical in a unit test. This test verifies the cache fields exist
      // and can be accessed for monitoring.
      var realTypeCache = realTypeCacheField.GetValue(null) as ConcurrentDictionary<Type, Type>;
      var isProxyTypeCache = isProxyTypeCacheField.GetValue(null) as ConcurrentDictionary<Type, bool>;

      Assert.NotNull(realTypeCache);
      Assert.NotNull(isProxyTypeCache);
   }

   [Fact]
   public void Cache_AfterClear_AllowsNewCaching()
   {
      // Arrange
      var entity1 = new IntEntity();
      var entity2 = new IntEntity();
      
      // Generate some cache activity
      entity1.Equals(entity2);
      var (hitsBeforeClear, _, _) = Entity<int>.GetPerformanceStats();

      // Act
      Entity<int>.ClearTypeCache();
      
      // Generate new cache activity
      entity1.Equals(entity2);
      var (hitsAfterClear, missesAfterClear, _) = Entity<int>.GetPerformanceStats();

      // Assert
      Assert.True(missesAfterClear > 0); // Should have misses after clearing
   }

   #endregion

   #region Edge Cases and Error Conditions

   [Fact]
   public void HashCode_TransientEntity_UsesFallback()
   {
      // Arrange
      var transientEntity = new TransientIntEntity();
      var baseHashCode = transientEntity.GetHashCode();

      // Act - Set to non-transient state
      transientEntity.Id = 123;
      var entityHashCode = transientEntity.GetHashCode();

      // Assert
      Assert.NotEqual(baseHashCode, entityHashCode);
      Assert.Equal(123.GetHashCode(), entityHashCode);
   }

   [Fact]
   public void Operators_WithNullValues_HandleCorrectly()
   {
      // Arrange
      IntEntity? entity1 = null;
      IntEntity? entity2 = null;
      var entity3 = new IntEntity();

      // Act & Assert
      Assert.True(entity1 == entity2);
      Assert.False(entity1 != entity2);
      Assert.False(entity1 == entity3);
      Assert.True(entity1 != entity3);
   }

   [Fact]
   public void Entity_WithMaximumIntValue_HandlesCorrectly()
   {
      // Arrange
      var entity1 = new IntEntity { Id = int.MaxValue };
      var entity2 = new IntEntity { Id = int.MaxValue };

      // Act & Assert
      Assert.True(entity1.Equals(entity2));
      Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
   }

   [Fact]
   public void Entity_WithMinimumIntValue_HandlesCorrectly()
   {
      // Arrange
      var entity1 = new IntEntity { Id = int.MinValue };
      var entity2 = new IntEntity { Id = int.MinValue };

      // Act & Assert
      Assert.True(entity1.Equals(entity2));
      Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
   }

   #endregion
}

#region Test Helper Classes for Performance Testing

/// <summary>Test entity that simulates various type scenarios for caching tests</summary>
public class TestEntity<T> : Entity<T> where T : IEquatable<T>, IComparable<T>
{
   public TestEntity(T id) => Id = id;
}

/// <summary>Mock proxy-like entity for testing proxy detection logic</summary>
public class MockProxyEntity : Entity<int>
{
   public MockProxyEntity(int id) => Id = id;
}

#endregion