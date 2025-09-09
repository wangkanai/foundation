// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.MySql;

/// <summary>
/// Provides extension methods for configuring MySQL-specific behaviors on Entity Framework Core properties.
/// </summary>
public static class ConfigurationExtensions
{
   private const string Now = "NOW()";

   /// <summary>
   /// Configures a property to use the MySQL "NOW()" function as the default value and
   /// to generate its value only when a new entity is added.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured.</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   public static void MySqlValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder)
   {
      builder.HasDefaultValueSql(Now);
      builder.ValueGeneratedOnAdd();
   }
}