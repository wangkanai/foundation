// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

public interface IUserAuditableEntity : ICreatedEntity, IUpdatedEntity, IDeletedEntity
{
   string? CreatedBy { get; set; }

   string? UpdatedBy { get; set; }

   string? DeletedBy { get; set; }
}