// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity that includes comprehensive soft delete functionality.</summary>
/// <remarks>
/// This interface combines the functionality of <see cref="IAuditable"/>, <see cref="ISoftDeletable"/>, and
/// <see cref="IDeletedEntity"/> to provide a complete structure for auditing entities with soft delete capabilities.
/// It tracks creation, update, and deletion timestamps along with the soft delete state.
/// </remarks>
public interface ISoftDeleteAuditable
   : IAuditable, ISoftDeletable, IDeletedEntity;