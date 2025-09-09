// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Foundation.Models;

namespace Wangkanai.Foundation.Configurations.Cases;

public class IntEntityTypeConfiguration : IEntityTypeConfiguration<IntEntity>
{
   public void Configure(EntityTypeBuilder<IntEntity> builder) => builder.HasKey(c => c.Id);
}