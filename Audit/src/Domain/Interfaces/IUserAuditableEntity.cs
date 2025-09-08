// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Domain;

/// <summary>
/// Represents an interface that combines user-specific auditing capabilities for tracking
/// creation, modification, and deletion activities performed by users.
/// </summary>
public interface IUserAuditableEntity : IUserAuditableEntity<string>;

/// <summary>
/// Represents an interface that extends the base-auditable entity functionality by including
/// user-specific auditing capabilities for tracking changes made by users.
/// </summary>
public interface IUserAuditableEntity<T>
   : IAuditableEntity, IUserCreatedAuditableEntity<T>, IUserUpdatedAuditableEntity<T>, IUserDeletedAuditableEntity<T>
   where T : IComparable<T>, IEquatable<T>;