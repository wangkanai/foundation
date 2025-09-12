// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Foundation;

namespace Wangkanai.EntityFramework;

public class TestAddress : ValueObject
{
   public string Street  { get; set; }
   public string City    { get; set; }
   public string State   { get; set; }
   public string ZipCode { get; set; }

   public TestAddress(string street, string city, string state, string zipCode)
   {
      Street  = street;
      City    = city;
      State   = state;
      ZipCode = zipCode;
   }

   // Required for EF Core
   private TestAddress() : this(string.Empty, string.Empty, string.Empty, string.Empty) { }
}