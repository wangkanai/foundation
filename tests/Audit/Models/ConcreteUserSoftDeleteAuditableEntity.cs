// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Models;

public class ConcreteUserSoftDeleteAuditableEntity
   : UserSoftDeleteAuditableEntity<Guid>
{
   public ConcreteUserSoftDeleteAuditableEntity() => Id = Guid.NewGuid();
}