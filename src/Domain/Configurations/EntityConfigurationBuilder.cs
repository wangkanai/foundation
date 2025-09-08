// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

public static class EntityConfigurationBuilder
{
   public static void HasDomainKey<T>(this EntityTypeBuilder<Entity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
   {
      builder.HasKey(x => x.Id);
      
      var idProperty = builder.Property(x => x.Id)
                             .IsRequired()
                             .ValueGeneratedOnAdd();

      // Configure based on the primary key type
      ConfigureByKeyType(idProperty);
   }

   private static void ConfigureByKeyType<T>(PropertyBuilder<T> property)
      where T : IEquatable<T>, IComparable<T>
   {
      var keyType = typeof(T);

      switch (Type.GetTypeCode(keyType))
      {
         case TypeCode.Int32:
         case TypeCode.Int64:
            // Let EF Core handle identity generation based on provider
            property.ValueGeneratedOnAdd();
            break;
            
         case TypeCode.String:
            property.HasMaxLength(450);       // Common database index key limit
            break;
            
         case TypeCode.Object when keyType == typeof(Guid):
            // Client-side GUID generation - database agnostic
            property.ValueGeneratedOnAdd();
            break;
      }
   }
}