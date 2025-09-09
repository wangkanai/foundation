// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.Sqlite;

/// <summary>
/// Provides extension methods for configuration of Entity Framework properties targeting SQLite.
/// </summary>
public static class ConfigurationExtensions
{
   /// <summary> Configures the specified property to have an SQLite value generated on adding. </summary>
   /// <typeparam name="T">The type of the property being configured.</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   public static void SqliteValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder)
   {
      builder.ValueGeneratedOnAdd();
   }
}