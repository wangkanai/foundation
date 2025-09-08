// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.Configurations;

public static class AuditableConfigurationBuilder
{
   public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
      where TEntity : class, IAuditableEntity
   {
      builder.HasDefaultCreated();
      builder.HasDefaultCreatedAndUpdated();
      builder.HasDefaultDeleted();
   }
}