// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>Enumeration representing the state of an entity in the context of a data store.</summary>
[Flags]
public enum EntryState
{
   Detached  = 1 << 0,
   Unchanged = 1 << 1,
   Added     = 1 << 2,
   Deleted   = 1 << 3,
   Modified  = 1 << 4
}