// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Audit.Models;

namespace Wangkanai.Audit.Extensions;

public class SoftDeleteExtensionsTests
{
   [Fact]
   public void MarkAsDeleted_SoftDeleteEntity_ShouldSetProperties()
   {
      // Arrange
      var entity = new SoftDeleteEntity();
      var beforeTime = DateTime.UtcNow.AddSeconds(-1);

      // Act
      var result = entity.MarkAsDeleted();
      var afterTime = DateTime.UtcNow.AddSeconds(1);

      // Assert
      Assert.Same(entity, result); // Should return same instance for chaining
      Assert.True(entity.IsDeleted);
      Assert.NotNull(entity.Deleted);
      Assert.True(entity.Deleted >= beforeTime && entity.Deleted <= afterTime);
   }

   [Fact]
   public void MarkAsDeleted_WithSpecificTime_ShouldUseProvidedTime()
   {
      // Arrange
      var entity = new SoftDeleteEntity();
      var specificTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

      // Act
      entity.MarkAsDeleted(specificTime);

      // Assert
      Assert.True(entity.IsDeleted);
      Assert.Equal(specificTime, entity.Deleted);
   }

   [Fact]
   public void MarkAsDeleted_UserSoftDeleteEntity_ShouldSetAllProperties()
   {
      // Arrange
      var entity = new UserSoftDeleteEntity();
      const string deletedBy = "admin";
      var beforeTime = DateTime.UtcNow.AddSeconds(-1);

      // Act
      var result = entity.MarkAsDeleted(deletedBy);
      var afterTime = DateTime.UtcNow.AddSeconds(1);

      // Assert
      Assert.Same(entity, result); // Should return same instance for chaining
      Assert.True(entity.IsDeleted);
      Assert.Equal(deletedBy, entity.DeletedBy);
      Assert.NotNull(entity.Deleted);
      Assert.True(entity.Deleted >= beforeTime && entity.Deleted <= afterTime);
   }

   [Fact]
   public void MarkAsDeleted_UserSoftDeleteEntity_WithSpecificTime_ShouldUseProvidedTime()
   {
      // Arrange
      var entity = new UserSoftDeleteEntity();
      const string deletedBy = "admin";
      var specificTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

      // Act
      entity.MarkAsDeleted(deletedBy, specificTime);

      // Assert
      Assert.True(entity.IsDeleted);
      Assert.Equal(deletedBy, entity.DeletedBy);
      Assert.Equal(specificTime, entity.Deleted);
   }

   [Fact]
   public void Restore_SoftDeleteEntity_ShouldClearProperties()
   {
      // Arrange
      var entity = new SoftDeleteEntity
      {
         IsDeleted = true,
         Deleted = DateTime.UtcNow
      };

      // Act
      var result = entity.Restore();

      // Assert
      Assert.Same(entity, result); // Should return same instance for chaining
      Assert.False(entity.IsDeleted);
      Assert.Null(entity.Deleted);
   }

   [Fact]
   public void Restore_UserSoftDeleteEntity_ShouldClearAllProperties()
   {
      // Arrange
      var entity = new UserSoftDeleteEntity
      {
         IsDeleted = true,
         Deleted = DateTime.UtcNow,
         DeletedBy = "admin"
      };

      // Act
      var result = entity.Restore();

      // Assert
      Assert.Same(entity, result); // Should return same instance for chaining
      Assert.False(entity.IsDeleted);
      Assert.Null(entity.Deleted);
      Assert.Null(entity.DeletedBy);
   }

   [Fact]
   public void IsSoftDeleted_WhenDeleted_ShouldReturnTrue()
   {
      // Arrange
      var entity = new SoftDeleteEntity { IsDeleted = true };

      // Act & Assert
      Assert.True(entity.IsSoftDeleted());
   }

   [Fact]
   public void IsSoftDeleted_WhenNotDeleted_ShouldReturnFalse()
   {
      // Arrange
      var entity = new SoftDeleteEntity { IsDeleted = false };

      // Act & Assert
      Assert.False(entity.IsSoftDeleted());
   }

   [Fact]
   public void IsActive_WhenNotDeleted_ShouldReturnTrue()
   {
      // Arrange
      var entity = new SoftDeleteEntity { IsDeleted = false };

      // Act & Assert
      Assert.True(entity.IsActive());
   }

   [Fact]
   public void IsActive_WhenDeleted_ShouldReturnFalse()
   {
      // Arrange
      var entity = new SoftDeleteEntity { IsDeleted = true };

      // Act & Assert
      Assert.False(entity.IsActive());
   }

   [Fact]
   public void SoftDeleteWorkflow_FullCycle_ShouldWork()
   {
      // Arrange
      var entity = new UserSoftDeleteEntity();
      const string deletedBy = "user123";

      // Act & Assert - Initial state
      Assert.True(entity.IsActive());
      Assert.False(entity.IsSoftDeleted());

      // Act & Assert - Delete
      entity.MarkAsDeleted(deletedBy);
      Assert.False(entity.IsActive());
      Assert.True(entity.IsSoftDeleted());
      Assert.Equal(deletedBy, entity.DeletedBy);
      Assert.NotNull(entity.Deleted);

      // Act & Assert - Restore
      entity.Restore();
      Assert.True(entity.IsActive());
      Assert.False(entity.IsSoftDeleted());
      Assert.Null(entity.DeletedBy);
      Assert.Null(entity.Deleted);
   }
}