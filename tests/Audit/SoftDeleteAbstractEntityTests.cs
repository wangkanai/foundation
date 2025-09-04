// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Audit.Models;

namespace Wangkanai.Audit;

public class SoftDeleteAbstractEntityTests
{
   [Fact]
   public void SoftDeleteAuditableEntity_ShouldImplementCorrectInterfaces()
   {
      // Arrange & Act
      var entity = new ConcreteSoftDeleteAuditableEntity();

      // Assert
      Assert.IsAssignableFrom<SoftDeleteAuditableEntity<Guid>>(entity);
      Assert.IsAssignableFrom<AuditableEntity<Guid>>(entity);
      Assert.IsAssignableFrom<ISoftDeleteAuditable>(entity);
      Assert.IsAssignableFrom<Entity<Guid>>(entity);
   }

   [Fact]
   public void SoftDeleteAuditableEntity_DefaultSerializationProperties_ShouldReturnTrue()
   {
      // Arrange & Act
      var entity = new ConcreteSoftDeleteAuditableEntity();

      // Assert
      Assert.True(entity.ShouldSerializeSoftDeleteProperties);
      Assert.True(entity.ShouldSerializeIsDeleted());
      Assert.True(entity.ShouldSerializeDeleted());
      Assert.True(entity.ShouldSerializeAuditableProperties);
   }

   [Fact]
   public void SoftDeleteAuditableEntity_DefaultValues_ShouldBeCorrect()
   {
      // Arrange & Act
      var entity = new ConcreteSoftDeleteAuditableEntity();

      // Assert
      Assert.False(entity.IsDeleted);
      Assert.Null(entity.Deleted);
      Assert.Null(entity.Created);
      Assert.Null(entity.Updated);
      Assert.NotEqual(Guid.Empty, entity.Id);
   }

   [Fact]
   public void UserSoftDeleteAuditableEntity_ShouldImplementCorrectInterfaces()
   {
      // Arrange & Act
      var entity = new ConcreteUserSoftDeleteAuditableEntity();

      // Assert
      Assert.IsAssignableFrom<UserSoftDeleteAuditableEntity<Guid>>(entity);
      Assert.IsAssignableFrom<SoftDeleteAuditableEntity<Guid>>(entity);
      Assert.IsAssignableFrom<IUserSoftDeleteAuditable>(entity);
      Assert.IsAssignableFrom<IUserAuditable>(entity);
   }

   [Fact]
   public void UserSoftDeleteAuditableEntity_SerializationMethods_ShouldFollowPattern()
   {
      // Arrange & Act
      var entity = new ConcreteUserSoftDeleteAuditableEntity();

      // Assert
      Assert.True(entity.ShouldSerializeDeletedBy());
      Assert.True(entity.ShouldSerializeSoftDeleteProperties);
      Assert.True(entity.ShouldSerializeCreatedBy());
      Assert.True(entity.ShouldSerializeUpdatedBy());
   }

   [Fact]
   public void UserSoftDeleteAuditableEntity_DefaultValues_ShouldBeCorrect()
   {
      // Arrange & Act
      var entity = new ConcreteUserSoftDeleteAuditableEntity();

      // Assert
      Assert.False(entity.IsDeleted);
      Assert.Null(entity.Deleted);
      Assert.Null(entity.DeletedBy);
      Assert.Null(entity.CreatedBy);
      Assert.Null(entity.UpdatedBy);
      Assert.NotEqual(Guid.Empty, entity.Id);
   }

   [Fact]
   public void UserSoftDeleteAuditableEntity_FullAuditCycle_ShouldWork()
   {
      // Arrange
      var          entity  = new ConcreteUserSoftDeleteAuditableEntity();
      var          now     = DateTime.UtcNow;
      const string creator = "creator";
      const string updater = "updater";
      const string deleter = "deleter";

      // Act - Create
      entity.Created   = now.AddHours(-2);
      entity.CreatedBy = creator;

      // Act - Update
      entity.Updated   = now.AddHours(-1);
      entity.UpdatedBy = updater;

      // Act - Delete
      entity.IsDeleted = true;
      entity.Deleted   = now;
      entity.DeletedBy = deleter;

      // Assert
      Assert.Equal(creator, entity.CreatedBy);
      Assert.Equal(updater, entity.UpdatedBy);
      Assert.Equal(deleter, entity.DeletedBy);
      Assert.True(entity.IsDeleted);
      Assert.True(entity.Created < entity.Updated);
      Assert.True(entity.Updated < entity.Deleted);
   }

   [Fact]
   public void SoftDeleteAuditableEntity_PropertyAssignment_ShouldWork()
   {
      // Arrange
      var entity     = new ConcreteSoftDeleteAuditableEntity();
      var deleteTime = DateTime.UtcNow;

      // Act
      entity.IsDeleted = true;
      entity.Deleted   = deleteTime;

      // Assert
      Assert.True(entity.IsDeleted);
      Assert.Equal(deleteTime, entity.Deleted);
   }
}