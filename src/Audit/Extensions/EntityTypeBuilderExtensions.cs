// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Audit;

namespace Microsoft.EntityFrameworkCore.Metadata.Builders;

public static class EntityTypeBuilderExtensions
{
      /// <summary>Configures the entity type to set a default value of the current date and time for the Created property when a new entity is added to the database context. This method is intended for entities implementing the
   /// <see cref="ICreatedEntity"/> interface.</summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="ICreatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultCreated<T>(this EntityTypeBuilder<T> builder)
      where T : class, ICreatedEntity
      => builder.Property(x => x.Created)
                .HasDefaultValue(DateTime.Now)
                .ValueGeneratedOnAdd();

   /// <summary>Configures the entity type to set a default value for the Updated property and to mark it as a value that is automatically generated when the entity is updated. This method is intended for entities that implement the
   /// <see cref="IUpdatedEntity"/> interface.</summary>
   /// <typeparam name="T">The type of the entity being configured. Must implement <see cref="IUpdatedEntity"/>.</typeparam>
   /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
   public static void HasDefaultUpdated<T>(this EntityTypeBuilder<T> builder)
      where T : class, IUpdatedEntity
      => builder.Property(x => x.Updated)
                .HasDefaultValue(DateTime.Now)
                .ValueGeneratedOnUpdate();

   /// <summary>Configures the entity type to set default values and value generation strategies for the Created and Updated properties. The Created property is assigned a default value of the current date and time and is generated when a new entity is added. The Updated property is assigned a default value of the current date and time and is generated or updated when the entity is added or modified.</summary>
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
}