// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

public class EntityConfiguration<T> : IEntityTypeConfiguration<Entity<T>>
   where T : IComparable<T>, IEquatable<T>
{
   public void Configure(EntityTypeBuilder<Entity<T>> builder)
   {
      builder.HasDomainKey();
   }
}