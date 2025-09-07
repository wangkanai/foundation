// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>
/// Represents an auditable entity with properties and behaviors that allow tracking of its creation, update, and deletion states.
/// </summary>
/// <remarks>
/// Implementing this interface indicates that an entity captures audit information related to its lifecycle events, including
/// creation, updates, and deletions.
/// </remarks>
public interface IAuditableEntity : ICreatedEntity, IUpdatedEntity, IDeletedEntity;