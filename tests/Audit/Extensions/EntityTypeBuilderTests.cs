// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Audit.Configurations.Cases;
using Wangkanai.Audit.Mocks;
using Wangkanai.Audit.Models;

namespace Wangkanai.Audit.Extensions;

public class EntityTypeBuilderTests
{
   [Fact]
   public void NewKeyOnAdd_Guid_ShouldHaveId()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<GuidEntity, GuidEntityTypeConfiguration>();
      var entity  = builder.Metadata;
      var id      = entity.FindProperty(nameof(GuidEntity.Id));

      // Act
      builder.NewGuidOnAdd();

      // Assert
      Assert.True(id!.ValueGenerated == ValueGenerated.OnAdd);
   }

   [Fact]
   public void NewKeyOnAdd_Int_ShouldHaveId()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<IntEntity, IntEntityTypeConfiguration>();
      var entity  = builder.Metadata;
      var id      = entity.FindProperty(nameof(IntEntity.Id));

      // Act
      builder.NewKeyOnAdd();

      // Assert
      Assert.True(id!.ValueGenerated == ValueGenerated.OnAdd);
   }

   [Fact]
   public void NewKeyOnAdd_Generic_ShouldHaveId()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<GuidEntity, GuidEntityTypeConfiguration>();
      var entity  = builder.Metadata;
      var id      = entity.FindProperty(nameof(GuidEntity.Id));

      // Act
      builder.NewKeyOnAdd<GuidEntity, Guid>();

      // Assert
      Assert.True(id!.ValueGenerated == ValueGenerated.OnAdd);
   }

   [Fact]
   public void HasDefaultCreated_ShouldConfigureCreatedProperty()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<CreatedEntity, CreatedEntityTypeConfiguration>();
      var entity  = builder.Metadata;
      var created = entity.FindProperty(nameof(CreatedEntity.Created));

      // Act
      builder.HasDefaultCreated();

      // Assert
      Assert.True(created!.ValueGenerated == ValueGenerated.OnAdd);
      // Assert.NotNull(created!.GetDefaultValue());
      // Assert.IsType<DateTime>(created!.GetDefaultValue());
   }

   [Fact]
   public void HasDefaultUpdated_ShouldConfigureUpdatedProperty()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<UpdatedEntity, UpdatedEntityTypeConfiguration>();
      var entity  = builder.Metadata;
      var updated = entity.FindProperty(nameof(UpdatedEntity.Updated));

      // Act
      builder.HasDefaultUpdated();

      // Assert
      Assert.True(updated!.ValueGenerated == ValueGenerated.OnUpdate);
      // Assert.NotNull(updated!.GetDefaultValue());
      // Assert.IsType<DateTime>(updated!.GetDefaultValue());
   }

   [Fact]
   public void HasDefaultCreatedAndUpdated_ShouldConfigureBothProperties()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<AuditEntity, AuditEntityTypeConfiguration>();
      var entity  = builder.Metadata;
      var created = entity.FindProperty(nameof(AuditEntity.Created));
      var updated = entity.FindProperty(nameof(AuditEntity.Updated));

      // Act
      builder.HasDefaultCreatedAndUpdated();

      // Assert
      Assert.True(created!.ValueGenerated == ValueGenerated.OnAdd);
      // Assert.NotNull(created!.GetDefaultValue());
      // Assert.IsType<DateTime>(created!.GetDefaultValue());

      Assert.True(updated!.ValueGenerated == ValueGenerated.OnAddOrUpdate);
      // Assert.NotNull(updated!.GetDefaultValue());
      // Assert.IsType<DateTime>(updated!.GetDefaultValue());
   }

   [Fact]
   public void HasDefaultSoftDelete_ShouldConfigureIsDeletedProperty()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<SoftDeleteEntity, SoftDeleteEntityTypeConfiguration>();
      var entity = builder.Metadata;
      var isDeleted = entity.FindProperty(nameof(SoftDeleteEntity.IsDeleted));

      // Act
      builder.HasDefaultSoftDelete();

      // Assert
      Assert.False(isDeleted!.IsNullable);
      Assert.Equal(false, isDeleted!.GetDefaultValue());
   }

   [Fact]
   public void HasSoftDeleteAudit_ShouldConfigureAllSoftDeleteProperties()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<SoftDeleteEntity, SoftDeleteEntityTypeConfiguration>();
      var entity = builder.Metadata;
      var created = entity.FindProperty(nameof(SoftDeleteEntity.Created));
      var updated = entity.FindProperty(nameof(SoftDeleteEntity.Updated));
      var isDeleted = entity.FindProperty(nameof(SoftDeleteEntity.IsDeleted));
      var deleted = entity.FindProperty(nameof(SoftDeleteEntity.Deleted));

      // Act
      builder.HasSoftDeleteAudit();

      // Assert
      // Check audit properties
      Assert.True(created!.ValueGenerated == ValueGenerated.OnAdd);
      Assert.True(updated!.ValueGenerated == ValueGenerated.OnAddOrUpdate);
      
      // Check soft delete properties
      Assert.False(isDeleted!.IsNullable);
      Assert.Equal(false, isDeleted!.GetDefaultValue());
      Assert.True(deleted!.IsNullable);
   }

   [Fact]
   public void HasUserSoftDeleteAudit_ShouldConfigureAllUserSoftDeleteProperties()
   {
      // Arrange
      var builder = MockExtensions.GetEntityTypeBuilder<UserSoftDeleteEntity, UserSoftDeleteEntityTypeConfiguration>();
      var entity = builder.Metadata;
      var created = entity.FindProperty(nameof(UserSoftDeleteEntity.Created));
      var updated = entity.FindProperty(nameof(UserSoftDeleteEntity.Updated));
      var isDeleted = entity.FindProperty(nameof(UserSoftDeleteEntity.IsDeleted));
      var deleted = entity.FindProperty(nameof(UserSoftDeleteEntity.Deleted));
      var createdBy = entity.FindProperty(nameof(UserSoftDeleteEntity.CreatedBy));
      var updatedBy = entity.FindProperty(nameof(UserSoftDeleteEntity.UpdatedBy));
      var deletedBy = entity.FindProperty(nameof(UserSoftDeleteEntity.DeletedBy));

      // Act
      builder.HasUserSoftDeleteAudit();

      // Assert
      // Check audit properties
      Assert.True(created!.ValueGenerated == ValueGenerated.OnAdd);
      Assert.True(updated!.ValueGenerated == ValueGenerated.OnAddOrUpdate);
      
      // Check soft delete properties
      Assert.False(isDeleted!.IsNullable);
      Assert.Equal(false, isDeleted!.GetDefaultValue());
      Assert.True(deleted!.IsNullable);
      
      // Check user audit properties
      Assert.True(createdBy!.IsNullable);
      Assert.Equal(128, createdBy!.GetMaxLength());
      Assert.True(updatedBy!.IsNullable);
      Assert.Equal(128, updatedBy!.GetMaxLength());
      Assert.True(deletedBy!.IsNullable);
      Assert.Equal(128, deletedBy!.GetMaxLength());
   }
}