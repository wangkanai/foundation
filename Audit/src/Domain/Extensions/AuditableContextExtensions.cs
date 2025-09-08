// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;

using Wangkanai.Audit.Configurations;

namespace Wangkanai.Audit;

/// <summary>
/// Provides extension methods for configuring audit-related entity configurations in the Entity Framework model.
/// </summary>
public static class AuditableContextExtensions
{
   /// <summary>Applies all auditable entity configurations to the Entity Framework model.</summary>
   /// <typeparam name="TKey">The type of the primary key for the auditable entities.</typeparam>
   /// <param name="builder">The Entity Framework <see cref="ModelBuilder"/> used to configure entity mappings.</param>
   /// <remarks>
   /// This method applies configurations for UserAuditableEntity, SoftDeleteAuditableEntity,
   /// and UserSoftDeleteAuditableEntity in a single call for convenience.
   /// </remarks>
   public static void ApplyAuditableConfiguration<TKey, TUserType, TUserKey>(this ModelBuilder builder)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      where TUserType : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
   {
      builder.ApplyConfiguration(new TrailConfiguration<TKey, TUserType, TUserKey>());
      // builder.ApplyConfiguration(new UserAuditConfiguration<TKey>());
   }
}