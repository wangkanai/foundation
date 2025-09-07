// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity that includes comprehensive soft delete functionality.</summary>
public interface ISoftDeleteAuditableEntity : IAuditableEntity, ISoftDeletable;