// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit;

public static class EntityTypeBuilderExtensions
{
   /// <summary>
   /// Configures the entity type to set a default value of the current date and time for the Created property when a new entity is added to the database context.
   /// This method is intended for entities implementing the <see cref="ICreatedEntity"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="ICreatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultCreated<T>(this EntityTypeBuilder<T> builder)
      where T : class, ICreatedEntity
      => builder.Property(x => x.Created)
                .HasDefaultValue(DateTime.Now)
                .ValueGeneratedOnAdd();

   /// <summary>
   /// Configures the entity type to set a default value for the Updated property and to mark it as a value that is automatically generated when the entity is updated.
   /// This method is intended for entities that implement the <see cref="IUpdatedEntity"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="IUpdatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultUpdated<T>(this EntityTypeBuilder<T> builder)
      where T : class, IUpdatedEntity
      => builder.Property(x => x.Updated)
                .HasDefaultValue(DateTime.Now)
                .ValueGeneratedOnUpdate();

   /// <summary>
   /// Configures the entity type to set default values and value generation strategies for the Created and Updated properties.
   /// The Created property is assigned a default value of the current date and time and is generated when a new entity is added.
   /// The Updated property is assigned a default value of the current date and time and is generated or updated when the entity is added or modified.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="ICreatedEntity"/> and <see cref="IUpdatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultCreatedAndUpdated<T>(this EntityTypeBuilder<T> builder)
      where T : class, ICreatedEntity, IUpdatedEntity
   {
      builder.HasDefaultCreated();

      builder.Property(x => x.Updated)
             .HasDefaultValue(DateTime.Now)
             .ValueGeneratedOnAddOrUpdate();
   }

   /// <summary>
   /// Configures the entity type to set a default value of false for the IsDeleted property.
   /// This method is intended for entities implementing the <see cref="ISoftDeletable"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="ISoftDeletable"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultSoftDelete<T>(this EntityTypeBuilder<T> builder)
      where T : class, ISoftDeletable
      => builder.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

   /// <summary>
   /// Configures the entity type for comprehensive soft delete audit functionality.
   /// Sets up the IsDeleted property with a default value of false and configures the Deleted timestamp property.
   /// This method is intended for entities implementing the <see cref="ISoftDeleteAuditable"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="ISoftDeleteAuditable"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasSoftDeleteAudit<T>(this EntityTypeBuilder<T> builder)
      where T : class, ISoftDeleteAuditable
   {
      builder.HasDefaultCreatedAndUpdated();
      builder.HasDefaultSoftDelete();

      builder.Property(x => x.Deleted)
             .IsRequired(false);
   }

   /// <summary>
   /// Configures the entity type for user-tracked soft delete audit functionality.
   /// Sets up all audit properties including user tracking for creation, updates, and soft deletion.
   /// This method is intended for entities implementing the <see cref="IUserSoftDeleteAuditable"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="IUserSoftDeleteAuditable"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasUserSoftDeleteAudit<T>(this EntityTypeBuilder<T> builder)
      where T : class, IUserSoftDeleteAuditable
   {
      builder.HasSoftDeleteAudit();

      builder.Property(x => x.CreatedBy)
             .HasMaxLength(128)
             .IsRequired(false);

      builder.Property(x => x.UpdatedBy)
             .HasMaxLength(128)
             .IsRequired(false);

      builder.Property(x => x.DeletedBy)
             .HasMaxLength(128)
             .IsRequired(false);
   }
}