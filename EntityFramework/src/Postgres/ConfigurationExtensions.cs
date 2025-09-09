// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.Postgres;

public static class ConfigurationExtensions
{
   public const string Now = "NOW()";

   public static void NpgValueGeneratedOnAdd<TProperty>(this PropertyBuilder<TProperty> builder)
   {
      builder.HasDefaultValueSql("NOW()");
      builder.ValueGeneratedOnAdd();
   }

   public static void NpgValueGeneratedOnUpdate<TProperty>(this PropertyBuilder<TProperty> builder)
   {
      builder.HasDefaultValueSql("NOW()");
      builder.ValueGeneratedOnUpdate();
   }

   public static void NpgValueGeneratedOnAddOrUpdate<TProperty>(this PropertyBuilder<TProperty> builder)
   {
      builder.HasDefaultValueSql("NOW()");
      builder.ValueGeneratedOnAddOrUpdate();
   }

   public static void NpgValueGeneratedNever<TProperty>(this PropertyBuilder<TProperty> builder)
      => builder.ValueGeneratedNever();
}