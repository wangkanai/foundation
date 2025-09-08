// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

public static class EntityConfigurationBuilder
{
   private const int IndexKeyLength = 450;

   public static void HasDomainKey<T>(this EntityTypeBuilder<Entity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
   {
      builder.HasKey(x => x.Id);

      builder.Property(x => x.Id)
             .IsRequired()
             .ValueGeneratedOnAdd()
             .HasKeyOptimization();
   }

   public static void HasKeyOptimization<T>(this PropertyBuilder<T> property)
      where T : IEquatable<T>, IComparable<T>
   {
      var keyType = typeof(T);

      _ = Type.GetTypeCode(keyType) switch
          {
             TypeCode.Int32 or TypeCode.Int64             => property,                               // Already configured with ValueGeneratedOnAdd()
             TypeCode.String                              => property.HasMaxLength(IndexKeyLength), // Common database index key limit
             TypeCode.Object when keyType == typeof(Guid) => property.ValueGeneratedOnAdd(),        // Client-side GUID generation - database agnostic
             _                                            => property
          };
   }
}