// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit;

/// <summary>
/// Factory class for creating Trail instances with simplified generic complexity.
/// This factory pattern hides the complexity of audit configuration setup and provides
/// convenient methods for common scenarios.
/// </summary>
public static class TrailFactory
{
    /// <summary>
    /// Creates a new audit trail with the specified key type and user configuration.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A new Trail instance configured for the specified types.</returns>
    public static Trail<TKey> Create<TKey, TUser, TUserKey>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var config = AuditConfiguration<TUser, TUserKey>.Create();
        return new Trail<TKey>(config.AsInterface());
    }

    /// <summary>
    /// Creates a new audit trail with explicit audit configuration.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <param name="auditConfiguration">The audit configuration to use.</param>
    /// <returns>A new Trail instance with the specified configuration.</returns>
    public static Trail<TKey> Create<TKey>(IAuditConfiguration auditConfiguration)
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return new Trail<TKey>(auditConfiguration);
    }

    /// <summary>
    /// Creates a new audit trail without user type constraints (basic trail).
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <returns>A new Trail instance without audit configuration.</returns>
    public static Trail<TKey> CreateBasic<TKey>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return new Trail<TKey>();
    }

    #region Specialized Factory Methods for Common Scenarios

    /// <summary>
    /// Creates a new audit trail with string-based key and specified user types.
    /// </summary>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A new string-keyed Trail instance.</returns>
    public static Trail<string> CreateStringKey<TUser, TUserKey>()
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        return Create<string, TUser, TUserKey>();
    }

    /// <summary>
    /// Creates a new audit trail with integer-based key and specified user types.
    /// </summary>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A new integer-keyed Trail instance.</returns>
    public static Trail<int> CreateIntKey<TUser, TUserKey>()
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        return Create<int, TUser, TUserKey>();
    }

    /// <summary>
    /// Creates a new audit trail with GUID-based key and specified user types.
    /// </summary>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A new GUID-keyed Trail instance.</returns>
    public static Trail<Guid> CreateGuidKey<TUser, TUserKey>()
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        return Create<Guid, TUser, TUserKey>();
    }

    /// <summary>
    /// Creates a new audit trail for standard ASP.NET Identity users (string-based user key).
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <returns>A new Trail instance configured for standard Identity users.</returns>
    public static Trail<TKey> CreateForIdentityUser<TKey>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return Create<TKey, IdentityUser, string>();
    }

    /// <summary>
    /// Creates a new audit trail for custom Identity users with GUID keys.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The custom user type.</typeparam>
    /// <returns>A new Trail instance configured for GUID-keyed users.</returns>
    public static Trail<TKey> CreateForGuidUser<TKey, TUser>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<Guid>
    {
        return Create<TKey, TUser, Guid>();
    }

    /// <summary>
    /// Creates a new audit trail for custom Identity users with integer keys.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The custom user type.</typeparam>
    /// <returns>A new Trail instance configured for integer-keyed users.</returns>
    public static Trail<TKey> CreateForIntUser<TKey, TUser>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<int>
    {
        return Create<TKey, TUser, int>();
    }

    #endregion

    #region Backward Compatibility Support

    /// <summary>
    /// Creates a trail instance that maintains compatibility with the old three-generic-parameter approach.
    /// This method is intended for migration scenarios and will be marked obsolete in future versions.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <param name="userId">The user ID to set on the trail.</param>
    /// <param name="user">The user to set on the trail.</param>
    /// <returns>A new Trail instance configured with the provided user information.</returns>
    public static Trail<TKey> CreateCompatibility<TKey, TUser, TUserKey>(
        TUserKey? userId = default, 
        TUser? user = default)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var trail = Create<TKey, TUser, TUserKey>();
        
        if (userId != null)
            trail.SetUserId(userId);
        
        if (user != null)
            trail.SetUser(user);

        return trail;
    }

    #endregion

    #region Configuration Helpers

    /// <summary>
    /// Creates an audit configuration for the specified user types.
    /// </summary>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>An audit configuration instance.</returns>
    public static AuditConfiguration<TUser, TUserKey> CreateConfiguration<TUser, TUserKey>()
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        return AuditConfiguration<TUser, TUserKey>.Create();
    }

    /// <summary>
    /// Creates multiple trail instances with the same configuration for batch operations.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <param name="count">The number of trail instances to create.</param>
    /// <returns>An array of Trail instances with shared configuration.</returns>
    public static Trail<TKey>[] CreateBatch<TKey, TUser, TUserKey>(int count)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than zero.", nameof(count));

        var config = AuditConfiguration<TUser, TUserKey>.Create().AsInterface();
        var trails = new Trail<TKey>[count];
        
        for (var i = 0; i < count; i++)
        {
            trails[i] = new Trail<TKey>(config);
        }

        return trails;
    }

    #endregion
}