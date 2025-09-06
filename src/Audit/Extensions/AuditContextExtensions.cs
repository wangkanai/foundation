// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;

using Wangkanai.Audit.Configurations;

namespace Wangkanai.Audit;

/// <summary>Provides extension methods for configuring audit-related entity configurations in the Entity Framework model.</summary>
public static class AuditContextExtensions
{
   /// <summary>Applies audit-related configurations to the Entity Framework model.</summary>
   /// <typeparam name="TKey">The type of the primary key for the audit entity.</typeparam>
   /// <typeparam name="TUserType">The type of the user entity associated with the audit entity.</typeparam>
   /// <typeparam name="TUserKey">The type of the primary key for the user entity.</typeparam>
   /// <param name="builder">The Entity Framework <see cref="ModelBuilder"/> used to configure entity mappings.</param>
   public static void ApplyAuditConfiguration<TKey, TUserType, TUserKey>(this ModelBuilder builder)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      where TUserType : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
      => builder.ApplyConfiguration<Audit<TKey, TUserType, TUserKey>>(new AuditConfiguration<TKey, TUserType, TUserKey>());

   /// <summary>Applies user auditable entity configuration to the Entity Framework model.</summary>
   /// <typeparam name="TKey">The type of the primary key for the auditable entity.</typeparam>
   /// <param name="builder">The Entity Framework <see cref="ModelBuilder"/> used to configure entity mappings.</param>
   public static void ApplyUserAuditConfiguration<TKey>(this ModelBuilder builder)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      => builder.ApplyConfiguration(new UserAuditConfiguration<TKey>());

   /// <summary>Applies soft delete auditable entity configuration to the Entity Framework model.</summary>
   /// <typeparam name="TKey">The type of the primary key for the auditable entity.</typeparam>
   /// <param name="builder">The Entity Framework <see cref="ModelBuilder"/> used to configure entity mappings.</param>
   public static void ApplySoftDeleteAuditConfiguration<TKey>(this ModelBuilder builder)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      => builder.ApplyConfiguration(new SoftDeleteAuditConfiguration<TKey>());

   /// <summary>Applies soft delete user auditable entity configuration to the Entity Framework model.</summary>
   /// <typeparam name="TKey">The type of the primary key for the auditable entity.</typeparam>
   /// <param name="builder">The Entity Framework <see cref="ModelBuilder"/> used to configure entity mappings.</param>
   public static void ApplySoftDeleteUserAuditConfiguration<TKey>(this ModelBuilder builder)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      => builder.ApplyConfiguration(new SoftDeleteUserAuditConfiguration<TKey>());

   /// <summary>Applies all auditable entity configurations to the Entity Framework model.</summary>
   /// <typeparam name="TKey">The type of the primary key for the auditable entities.</typeparam>
   /// <param name="builder">The Entity Framework <see cref="ModelBuilder"/> used to configure entity mappings.</param>
   /// <remarks>
   /// This method applies configurations for UserAuditableEntity, SoftDeleteAuditableEntity, 
   /// and UserSoftDeleteAuditableEntity in a single call for convenience.
   /// </remarks>
   public static void ApplyAuditableEntityConfigurations<TKey>(this ModelBuilder builder)
      where TKey : IEquatable<TKey>, IComparable<TKey>
   {
      builder.ApplyUserAuditConfiguration<TKey>();
      builder.ApplySoftDeleteAuditConfiguration<TKey>();
      builder.ApplySoftDeleteUserAuditConfiguration<TKey>();
   }
}