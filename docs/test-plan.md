# Test Structure Implementation Plan

## Executive Summary

This document outlines the comprehensive plan to implement a hybrid test structure across all modules in the Foundation solution.
The new structure combines layer-based organization with test-type categorization, providing both architectural clarity and
execution flexibility.

## Current State

```
Foundation/tests/Domain/
Foundation/tests/Application/
Audit/tests/Domain/
EntityFramework/tests/Core/
```

## Target State

```
[Module]/tests/
  Domain/
    Unit/           # Pure domain logic tests
    Integration/    # Domain with persistence tests
  Application/
    Unit/          # Service logic tests
    Functional/    # Use case scenario tests
  Infrastructure/
    Integration/   # Real database/external service tests
```

## Implementation Phases

### Phase 1: Foundation Module Setup

**Timeline**: Sprint 1 (Days 1-2)
**Priority**: High

#### Tasks

- [ ] Create directory structure for Foundation/tests
   - [ ] Domain/Unit
   - [ ] Domain/Integration
   - [ ] Application/Unit
   - [ ] Application/Functional
   - [ ] Infrastructure/Integration
- [ ] Move existing Domain tests to Domain/Unit
- [ ] Move existing Application tests to Application/Unit
- [ ] Update project references
- [ ] Add test categorization attributes

### Phase 2: Audit Module Setup

**Timeline**: Sprint 1 (Days 2-3)
**Priority**: High

#### Tasks

- [ ] Create directory structure for Audit/tests
   - [ ] Domain/Unit
   - [ ] Domain/Integration
   - [ ] Application/Unit
   - [ ] Application/Functional
   - [ ] Infrastructure/Integration
- [ ] Create initial test projects
- [ ] Configure project references
- [ ] Add sample tests for each category

### Phase 3: EntityFramework Module Setup

**Timeline**: Sprint 1 (Days 3-4)
**Priority**: High

#### Tasks

- [ ] Create directory structure for EntityFramework/tests
   - [ ] Core/Unit
   - [ ] Core/Integration
   - [ ] PostgreSQL/Integration
   - [ ] SqlServer/Integration
- [ ] Move existing tests to appropriate folders
- [ ] Update project configurations
- [ ] Add database-specific integration tests

### Phase 4: Test Infrastructure

**Timeline**: Sprint 1 (Days 4-5)
**Priority**: Medium

#### Tasks

- [ ] Create shared test utilities project
   - [ ] TestBase classes for each test type
   - [ ] Database fixtures for integration tests
   - [ ] Mock builders for unit tests
   - [ ] Test data generators
- [ ] Implement test categorization system
   - [ ] Define standard test traits
   - [ ] Create attribute classes
   - [ ] Document usage patterns

### Phase 5: CI/CD Integration

**Timeline**: Sprint 1 (Day 6)
**Priority**: Medium

#### Tasks

- [ ] Update build pipeline
   - [ ] Add test filtering by category
   - [ ] Configure parallel execution for unit tests
   - [ ] Set up database containers for integration tests
- [ ] Create test execution profiles
   - [ ] Fast feedback (unit only)
   - [ ] PR validation (unit + functional)
   - [ ] Full validation (all tests)
- [ ] Configure test reporting
   - [ ] Coverage reports per category
   - [ ] Performance metrics
   - [ ] Failure analysis

## Test Categorization System

### Standard Test Traits

```csharp
[Trait("Category", "Unit")]        // Test type
[Trait("Layer", "Domain")]         // Architectural layer
[Trait("Performance", "Fast")]     // Execution speed
[Trait("Database", "Required")]    // External dependencies
```

### Test Categories

#### Unit Tests

- **Characteristics**: No external dependencies, fast execution (<100ms)
- **Scope**: Single class or small unit of code
- **Mocking**: Heavy use of mocks and stubs
- **Example**: Entity validation, value object equality

#### Functional Tests

- **Characteristics**: May use in-memory databases, moderate speed
- **Scope**: Complete use case or feature
- **Mocking**: Minimal, prefer real implementations
- **Example**: Application service workflows

#### Integration Tests

- **Characteristics**: Real external dependencies, slower execution
- **Scope**: System boundaries and external integrations
- **Mocking**: No mocking of tested components
- **Example**: Database operations, API calls

## Directory Structure Details

### Foundation Module

```
Foundation/
  tests/
    Domain/
      Unit/
        Wangkanai.Foundation.Domain.UnitTests.csproj
        Entities/
          EntityTests.cs
        ValueObjects/
          ValueObjectTests.cs
      Integration/
        Wangkanai.Foundation.Domain.IntegrationTests.csproj
        Repositories/
          RepositoryTests.cs
    Application/
      Unit/
        Wangkanai.Foundation.Application.UnitTests.csproj
        Services/
          ServiceTests.cs
      Functional/
        Wangkanai.Foundation.Application.FunctionalTests.csproj
        UseCases/
          CreateEntityTests.cs
    Infrastructure/
      Integration/
        Wangkanai.Foundation.Infrastructure.IntegrationTests.csproj
        Database/
          DbContextTests.cs
```

### Audit Module

```
Audit/
  tests/
    Domain/
      Unit/
        Wangkanai.Audit.Domain.UnitTests.csproj
        Entities/
          AuditableEntityTests.cs
          TrailTests.cs
      Integration/
        Wangkanai.Audit.Domain.IntegrationTests.csproj
    Application/
      Unit/
        Wangkanai.Audit.Application.UnitTests.csproj
        Configurations/
          AuditConfigurationTests.cs
      Functional/
        Wangkanai.Audit.Application.FunctionalTests.csproj
        Scenarios/
          AuditTrailScenarioTests.cs
    Infrastructure/
      Integration/
        Wangkanai.Audit.Infrastructure.IntegrationTests.csproj
        Stores/
          TrailStoreTests.cs
```

### EntityFramework Module

```
EntityFramework/
  tests/
    Core/
      Unit/
        Wangkanai.EntityFramework.Core.UnitTests.csproj
        Extensions/
          DbContextExtensionsTests.cs
      Integration/
        Wangkanai.EntityFramework.Core.IntegrationTests.csproj
        Migrations/
          MigrationTests.cs
    PostgreSQL/
      Integration/
        Wangkanai.EntityFramework.PostgreSQL.IntegrationTests.csproj
        PostgreSQLContextTests.cs
    SqlServer/
      Integration/
        Wangkanai.EntityFramework.SqlServer.IntegrationTests.csproj
        SqlServerContextTests.cs
```

## Execution Commands

### Development Commands

```bash
# Run only unit tests (fast feedback)
dotnet test --filter "Category=Unit"

# Run tests for specific layer
dotnet test --filter "Layer=Domain"

# Run tests for specific module
dotnet test Foundation/tests --filter "Category=Unit"

# Run all tests except integration
dotnet test --filter "Category!=Integration"
```

### CI/CD Pipeline Commands

```bash
# PR Validation
dotnet test --filter "Category=Unit|Category=Functional" --logger "trx"

# Nightly Build
dotnet test --logger "trx" --collect:"XPlat Code Coverage"

# Release Validation
dotnet test --configuration Release --no-build
```

## Success Metrics

### Phase 1-3 Success Criteria

- [x] All existing tests migrated to new structure
- [x] No test failures after migration
- [x] Project references properly configured
- [x] Test discovery working in IDE

### Phase 4 Success Criteria

- [x] Shared utilities reduce code duplication by >30%
- [x] Test traits consistently applied
- [x] Clear documentation for test patterns

### Phase 5 Success Criteria

- [x] Unit tests complete in <30 seconds
- [x] Integration tests properly isolated
- [x] CI/CD pipeline optimized for feedback speed
- [x] Test reports accessible and actionable

## Risk Mitigation

### Identified Risks

1. **Test Discovery Issues**
   - Mitigation: Maintain clear naming conventions
   - Fallback: Use explicit test runners

2. **Project Reference Complexity**
   - Mitigation: Document reference diagram
   - Fallback: Simplify to fewer test projects

3. **Migration Effort**
   - Mitigation: Automate with scripts
   - Fallback: Gradual migration approach

## Rollback Plan

If issues arise during implementation:

1. Git branches preserve original structure
2. Revert to layer-based organization
3. Apply test traits without restructuring

## Appendices

### A. Test Project Template

```xml

<Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup>
      <IsPackable>false</IsPackable>
      <IsTestProject>true</IsTestProject>
   </PropertyGroup>

   <ItemGroup>
      <PackageReference Include="Microsoft.NET.Test.Sdk"/>
      <PackageReference Include="xunit"/>
      <PackageReference Include="xunit.runner.visualstudio"/>
      <PackageReference Include="Moq"/>
      <PackageReference Include="FluentAssertions"/>
   </ItemGroup>
</Project>
```

### B. Test Base Class Template

```csharp
public abstract class UnitTestBase
{
    protected Mock<T> CreateMock<T>() where T : class
        => new Mock<T>();
}

public abstract class IntegrationTestBase : IDisposable
{
    protected readonly IServiceProvider ServiceProvider;

    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    protected abstract void ConfigureServices(IServiceCollection services);

    public void Dispose()
        => (ServiceProvider as IDisposable)?.Dispose();
}
```

### C. GitHub Actions Workflow Template

```yaml
name: Test Suite

on: [ push, pull_request ]

jobs:
   unit-tests:
      runs-on: ubuntu-latest
      steps:
         -  uses: actions/checkout@v3
         -  uses: actions/setup-dotnet@v3
         -  run: dotnet test --filter Category=Unit

   integration-tests:
      runs-on: ubuntu-latest
      services:
         postgres:
            image: postgres:15
         sqlserver:
            image: mcr.microsoft.com/mssql/server:2022-latest
      steps:
         -  uses: actions/checkout@v3
         -  uses: actions/setup-dotnet@v3
         -  run: dotnet test --filter Category=Integration
```

## Revision History

- v1.0 - Initial plan creation
- v1.1 - Added detailed directory structures
- v1.2 - Added CI/CD integration details

---
*Last Updated: 2025-01-09*
*Status: Ready for Implementation*