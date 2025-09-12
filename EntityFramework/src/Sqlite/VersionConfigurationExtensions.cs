// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring SQLite-specific row versioning behaviors on Entity Framework Core properties.
/// </summary>
public static class VersionConfigurationExtensions
{
   private const string DefaultColumnName = "row_version";
   private const long   DefaultStartValue = 1L;

   /// <summary>
   /// Configures a property as an SQLite row version column with integer-based versioning starting at 1.
   /// Requires application-level increment logic in SaveChanges override.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured (must be long).</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   /// <returns>The property builder for method chaining.</returns>
   public static PropertyBuilder<T> HasSqliteRowVersion<T>(this PropertyBuilder<T> builder)
      => builder.HasSqliteRowVersion(DefaultStartValue);

   /// <summary>
   /// Configures a property as an SQLite row version column with integer-based versioning starting at the specified value.
   /// Requires application-level increment logic in SaveChanges override.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured (must be long).</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   /// <param name="startValue">The starting value for the row version.</param>
   /// <returns>The property builder for method chaining.</returns>
   public static PropertyBuilder<T> HasSqliteRowVersion<T>(this PropertyBuilder<T> builder, long startValue)
   {
      builder.HasColumnName(DefaultColumnName)
             .HasDefaultValue(startValue)
             .IsConcurrencyToken()
             .ValueGeneratedNever(); // Manual versioning requires no automatic generation

      return builder;
   }

   /// <summary>
   /// Configures a property as an SQLite timestamp-based row version column.
   /// Uses DateTime values for optimistic concurrency control.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured (must be DateTime).</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   /// <returns>The property builder for method chaining.</returns>
   public static PropertyBuilder<T> HasSqliteTimestampRowVersion<T>(this PropertyBuilder<T> builder)
   {
      builder.HasColumnName(DefaultColumnName)
             .IsConcurrencyToken()
             .ValueGeneratedOnAddOrUpdate(); // Automatically updated on changes

      return builder;
   }
}