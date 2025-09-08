// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit;

public static class EntityTypeBuilderExtensions
{
   /// <summary>
   /// Configures the entity type to set a default value of the current UTC DateTimeOffset for the
   /// <see cref="ICreatedEntity.Created"/> property when a new entity is added to the database context.
   /// This method is intended for entities implementing the <see cref="ICreatedEntity"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="ICreatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultCreated<T>(this EntityTypeBuilder<T> builder)
      where T : class, ICreatedEntity
   {
      builder.Property(x => x.Created)
             .HasDefaultValueSql("SYSDATETIMEOFFSET()")
             .ValueGeneratedOnAdd();
      builder.HasIndex(x => x.Created);
   }

   /// <summary>
   /// Configures the entity type to set a default value for the <see cref="IUpdatedEntity.Updated"/> property using UTC DateTimeOffset and
   /// to mark it as a value that is automatically generated when the entity is updated.
   /// This method is intended for entities that implement the <see cref="IUpdatedEntity"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="IUpdatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultUpdated<T>(this EntityTypeBuilder<T> builder)
      where T : class, IUpdatedEntity
   {
      builder.Property(x => x.Updated)
             .HasDefaultValueSql("SYSDATETIMEOFFSET()")
             .ValueGeneratedOnUpdate();
      builder.HasIndex(x => x.Updated);
   }

   /// <summary>
   /// Configures the entity type to set default values and value generation strategies for the
   /// <see cref="ICreatedEntity.Created"/> and <see cref="IUpdatedEntity.Updated"/> properties using UTC DateTimeOffset.
   /// The <see cref="ICreatedEntity.Created"/> property is assigned a default value of the current UTC DateTimeOffset and
   /// is generated when a new entity is added.
   /// The Updated property is assigned a default value of the current UTC DateTimeOffset and is generated or
   /// updated when the entity is added or modified.
   /// </summary>
   /// <typeparam name="T">
   /// The type of the entity being configured. Must implement <see cref="ICreatedEntity"/> and <see cref="IUpdatedEntity"/>.
   /// </typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultCreatedAndUpdated<T>(this EntityTypeBuilder<T> builder)
      where T : class, ICreatedEntity, IUpdatedEntity
   {
      builder.HasDefaultCreated();

      builder.Property(x => x.Updated)
             .HasDefaultValueSql("SYSDATETIMEOFFSET()")
             .ValueGeneratedOnAddOrUpdate();
      builder.HasIndex(x => x.Updated);
   }

   /// <summary>
   /// Configures the entity type to set a default value of null for the <see cref="IDeletedEntity.Deleted"/> property,
   /// intended for soft delete scenarios. This method is applicable to entities implementing
   /// the <see cref="IDeletedEntity"/> interface.
   /// </summary>
   /// <typeparam name="T">
   /// The type of the entity being configured. Must implement <see cref="IDeletedEntity"/>.
   /// </typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultDeleted<T>(this EntityTypeBuilder<T> builder)
      where T : class, IDeletedEntity
   {
      builder.Property(x => x.Deleted)
             .HasDefaultValue(null)
             .ValueGeneratedOnUpdate();
      builder.HasIndex(x => x.Deleted);
   }
}