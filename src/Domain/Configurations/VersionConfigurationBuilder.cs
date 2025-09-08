// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

/// <summary>
/// Provides extension methods for configuring row versioning properties in an entity type.
/// </summary>
public static class VersionConfigurationBuilder
{
   private const string RowVersion = "RowVersion";

   /// <summary>
   /// Configures the entity type to include a RowVersion property for concurrency control.
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