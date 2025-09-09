// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Foundation;

namespace Wangkanai.Audit;

/// <summary>
/// Defines an auditable entity interface providing fundamental functionality for tracking lifecycle events
/// such as creation, modification, and deletion.
/// </summary>
/// <remarks>
/// This interface extends a generic auditable entity interface with a default identifier type of <see cref="string"/>.
/// It integrates capabilities for capturing audit-related metadata for entities, enabling monitoring and logging
/// of their creation, updates, and deletions.
/// </remarks>
public interface IAuditableEntity : IAuditableEntity<string>;

/// <summary>
/// Represents an auditable entity with timezone-aware properties and behaviors that allow tracking of its creation, update, and deletion states.
/// </summary>
/// <remarks>
/// Implementing this interface indicates that an entity captures timezone-aware audit information related to its lifecycle events, including
/// creation, updates, and deletions using DateTimeOffset for precise timestamp tracking across different timezones.
/// </remarks>
public interface IAuditableEntity<T> : ICreatedEntity, IUpdatedEntity, IDeletedEntity
   where T : IComparable<T>, IEquatable<T>;