// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

/// <summary>
/// Provides extension methods to configure RowVersion properties for entity types.
/// Offers support for concurrency control in entity framework configurations.
/// </summary>
public static class VersionConfigurationBuilder
{
   private const string RowVersion = "RowVersion";

   /// <summary>
   /// Configures a RowVersion property with appropriate database-specific settings.
   /// </summary>
   /// <typeparam name="T">The type of the RowVersion property.</typeparam>
   /// <param name="property">The property builder to configure.</param>
   /// <returns>The configured property builder.</returns>
   private static PropertyBuilder ConfigureRowVersion<T>(this PropertyBuilder property)
   {
      property.IsConcurrencyToken();

      if (typeof(T) == typeof(byte[]))
         property.IsRowVersion();

      return property;
   }

   /// <summary>
   /// Configures the entity type to include a RowVersion property for concurrency control with a generic type.
   /// </summary>
   /// <typeparam name="T">
   /// The type of the RowVersion property (e.g., byte[], string, long, etc.).
   /// </typeparam>
   /// <param name="builder">
   /// The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.
   /// </param>
   /// <returns>
   /// A <see cref="PropertyBuilder"/> configured with the RowVersion property as a concurrency token.
   /// </returns>
   public static PropertyBuilder HasRowVersion<T>(this EntityTypeBuilder<IHasRowVersion<T>> builder)
      => builder.Property<T>(RowVersion).ConfigureRowVersion<T>();

   /// <summary>
   /// Configures the entity type to include a RowVersion property for concurrency control.
   /// Uses byte[] type for compatibility with SQL Server and most relational databases.
   /// </summary>
   /// <param name="builder">
   /// The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.
   /// </param>
   /// <returns>
   /// A <see cref="PropertyBuilder"/> configured with the RowVersion property as a concurrency token.
   /// </returns>
   public static PropertyBuilder HasRowVersion(this EntityTypeBuilder<IHasRowVersion> builder)
      => builder.Property<byte[]>(RowVersion).ConfigureRowVersion<byte[]>();

   /// <summary>
   /// Configures the entity type to include a RowVersion property for concurrency control.
   /// </summary>
   /// <typeparam name="T">
   /// The type of the entity's primary key, which must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
   /// </typeparam>
   /// <param name="builder">
   /// The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.
   /// </param>
   /// <returns>
   /// A <see cref="PropertyBuilder"/> configured with the RowVersion property as a concurrency token.
   /// </returns>
   public static PropertyBuilder HasRowVersion<T>(this EntityTypeBuilder<Entity<T>> builder)
      where T : IEquatable<T>, IComparable<T>
      => builder.Property<byte[]>(RowVersion).ConfigureRowVersion<byte[]>();
}