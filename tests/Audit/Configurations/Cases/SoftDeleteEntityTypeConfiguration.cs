// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Audit.Models;

namespace Wangkanai.Audit.Configurations.Cases;

public class SoftDeleteEntityTypeConfiguration : IEntityTypeConfiguration<SoftDeleteEntity>
{
   public void Configure(EntityTypeBuilder<SoftDeleteEntity> builder)
   {
      builder.HasKey(x => x.Id);
      
      // Configure audit properties
      builder.Property(x => x.Created)
         .IsRequired(false);
      
      builder.Property(x => x.Updated)
         .IsRequired(false);
      
      // Configure soft delete properties
      builder.Property(x => x.IsDeleted)
         .IsRequired()
         .HasDefaultValue(false);
      
      builder.Property(x => x.Deleted)
         .IsRequired(false);
   }
}