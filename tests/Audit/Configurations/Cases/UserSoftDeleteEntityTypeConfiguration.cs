// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Audit.Models;

namespace Wangkanai.Audit.Configurations.Cases;

public class UserSoftDeleteEntityTypeConfiguration : IEntityTypeConfiguration<UserSoftDeleteEntity>
{
   public void Configure(EntityTypeBuilder<UserSoftDeleteEntity> builder)
   {
      builder.HasKey(x => x.Id);
      
      // Configure audit properties
      builder.Property(x => x.Created)
         .IsRequired(false);
      
      builder.Property(x => x.Updated)
         .IsRequired(false);
      
      // Configure user audit properties
      builder.Property(x => x.CreatedBy)
         .HasMaxLength(128)
         .IsRequired(false);
      
      builder.Property(x => x.UpdatedBy)
         .HasMaxLength(128)
         .IsRequired(false);
      
      // Configure soft delete properties
      builder.Property(x => x.IsDeleted)
         .IsRequired()
         .HasDefaultValue(false);
      
      builder.Property(x => x.Deleted)
         .IsRequired(false);
      
      builder.Property(x => x.DeletedBy)
         .HasMaxLength(128)
         .IsRequired(false);
   }
}