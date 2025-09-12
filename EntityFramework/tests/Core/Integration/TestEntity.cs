// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Foundation;

namespace Wangkanai.EntityFramework;

public class TestEntity : Entity<Guid>
{
   public TestEntity() => Id = Guid.NewGuid();

   public string Name        { get; set; } = string.Empty;
   public string Description { get; set; } = string.Empty;
}