// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Foundation.Configurations;

/// <summary>
/// Provides extension methods for configuring entity keys in a domain-driven design context.
/// This class is used to simplify the configuration of primary keys for generic entity types.
/// </summary>
public static class KeyConfigurationBuilder
{
   private const int IndexKeyLength = 450;

   /// <summary>
   /// Configures the primary key for an entity type in a domain-driven design context.
   /// </summary>
   /// <typeparam name="T">
   /// The type of the primary key. It must implement <see cref="IEquatable{T}"/>
   /// and <see cref="IComparable{T}"/>.
   /// </typeparam>
   /// <param name="builder">
   /// The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.
   /// </param>
   public static void HasDomainKey<T>(this EntityTypeBuilder<IEntity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
   {
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Id)
             .IsRequired()
             .ApplyKeyOptimizations();
   }

   /// <summary>
   /// Configures the primary key for entities inheriting from Entity{T}.
   /// </summary>
   /// <typeparam name="TEntity">The entity type that inherits from Entity{T}.</typeparam>
   /// <typeparam name="TKey">The type of the primary key.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   public static EntityTypeBuilder<TEntity> HasDomainKey<TEntity, TKey>(this EntityTypeBuilder<TEntity> builder)
      where TEntity : Entity<TKey>
      where TKey : IEquatable<TKey>, IComparable<TKey>
   {
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Id)
             .IsRequired()
             .ApplyKeyOptimizations();
      return builder;
   }

   /// <summary>
   /// Configures an entity inheriting from Entity{T} with comprehensive settings.
   /// </summary>
   /// <typeparam name="TEntity">The entity type that inherits from Entity{T}.</typeparam>
   /// <typeparam name="TKey">The type of the primary key.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   public static EntityTypeBuilder<TEntity> ConfigureEntity<TEntity, TKey>(this EntityTypeBuilder<TEntity> builder)
      where TEntity : Entity<TKey>
      where TKey : IEquatable<TKey>, IComparable<TKey>
   {
      builder.HasDomainKey<TEntity, TKey>();
      builder.HasIndex(x => x.Id);
      return builder;
   }

   /// <summary>
   /// Configures optimized ID settings for entities inheriting from Entity{T}.
   /// </summary>
   /// <typeparam name="TEntity">The entity type that inherits from Entity{T}.</typeparam>
   /// <typeparam name="TKey">The type of the primary key.</typeparam>
   /// <param name="builder">The entity type builder.</param>
   public static EntityTypeBuilder<TEntity> HasOptimizedId<TEntity, TKey>(this EntityTypeBuilder<TEntity> builder)
      where TEntity : Entity<TKey>
      where TKey : IEquatable<TKey>, IComparable<TKey>
   {
      builder.Property(x => x.Id)
             .IsRequired()
             .ApplyKeyOptimizations();

      builder.HasIndex(x => x.Id);
      return builder;
   }

   private static PropertyBuilder<T> ApplyKeyOptimizations<T>(this PropertyBuilder<T> property)
      where T : IEquatable<T>, IComparable<T>
   {
      var keyType = typeof(T);

      _ = Type.GetTypeCode(keyType) switch
          {
             TypeCode.Byte                                => property.ValueGeneratedOnAdd(),
             TypeCode.Int32                               => property.ValueGeneratedOnAdd(),
             TypeCode.Int64                               => property.ValueGeneratedOnAdd(),                         // EF Core handles identity generation
             TypeCode.String                              => property.HasMaxLength(IndexKeyLength).IsUnicode(false), // String keys are application-provided
             TypeCode.Object when keyType == typeof(Guid) => property.ValueGeneratedOnAdd(),                         // Client-side GUID generation
             _                                            => property
          };

      return property;
   }
}