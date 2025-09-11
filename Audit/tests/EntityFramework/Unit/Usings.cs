// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.AspNetCore.Identity;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;

global using FluentAssertions;
global using Moq;
global using Xunit;

global using Wangkanai.Audit;
global using Wangkanai.Audit.EntityFramework;
global using Wangkanai.Audit.EntityFramework.Repositories;