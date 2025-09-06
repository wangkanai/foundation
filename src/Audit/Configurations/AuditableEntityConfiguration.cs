// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.Configurations;

/// <summary>
/// Provides the base configuration for auditable entities with timestamp tracking.
/// This class defines common audit properties and indexes for entities implementing <see cref="IAuditable"/>.
/// </summary>
/// <typeparam name="TEntity">The auditable entity type.</typeparam>
/// <typeparam name="TKey">
/// The type of the primary key for the auditable entity. Must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
/// </typeparam>
public abstract class AuditableEntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
	where TEntity : class, IAuditable
	where TKey : IEquatable<TKey>, IComparable<TKey>
{
	/// <summary>Configures the base auditable properties for the entity.</summary>
	/// <param name="builder">An object that provides a simple API for configuring an entity type.</param>
	public virtual void Configure(EntityTypeBuilder<TEntity> builder)
	{
		ConfigureAuditableProperties(builder);
		ConfigureAdditionalProperties(builder);
	}

	/// <summary>Configures the basic auditable timestamp properties.</summary>
	/// <param name="builder">The entity type builder.</param>
	protected virtual void ConfigureAuditableProperties(EntityTypeBuilder<TEntity> builder)
	{
		// Configure base auditable properties
		builder.Property(x => x.Created)
			   .IsRequired(false);

		builder.Property(x => x.Updated)
			   .IsRequired(false);

		// Create indexes for efficient querying
		builder.HasIndex(x => x.Created);
		builder.HasIndex(x => x.Updated);
	}

	/// <summary>Override this method to configure additional properties specific to the derived entity type.</summary>
	/// <param name="builder">The entity type builder.</param>
	protected abstract void ConfigureAdditionalProperties(EntityTypeBuilder<TEntity> builder);
}