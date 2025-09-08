// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Domain;

/// <summary>
/// Represents an auditable entity with timezone-aware properties and behaviors that allow tracking of its creation, update, and deletion states.
/// </summary>
/// <remarks>
/// Implementing this interface indicates that an entity captures timezone-aware audit information related to its lifecycle events, including
/// creation, updates, and deletions using DateTimeOffset for precise timestamp tracking across different timezones.
/// </remarks>
public interface IAuditableEntity : ICreatedEntity, IUpdatedEntity, IDeletedEntity;