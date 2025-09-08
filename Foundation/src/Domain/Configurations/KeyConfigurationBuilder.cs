// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

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
   public static void HasDomainKey<T>(this EntityTypeBuilder<Entity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
   {
      builder.HasKey(x => x.Id);

      builder.Property(x => x.Id)
             .IsRequired()
             .ApplyKeyOptimizations();
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