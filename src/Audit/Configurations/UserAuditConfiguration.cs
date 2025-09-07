// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.Configurations;

/// <summary>
/// Provides the configuration for the <see cref="UserAuditableEntity{TKey}"/> entity type.
/// This class is responsible for defining the model and entity relationship mappings for use with Entity Framework
/// for entities that require user tracking during create and update operations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the primary key for the auditable entity. Must implement <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.
/// </typeparam>
public class UserAuditConfiguration<TKey>
   : AuditableEntityConfiguration<UserAuditableEntity<TKey>, TKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
{
   /// <summary>Configures the additional user tracking properties for the <see cref="UserAuditableEntity{TKey}"/> class.</summary>
   /// <param name="builder">An object that provides a simple API for configuring an entity type.</param>
   protected virtual void ConfigureAdditionalProperties(EntityTypeBuilder<UserAuditableEntity<TKey>> builder)
   {
      // Configure user tracking properties
      builder.Property(x => x.CreatedBy)
             .HasMaxLength(128)
             .IsRequired(false);

      builder.Property(x => x.UpdatedBy)
             .HasMaxLength(128)
             .IsRequired(false);

      // Create indexes for efficient querying
      builder.HasIndex(x => x.CreatedBy);
      builder.HasIndex(x => x.UpdatedBy);
   }
}