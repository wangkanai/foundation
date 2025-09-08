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
   {
      var propertyBuilder = builder.Property<T>(RowVersion).IsConcurrencyToken();

      // Apply SQL Server specific configuration for byte[] types
      if (typeof(T) == typeof(byte[]))
         propertyBuilder.IsRowVersion();

      return propertyBuilder;
   }

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
      => builder.Property<byte[]>(RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

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
      => builder.Property<byte[]>(RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
}