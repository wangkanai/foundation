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
   public static PropertyBuilder<T> SqliteValueGeneratedOnAdd<T>(this PropertyBuilder<T> builder) => builder.ValueGeneratedOnAdd();

   /// <summary>
   /// Configures the specified string property to use NOCASE collation for case-insensitive comparisons in SQLite.
   /// This optimization improves performance for string searches and comparisons.
   /// </summary>
   /// <param name="builder">The property builder for the string property being configured.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   public static PropertyBuilder<string> HasSqliteCollation(this PropertyBuilder<string> builder) => builder.HasAnnotation("Relational:Collation", "NOCASE");

   /// <summary>
   /// Optimizes the specified string property for SQLite search operations by configuring appropriate settings
   /// for equality comparisons and indexing performance.
   /// </summary>
   /// <param name="builder">The property builder for the string property being configured.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   public static PropertyBuilder<string> OptimizeForSqliteSearch(this PropertyBuilder<string> builder) =>
      builder
        .HasAnnotation("Relational:Collation", "NOCASE")
        .HasAnnotation("Sqlite:InlineFts",     true);

   /// <summary>
   /// Configures the specified property to use TEXT affinity in SQLite, ensuring proper string handling
   /// and comparison behavior. This is particularly useful for properties that should be treated as text
   /// even if they contain numeric values.
   /// </summary>
   /// <typeparam name="T">The type of the property being configured.</typeparam>
   /// <param name="builder">The property builder used to configure the property.</param>
   /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
   public static PropertyBuilder<T> HasSqliteTextAffinity<T>(this PropertyBuilder<T> builder) => builder.HasAnnotation("Sqlite:Affinity", "TEXT");
}