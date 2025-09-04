// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Models;

public class ConcreteSoftDeleteAuditableEntity
   : SoftDeleteAuditableEntity<Guid>
{
   public ConcreteSoftDeleteAuditableEntity() => Id = Guid.NewGuid();
}