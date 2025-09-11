// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit;

/// <summary>
/// Specialized audit trail implementation with string-based keys.
/// This class reduces complexity for the most common audit trail scenario.
/// </summary>
public sealed class StringKeyTrail : Trail<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringKeyTrail"/> class.
    /// </summary>
    public StringKeyTrail() : base()
    {
        // String keys are typically UUID or GUID strings
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringKeyTrail"/> class with audit configuration.
    /// </summary>
    /// <param name="auditConfiguration">The audit configuration.</param>
    public StringKeyTrail(IAuditConfiguration auditConfiguration) : base(auditConfiguration)
    {
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates a new string-key trail for standard Identity users.
    /// </summary>
    /// <returns>A new StringKeyTrail configured for Identity users.</returns>
    public static StringKeyTrail ForIdentityUsers()
    {
        var config = AuditConfiguration<IdentityUser, string>.Create();
        return new StringKeyTrail(config.AsInterface());
    }

    /// <summary>
    /// Creates a new string-key trail for custom user types.
    /// </summary>
    /// <typeparam name="TUser">The custom user type.</typeparam>
    /// <typeparam name="TUserKey">The user key type.</typeparam>
    /// <returns>A new StringKeyTrail configured for the specified user type.</returns>
    public static StringKeyTrail ForUsers<TUser, TUserKey>()
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var config = AuditConfiguration<TUser, TUserKey>.Create();
        return new StringKeyTrail(config.AsInterface());
    }
}

/// <summary>
/// Specialized audit trail implementation with integer-based keys.
/// This class provides optimized performance for integer-keyed scenarios.
/// </summary>
public sealed class IntKeyTrail : Trail<int>
{
    private static int _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntKeyTrail"/> class.
    /// </summary>
    public IntKeyTrail() : base()
    {
        // Auto-increment ID for integer keys (use with caution in production)
        // In production scenarios, let the database handle ID generation
        Id = Interlocked.Increment(ref _nextId);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntKeyTrail"/> class with audit configuration.
    /// </summary>
    /// <param name="auditConfiguration">The audit configuration.</param>
    public IntKeyTrail(IAuditConfiguration auditConfiguration) : base(auditConfiguration)
    {
        Id = Interlocked.Increment(ref _nextId);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntKeyTrail"/> class with explicit ID.
    /// </summary>
    /// <param name="id">The explicit ID to use.</param>
    /// <param name="auditConfiguration">The audit configuration.</param>
    public IntKeyTrail(int id, IAuditConfiguration? auditConfiguration = null) : base(auditConfiguration)
    {
        Id = id;
    }

    /// <summary>
    /// Creates a new integer-key trail for standard Identity users.
    /// </summary>
    /// <returns>A new IntKeyTrail configured for Identity users.</returns>
    public static IntKeyTrail ForIdentityUsers()
    {
        var config = AuditConfiguration<IdentityUser, string>.Create();
        return new IntKeyTrail(config.AsInterface());
    }

    /// <summary>
    /// Creates a new integer-key trail for integer-keyed users.
    /// </summary>
    /// <typeparam name="TUser">The user type.</typeparam>
    /// <returns>A new IntKeyTrail configured for integer-keyed users.</returns>
    public static IntKeyTrail ForIntUsers<TUser>()
        where TUser : IdentityUser<int>
    {
        var config = AuditConfiguration<TUser, int>.Create();
        return new IntKeyTrail(config.AsInterface());
    }

    /// <summary>
    /// Resets the internal ID counter (use only in testing scenarios).
    /// </summary>
    /// <param name="startValue">The value to reset the counter to.</param>
    public static void ResetIdCounter(int startValue = 1)
    {
        Interlocked.Exchange(ref _nextId, startValue);
    }
}

/// <summary>
/// Specialized audit trail implementation with GUID-based keys.
/// This class provides the most robust key generation for distributed scenarios.
/// </summary>
public sealed class GuidKeyTrail : Trail<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidKeyTrail"/> class.
    /// </summary>
    public GuidKeyTrail() : base()
    {
        // GUID keys are automatically generated
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GuidKeyTrail"/> class with audit configuration.
    /// </summary>
    /// <param name="auditConfiguration">The audit configuration.</param>
    public GuidKeyTrail(IAuditConfiguration auditConfiguration) : base(auditConfiguration)
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GuidKeyTrail"/> class with explicit ID.
    /// </summary>
    /// <param name="id">The explicit GUID to use.</param>
    /// <param name="auditConfiguration">The audit configuration.</param>
    public GuidKeyTrail(Guid id, IAuditConfiguration? auditConfiguration = null) : base(auditConfiguration)
    {
        Id = id;
    }

    /// <summary>
    /// Creates a new GUID-key trail for standard Identity users.
    /// </summary>
    /// <returns>A new GuidKeyTrail configured for Identity users.</returns>
    public static GuidKeyTrail ForIdentityUsers()
    {
        var config = AuditConfiguration<IdentityUser, string>.Create();
        return new GuidKeyTrail(config.AsInterface());
    }

    /// <summary>
    /// Creates a new GUID-key trail for GUID-keyed users.
    /// </summary>
    /// <typeparam name="TUser">The user type.</typeparam>
    /// <returns>A new GuidKeyTrail configured for GUID-keyed users.</returns>
    public static GuidKeyTrail ForGuidUsers<TUser>()
        where TUser : IdentityUser<Guid>
    {
        var config = AuditConfiguration<TUser, Guid>.Create();
        return new GuidKeyTrail(config.AsInterface());
    }

    /// <summary>
    /// Creates a deterministic GUID from a seed value (useful for testing).
    /// </summary>
    /// <param name="seed">The seed value to generate a deterministic GUID.</param>
    /// <param name="auditConfiguration">The audit configuration.</param>
    /// <returns>A new GuidKeyTrail with a deterministic ID.</returns>
    public static GuidKeyTrail CreateDeterministic(int seed, IAuditConfiguration? auditConfiguration = null)
    {
        // Create a deterministic GUID from seed for testing purposes
        var bytes = new byte[16];
        BitConverter.GetBytes(seed).CopyTo(bytes, 0);
        var deterministicGuid = new Guid(bytes);
        
        return new GuidKeyTrail(deterministicGuid, auditConfiguration);
    }
}

/// <summary>
/// Extension methods for specialized trail types to provide fluent API.
/// </summary>
public static class SpecializedTrailExtensions
{
    /// <summary>
    /// Sets common audit properties in a fluent manner.
    /// </summary>
    /// <typeparam name="T">The trail key type.</typeparam>
    /// <param name="trail">The trail instance.</param>
    /// <param name="trailType">The type of audit action.</param>
    /// <param name="entityName">The name of the entity being audited.</param>
    /// <param name="primaryKey">The primary key of the audited entity.</param>
    /// <returns>The trail instance for fluent chaining.</returns>
    public static Trail<T> WithAuditInfo<T>(
        this Trail<T> trail, 
        TrailType trailType, 
        string entityName, 
        string? primaryKey = null)
        where T : IEquatable<T>, IComparable<T>
    {
        trail.TrailType = trailType;
        trail.EntityName = entityName;
        trail.PrimaryKey = primaryKey;
        trail.Timestamp = DateTime.UtcNow;
        
        return trail;
    }

    /// <summary>
    /// Sets user information in a fluent manner.
    /// </summary>
    /// <typeparam name="T">The trail key type.</typeparam>
    /// <typeparam name="TUser">The user type.</typeparam>
    /// <param name="trail">The trail instance.</param>
    /// <param name="user">The user performing the action.</param>
    /// <returns>The trail instance for fluent chaining.</returns>
    public static Trail<T> WithUser<T, TUser>(this Trail<T> trail, TUser? user)
        where T : IEquatable<T>, IComparable<T>
        where TUser : class
    {
        trail.SetUser(user);
        return trail;
    }

    /// <summary>
    /// Sets user ID information in a fluent manner.
    /// </summary>
    /// <typeparam name="T">The trail key type.</typeparam>
    /// <typeparam name="TUserKey">The user key type.</typeparam>
    /// <param name="trail">The trail instance.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The trail instance for fluent chaining.</returns>
    public static Trail<T> WithUserId<T, TUserKey>(this Trail<T> trail, TUserKey? userId)
        where T : IEquatable<T>, IComparable<T>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        trail.SetUserId(userId);
        return trail;
    }

    /// <summary>
    /// Sets changed values in a fluent manner.
    /// </summary>
    /// <typeparam name="T">The trail key type.</typeparam>
    /// <param name="trail">The trail instance.</param>
    /// <param name="oldValues">Dictionary of old values.</param>
    /// <param name="newValues">Dictionary of new values.</param>
    /// <returns>The trail instance for fluent chaining.</returns>
    public static Trail<T> WithChanges<T>(
        this Trail<T> trail, 
        Dictionary<string, object>? oldValues = null, 
        Dictionary<string, object>? newValues = null)
        where T : IEquatable<T>, IComparable<T>
    {
        if (oldValues != null)
            trail.OldValues = oldValues;
            
        if (newValues != null)
            trail.NewValues = newValues;
            
        return trail;
    }
}