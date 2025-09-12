// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Foundation;

namespace Wangkanai.EntityFramework.Mocks;

public class TestEntityWithValueObject : Entity<Guid>
{
   public TestEntityWithValueObject()
   {
      Id = Guid.NewGuid();
   }

   public string       Name    { get; set; } = string.Empty;
   public TestAddress? Address { get; set; }
}