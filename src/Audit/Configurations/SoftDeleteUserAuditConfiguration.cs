// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.Configurations;

/// <summary>
/// Provides the configuration for the <see cref="UserSoftDeleteAuditableEntity{TKey}"/> entity type.
/// This class is responsible for defining the model and entity relationship mappings for use with Entity Framework
/// for entities that require comprehensive audit tracking with user information and soft delete capabilities.
/// </summary>
/// <typeparam name="TKey">
/// The type of the primary key for the auditable entity. Must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
/// </typeparam>
public class SoftDeleteUserAuditConfiguration<TKey>
   : AuditableEntityConfiguration<UserSoftDeleteAuditableEntity<TKey>, TKey>
	where TKey : IEquatable<TKey>, IComparable<TKey>
{
	/// <summary>Configures all properties for the <see cref="UserSoftDeleteAuditableEntity{TKey}"/> class.</summary>
	/// <param name="builder">An object that provides a simple API for configuring an entity type.</param>
	protected override void ConfigureAdditionalProperties(EntityTypeBuilder<UserSoftDeleteAuditableEntity<TKey>> builder)
	{
		// Configure user tracking properties
		builder.Property(x => x.CreatedBy)
			   .HasMaxLength(128)
			   .IsRequired(false);

		builder.Property(x => x.UpdatedBy)
			   .HasMaxLength(128)
			   .IsRequired(false);

		// Configure soft delete properties
		builder.Property(x => x.IsDeleted)
			   .IsRequired()
			   .HasDefaultValue(false);

		builder.Property(x => x.Deleted)
			   .IsRequired(false);

		builder.Property(x => x.DeletedBy)
			   .HasMaxLength(128)
			   .IsRequired(false);

		// Create indexes for efficient querying
		builder.HasIndex(x => x.CreatedBy);
		builder.HasIndex(x => x.UpdatedBy);
		builder.HasIndex(x => x.DeletedBy);
		builder.HasIndex(x => x.IsDeleted);
		builder.HasIndex(x => x.Deleted);

		// Add composite indexes for common query patterns
		builder.HasIndex(x => new { x.IsDeleted, x.Created });
		builder.HasIndex(x => new { x.IsDeleted, x.Updated });
		builder.HasIndex(x => new { x.IsDeleted, x.CreatedBy });
		builder.HasIndex(x => new { x.IsDeleted, x.UpdatedBy });
		builder.HasIndex(x => new { x.IsDeleted, x.DeletedBy });

		// Configure query filter to exclude soft-deleted entities by default
		builder.HasQueryFilter(x => !x.IsDeleted);
	}
}