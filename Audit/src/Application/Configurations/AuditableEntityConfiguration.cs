// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wangkanai.Foundation.Configurations;

namespace Wangkanai.Audit.Configurations;

/// <summary>
/// Provides the base configuration for auditable entities with timestamp tracking.
/// This class defines common audit properties and indexes for entities implementing <see cref="IAuditableEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The auditable entity type.</typeparam>
/// <typeparam name="TKey">
/// The type of the primary key for the auditable entity. Must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
/// </typeparam>
public abstract class AuditableEntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
   where TEntity : class, IAuditableEntity<TKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
{
   /// <summary>Configures the base auditable properties for the entity.</summary>
   /// <param name="builder">An object that provides a simple API for configuring an entity type.</param>
   public virtual void Configure(EntityTypeBuilder<TEntity> builder)
   {
      builder.HasDomainKey<TKey>();
      builder.HasDefaultCreatedAndUpdated();
      builder.HasDefaultDeleted();
   }
}