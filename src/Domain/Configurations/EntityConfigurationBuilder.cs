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
             .ApplyKeyOptimizations();
   }

   private static void ApplyKeyOptimizations<T>(this PropertyBuilder<T> property)
      where T : IEquatable<T>, IComparable<T>
   {
      var keyType = typeof(T);

      _ = Type.GetTypeCode(keyType) switch
          {
             TypeCode.Int32 or TypeCode.Int64             => property.ValueGeneratedOnAdd(),                              // EF Core handles identity generation
             TypeCode.String                              => property.HasMaxLength(IndexKeyLength),                       // String keys are application-provided
             TypeCode.Object when keyType == typeof(Guid) => property.ValueGeneratedOnAdd(),                              // Client-side GUID generation
             _                                            => property
          };
   }
}