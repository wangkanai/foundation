// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Foundation;
using Wangkanai.Foundation.Configurations;

namespace Wangkanai.Audit.Configurations;

public static class AuditableConfigurationBuilder
{
   public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
      where TEntity : class, IEntity<int>, IAuditableEntity
   {
      builder.HasDefaultCreated();
      builder.HasDefaultCreatedAndUpdated();
      builder.HasDefaultDeleted();
   }

   public static void ConfigureAuditableEntity<TEntity, TKey>(this EntityTypeBuilder<TEntity> builder)
      where TEntity : class, IEntity<TKey>, IAuditableEntity<TKey>
      where TKey : IEquatable<TKey>, IComparable<TKey>
   {
      //builder.HasDomainKey<TKey>();
      builder.HasDefaultCreated();
      builder.HasDefaultCreatedAndUpdated();
      builder.HasDefaultDeleted();
   }
}