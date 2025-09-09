// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.MySql;

public static class ConfigurationExtensions
{
   private const string GetDate = "GETDATE()";

   public static void SqlValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder)
   {
      builder.HasDefaultValueSql(GetDate);
      builder.ValueGeneratedOnAdd();
   }
}