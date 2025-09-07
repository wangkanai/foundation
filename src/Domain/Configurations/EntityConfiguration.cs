// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Domain.Configurations;

/// <summary>
/// Represents the base configuration class for an entity of type <see cref="Entity{T}"/>.
/// Provides configuration settings for the entity's domain-specific model using Entity Framework Core conventions.
/// </summary>
/// <typeparam name="T">
/// The type of the unique identifier for the entity. Must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
/// </typeparam>
public class EntityConfiguration<T> : IEntityTypeConfiguration<Entity<T>>
   where T : IComparable<T>, IEquatable<T>
{
   public void Configure(EntityTypeBuilder<Entity<T>> builder)
   {
      builder.HasDomainKey();
   }
}