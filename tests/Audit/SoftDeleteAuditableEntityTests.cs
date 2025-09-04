// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Audit.Models;

namespace Wangkanai.Audit;

public class SoftDeleteAuditableEntityTests
{
   [Fact]
   public void SoftDeleteEntity_ShouldImplement_ISoftDeleteAuditable()
   {
      // Arrange & Act
      var entity = new SoftDeleteEntity();

      // Assert
      Assert.IsAssignableFrom<ISoftDeleteAuditable>(entity);
      Assert.IsAssignableFrom<ISoftDeletable>(entity);
      Assert.IsAssignableFrom<IDeletedEntity>(entity);
      Assert.IsAssignableFrom<IAuditable>(entity);
   }

   [Fact]
   public void SoftDeleteEntity_DefaultState_ShouldNotBeDeleted()
   {
      // Arrange & Act
      var entity = new SoftDeleteEntity();

      // Assert
      Assert.False(entity.IsDeleted);
      Assert.Null(entity.Deleted);
   }

   [Fact]
   public void SoftDeleteEntity_SetIsDeleted_ShouldRetainValue()
   {
      // Arrange
      var entity = new SoftDeleteEntity();

      // Act
      entity.IsDeleted = true;

      // Assert
      Assert.True(entity.IsDeleted);
   }

   [Fact]
   public void SoftDeleteEntity_SetDeleted_ShouldRetainValue()
   {
      // Arrange
      var entity      = new SoftDeleteEntity();
      var deletedTime = DateTime.UtcNow;

      // Act
      entity.Deleted = deletedTime;

      // Assert
      Assert.Equal(deletedTime, entity.Deleted);
   }

   [Fact]
   public void SoftDeleteEntity_MarkAsDeleted_ShouldSetBothProperties()
   {
      // Arrange
      var entity      = new SoftDeleteEntity();
      var deletedTime = DateTime.UtcNow;

      // Act
      entity.IsDeleted = true;
      entity.Deleted   = deletedTime;

      // Assert
      Assert.True(entity.IsDeleted);
      Assert.Equal(deletedTime, entity.Deleted);
   }

   [Fact]
   public void SoftDeleteEntity_AuditProperties_ShouldBeAvailable()
   {
      // Arrange
      var entity      = new SoftDeleteEntity();
      var createdTime = DateTime.UtcNow.AddHours(-1);
      var updatedTime = DateTime.UtcNow;

      // Act
      entity.Created = createdTime;
      entity.Updated = updatedTime;

      // Assert
      Assert.Equal(createdTime, entity.Created);
      Assert.Equal(updatedTime, entity.Updated);
   }

   [Fact]
   public void UserSoftDeleteEntity_ShouldImplement_IUserSoftDeleteAuditable()
   {
      // Arrange & Act
      var entity = new UserSoftDeleteEntity();

      // Assert
      Assert.IsAssignableFrom<IUserSoftDeleteAuditable>(entity);
      Assert.IsAssignableFrom<ISoftDeleteAuditable>(entity);
      Assert.IsAssignableFrom<IUserAuditable>(entity);
   }

   [Fact]
   public void UserSoftDeleteEntity_DefaultState_ShouldHaveNullUserProperties()
   {
      // Arrange & Act
      var entity = new UserSoftDeleteEntity();

      // Assert
      Assert.False(entity.IsDeleted);
      Assert.Null(entity.Deleted);
      Assert.Null(entity.CreatedBy);
      Assert.Null(entity.UpdatedBy);
      Assert.Null(entity.DeletedBy);
   }

   [Fact]
   public void UserSoftDeleteEntity_SetDeletedBy_ShouldRetainValue()
   {
      // Arrange
      var          entity = new UserSoftDeleteEntity();
      const string userId = "user123";

      // Act
      entity.DeletedBy = userId;

      // Assert
      Assert.Equal(userId, entity.DeletedBy);
   }

   [Fact]
   public void UserSoftDeleteEntity_FullDeleteOperation_ShouldTrackAllProperties()
   {
      // Arrange
      var          entity      = new UserSoftDeleteEntity();
      var          deletedTime = DateTime.UtcNow;
      const string deletedBy   = "admin";

      // Act
      entity.IsDeleted = true;
      entity.Deleted   = deletedTime;
      entity.DeletedBy = deletedBy;

      // Assert
      Assert.True(entity.IsDeleted);
      Assert.Equal(deletedTime, entity.Deleted);
      Assert.Equal(deletedBy,   entity.DeletedBy);
   }

   [Fact]
   public void UserSoftDeleteEntity_UserAuditProperties_ShouldBeAvailable()
   {
      // Arrange
      var          entity  = new UserSoftDeleteEntity();
      const string creator = "creator123";
      const string updater = "updater456";

      // Act
      entity.CreatedBy = creator;
      entity.UpdatedBy = updater;

      // Assert
      Assert.Equal(creator, entity.CreatedBy);
      Assert.Equal(updater, entity.UpdatedBy);
   }
}