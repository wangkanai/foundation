// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Audit.Models;

namespace Wangkanai.Audit.Configurations.Cases;

public class UpdatedEntityTypeConfiguration : IEntityTypeConfiguration<UpdatedEntity>
{
   public void Configure(EntityTypeBuilder<UpdatedEntity> builder) => builder.HasKey(x => x.Id);
}