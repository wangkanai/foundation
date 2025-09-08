// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

public static class EntityConfigurationBuilder
{
   private const int    IndexKeyLength = 450;
   private const string RowVersion     = "RowVersion";

   public static void HasDomainKey<T>(this EntityTypeBuilder<Entity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
   {
      builder.HasKey(x => x.Id);

      builder.Property(x => x.Id)
             .IsRequired()
             .ApplyKeyOptimizations();
   }

   public static void HasRowVersion<T>(this EntityTypeBuilder<Entity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
   {
      builder.Property<byte[]>(RowVersion)
             .IsRowVersion()
             .IsConcurrencyToken();
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