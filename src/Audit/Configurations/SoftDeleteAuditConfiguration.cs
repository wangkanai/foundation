// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.Configurations;

/// <summary>
/// Provides the configuration for the <see cref="SoftDeleteAuditableEntity{T}"/> entity type.
/// This class is responsible for defining the model and entity relationship mappings for use with Entity Framework
/// for entities that require soft delete capabilities with audit tracking.
/// </summary>
/// <typeparam name="T">
/// The type of the primary key for the auditable entity. Must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
/// </typeparam>
public class SoftDeleteAuditConfiguration<T> : AuditableEntityConfiguration<SoftDeleteAuditableEntity<T>, T>
	where T : IEquatable<T>, IComparable<T>
{
	/// <summary>Configures the additional soft delete properties for the <see cref="SoftDeleteAuditableEntity{T}"/> class.</summary>
	/// <param name="builder">An object that provides a simple API for configuring an entity type.</param>
	protected override void ConfigureAdditionalProperties(EntityTypeBuilder<SoftDeleteAuditableEntity<T>> builder)
	{
		// Configure soft delete properties
		builder.Property(x => x.IsDeleted)
			   .IsRequired()
			   .HasDefaultValue(false);

		builder.Property(x => x.Deleted)
			   .IsRequired(false);

		// Create indexes for efficient querying
		builder.HasIndex(x => x.IsDeleted);
		builder.HasIndex(x => x.Deleted);

		// Add composite index for common query patterns
		builder.HasIndex(x => new { x.IsDeleted, x.Created });
		builder.HasIndex(x => new { x.IsDeleted, x.Updated });

		// Configure query filter to exclude soft-deleted entities by default
		builder.HasQueryFilter(x => !x.IsDeleted);
	}
}