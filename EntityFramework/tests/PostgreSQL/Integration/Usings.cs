// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Npgsql;
global using Npgsql.EntityFrameworkCore.PostgreSQL;

global using FluentAssertions;

global using Testcontainers.PostgreSql;

global using Wangkanai.EntityFramework.Postgres;
global using Wangkanai.Foundation;

global using Xunit;
