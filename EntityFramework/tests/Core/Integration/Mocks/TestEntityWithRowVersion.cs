// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Audit;
using Wangkanai.Foundation;

namespace Wangkanai.EntityFramework.Mocks;

public class TestEntityWithRowVersion : Entity<Guid>, IHasRowVersion
{
   public TestEntityWithRowVersion() => Id = Guid.NewGuid();

   public string  Name        { get; set; } = string.Empty;
   public string  Description { get; set; } = string.Empty;
   public byte[]? RowVersion  { get; set; }
}