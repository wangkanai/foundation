// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation.Models;

/// <summary>Base entity class for integer-keyed entities used in testing.</summary>
public abstract class KeyIntEntity : Entity<int>;

/// <summary>Base entity class for GUID-keyed entities used in testing.</summary>
public abstract class KeyGuidEntity : Entity<Guid>;