// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.Sqlite;

public static class ConfigurationExtensions
{
   public static void SqliteValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder)
   {
      builder.ValueGeneratedOnAdd();
   }
}