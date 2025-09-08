// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Domain;

/// <summary>Represents an entity that supports tracking its deletable state.</summary>
/// <remarks>
/// This interface is designed for entities that require audit capabilities for deletion operations.
/// It extends the <see cref="IDeletedEntity"/> interface, inheriting the contract for capturing and storing the
/// deletion timestamp, which is often used in soft delete mechanisms.
/// </remarks>
public interface IDeletable : IDeletedEntity;