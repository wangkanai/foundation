# Tech Stack and Dependencies

## Framework

- **.NET 9.0** - Target framework
- **C# 13** - Latest language features
- **Nullable Reference Types** - Enabled throughout
- **File-scoped namespaces** - Required by .editorconfig
- **Implicit Usings** - Enabled for simplified namespace management

## Core Dependencies

- **Entity Framework Core 9.0** - ORM and data access
- **Microsoft.EntityFrameworkCore.Relational** - Relational database support
- **Microsoft.EntityFrameworkCore.SqlServer** - SQL Server provider
- **Microsoft.Extensions.Identity.Stores** - Identity integration

## Testing Stack

- **xunit.v3** (v2.0.3) - Primary testing framework
- **Microsoft.TestPlatform.TestHost** (v17.14.1) - Test host
- **Microsoft.NET.Test.Sdk** (v17.14.1) - .NET test SDK
- **FluentAssertions** (v8.5.0) - Assertion library
- **Moq** (v4.20.72) - Mocking framework
- **coverlet.msbuild** (v6.0.4) - Code coverage

## Development Tools

- **BenchmarkDotNet** (v0.15.2) - Performance benchmarking
- **Microsoft.SourceLink.GitHub** (v8.0.0) - Source linking
- **SonarCloud** - Code quality analysis
- **Codecov** - Coverage reporting

## Package Management

- **Central Package Management** - Enabled via Directory.Packages.props
- **Transitive Pinning** - Enabled for dependency stability

## Build System

- **MSBuild** with custom Directory.Build.props/targets
- **PowerShell** build scripts (build.ps1, sign.ps1)
- **GitHub Actions** CI/CD pipeline