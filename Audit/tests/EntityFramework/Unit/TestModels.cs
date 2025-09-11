// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit.EntityFramework.Tests;

/// <summary>Test user class for testing audit functionality.</summary>
public class TestUser : IdentityUser<string>
{
	/// <summary>Initializes a new instance of the TestUser class.</summary>
	public TestUser()
	{
		Id = Guid.NewGuid().ToString();
		UserName = "TestUser";
		Email = "test@example.com";
	}

	/// <summary>Initializes a new instance of the TestUser class with the specified username.</summary>
	/// <param name="userName">The username for the test user.</param>
	public TestUser(string userName) : this()
	{
		UserName = userName;
		Email = $"{userName}@example.com";
	}
}