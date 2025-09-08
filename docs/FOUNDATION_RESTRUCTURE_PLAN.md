# Wangkanai Foundation Restructuring Plan

## 🎯 **Objective**

Restructure the current repository from flat `Wangkanai.*` naming to hierarchical `Wangkanai.Foundation.*` pattern, following the
established Federation repository architecture.

## 📊 **Current State Analysis**

### Current Package Structure

```
Wangkanai.Domain (v5.0.0)        → Core DDD patterns, events
Wangkanai.Audit (v0.3.0)         → Audit trail functionality
Wangkanai.EntityFramework (v3.7.0) → EF Core integrations
```

### Dependencies Mapping

```
Domain: Standalone (Microsoft.Extensions.Hosting, EF, etc.)
Audit: Depends on Domain
EntityFramework: Depends on Audit
```

### Key Constraints

- ✅ Only used in 1 project (safe to break)
- ⚠️ `Wangkanai.Hosting` already exists (2.2M downloads)
- 🎯 Must resolve Issue #50 (IEventListener coupling)

---

## 🚀 **Target Architecture**

### New Package Structure

```
Wangkanai.Foundation                    → Metapackage (v1.0.0)
├─ Wangkanai.Foundation.Domain          → Core DDD patterns (v1.0.0)
├─ Wangkanai.Foundation.Audit           → Audit functionality (v1.0.0)
├─ Wangkanai.Foundation.EntityFramework → EF integrations (v1.0.0)
└─ Wangkanai.Foundation.Events          → Event infrastructure (v1.0.0) [NEW]
```

### Dependency Flow

```
Foundation → References all sub-packages
Foundation.Domain → Standalone (pure DDD)
Foundation.Audit → Depends on Foundation.Domain
Foundation.EntityFramework → Depends on Foundation.Audit
Foundation.Events → Depends on Foundation.Domain + Microsoft.Extensions.Hosting
```

---

## 📋 **Implementation Phases**

## **Phase 1: Preparation & Backup** ⏱️ 30 minutes

### 1.1 Create Safety Backup

```bash
# Create backup branch
git checkout -b backup/pre-foundation-restructure
git push -u origin backup/pre-foundation-restructure

# Tag current state
git tag -a v5.0.0-pre-foundation -m "Pre-Foundation restructure snapshot"
git push origin v5.0.0-pre-foundation
```

### 1.2 Create Working Branch

```bash
git checkout main
git checkout -b feature/foundation-restructure
```

### 1.3 Document Current State

- [x] Current package versions documented
- [x] Dependency mapping completed
- [x] File structure analysis done

---

## **Phase 2: Project Structure Creation** ⏱️ 45 minutes

### 2.1 Create New Foundation Directory Structure

```bash
mkdir -p src/Foundation/Domain
mkdir -p src/Foundation/Audit
mkdir -p src/Foundation/EntityFramework
mkdir -p src/Foundation/Events
mkdir -p src/Foundation/Metapackage
```

### 2.2 Create New Project Files

- `src/Foundation/Domain/Wangkanai.Foundation.Domain.csproj`
- `src/Foundation/Audit/Wangkanai.Foundation.Audit.csproj`
- `src/Foundation/EntityFramework/Wangkanai.Foundation.EntityFramework.csproj`
- `src/Foundation/Events/Wangkanai.Foundation.Events.csproj`
- `src/Foundation/Metapackage/Wangkanai.Foundation.csproj`

### 2.3 Update Solution File

- Add new projects to `Domain.slnx`
- Organize into Foundation folder structure

---

## **Phase 3: Content Migration** ⏱️ 2 hours

### 3.1 Migrate Domain Content

**Source**: `src/Domain/` → **Target**: `src/Foundation/Domain/`

**Files to Move**:

```
✅ Core abstractions: Entity.cs, ValueObject.cs
✅ Interfaces: All I*.cs files
✅ Primitives: Error.cs, Result.cs, etc.
✅ Extensions: ReflectionExtensions.cs, etc.
✅ Configurations: EntityConfiguration.cs, etc.
⚠️ Events: Move to Foundation.Events (Issue #50 separation)
⚠️ Infrastructure: Move hosting-related to Foundation.Events
```

**Namespace Changes**:

```csharp
// Before
namespace Wangkanai.Domain;

// After
namespace Wangkanai.Foundation.Domain;
```

### 3.2 Migrate Audit Content

**Source**: `src/Audit/` → **Target**: `src/Foundation/Audit/`

**Files to Move**:

```
✅ All current audit files
✅ Update project references to Foundation.Domain
✅ Update namespaces to Wangkanai.Foundation.Audit
```

### 3.3 Migrate EntityFramework Content

**Source**: `src/EntityFramework/` → **Target**: `src/Foundation/EntityFramework/`

**Files to Move**:

```
✅ All current EF files
✅ Update project references to Foundation.Audit
✅ Update namespaces to Wangkanai.Foundation.EntityFramework
```

### 3.4 Create Events Package (Issue #50 Resolution)

**Target**: `src/Foundation/Events/`

**Content to Create**:

```csharp
// Pure domain interface (stays in Foundation.Domain)
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

// Infrastructure wrapper (new in Foundation.Events)
public interface IEventListenerService<TEvent, TAction> : IHostedService
    where TEvent : IDomainEvent
{
    Task ProcessEventAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

public class EventListenerService<TEvent, TAction> : IEventListenerService<TEvent, TAction>
    where TEvent : IDomainEvent
{
    // Implementation with IHostedService integration
}
```

---

## **Phase 4: Test Migration** ⏱️ 1 hour

### 4.1 Create New Test Structure

```bash
mkdir -p tests/Foundation/Domain
mkdir -p tests/Foundation/Audit
mkdir -p tests/Foundation/EntityFramework
mkdir -p tests/Foundation/Events
```

### 4.2 Migrate Test Projects

- Update test project names and references
- Update namespaces in test files
- Ensure all tests pass with new structure

### 4.3 Update Benchmark Projects

- Migrate benchmark projects to new structure
- Update references and namespaces

---

## **Phase 5: Solution Configuration** ⏱️ 30 minutes

### 5.1 Update Solution Files

- Update `Domain.slnx` with new project structure
- Update `Directory.Build.props` with new package metadata
- Update `Directory.Packages.props` if needed

### 5.2 Update Build Scripts

- Update `build.ps1` for new package names
- Update `sign.ps1` for new package names
- Update GitHub workflows

### 5.3 Update Documentation

- Update `README.md` with new package names
- Update `CLAUDE.md` architecture section
- Create migration guide for consumers

---

## **Phase 6: Package Configuration** ⏱️ 45 minutes

### 6.1 Configure Metapackage

```xml

<Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup>
      <VersionPrefix>1.0.0</VersionPrefix>
      <Product>Wangkanai Foundation</Product>
      <PackageTags>wangkanai;foundation;ddd;domain;audit;entityframework</PackageTags>
      <Description>Complete DDD foundation package including Domain, Audit, EntityFramework, and Events</Description>
   </PropertyGroup>
   <ItemGroup>
      <ProjectReference Include="../Domain/Wangkanai.Foundation.Domain.csproj"/>
      <ProjectReference Include="../Audit/Wangkanai.Foundation.Audit.csproj"/>
      <ProjectReference Include="../EntityFramework/Wangkanai.Foundation.EntityFramework.csproj"/>
      <ProjectReference Include="../Events/Wangkanai.Foundation.Events.csproj"/>
   </ItemGroup>
</Project>
```

### 6.2 Configure Individual Packages

- Set all packages to v1.0.0 (clean slate)
- Update package descriptions and tags
- Configure proper dependencies

---

## **Phase 7: Validation & Testing** ⏱️ 1 hour

### 7.1 Build Verification

```bash
dotnet clean
dotnet restore
dotnet build
```

### 7.2 Test Execution

```bash
dotnet test
```

### 7.3 Package Generation Test

```bash
dotnet pack --configuration Release
```

### 7.4 Consumer Project Update

- Update your single consumer project
- Verify everything works correctly

---

## **Phase 8: Cleanup & Final Steps** ⏱️ 30 minutes

### 8.1 Remove Old Structure

- Delete old `src/Domain/`, `src/Audit/`, `src/EntityFramework/` directories
- Delete old test directories
- Clean up solution references

### 8.2 Final Documentation

- Update all documentation references
- Create comprehensive migration guide
- Document breaking changes

### 8.3 Commit & Tag

```bash
git add .
git commit -m "feat: restructure to Wangkanai.Foundation hierarchy

BREAKING CHANGES:
- Wangkanai.Domain → Wangkanai.Foundation.Domain
- Wangkanai.Audit → Wangkanai.Foundation.Audit
- Wangkanai.EntityFramework → Wangkanai.Foundation.EntityFramework
- Add new Wangkanai.Foundation.Events package
- Add new Wangkanai.Foundation metapackage

Resolves: #50 (IEventListener coupling)"

git tag -a v1.0.0 -m "Wangkanai Foundation v1.0.0 - Complete restructure"
```

---

## ⚠️ **Risk Mitigation**

### Rollback Plan

```bash
# If issues occur:
git checkout backup/pre-foundation-restructure
git checkout -b hotfix/rollback-foundation
# Restore original state
```

### Testing Strategy

- [x] Unit tests must pass at each phase
- [x] Build must succeed at each phase
- [x] Consumer project must work after migration
- [x] Package generation must succeed

### Validation Checklist

- [ ] All namespaces updated correctly
- [ ] All project references working
- [ ] All tests passing
- [ ] Consumer project successfully updated
- [ ] All packages build and pack correctly
- [ ] Issue #50 resolved (event infrastructure separated)

---

## 📈 **Success Metrics**

### Completion Criteria

- ✅ All packages follow `Wangkanai.Foundation.*` naming
- ✅ Issue #50 resolved with clean separation
- ✅ Consumer project working with new packages
- ✅ All tests passing
- ✅ Documentation updated
- ✅ Clean git history with proper tags

### Time Estimate: **6-7 hours total**

### Risk Level: **Low** (single consumer, good backup strategy)

---

## 🔄 **Next Steps After Completion**

1. **Publish to NuGet** - New Foundation packages
2. **Deprecate Old Packages** - Mark as obsolete with migration instructions
3. **Update Documentation** - All references to new package names
4. **Monitor Usage** - Ensure smooth transition

---

*This plan provides a comprehensive, step-by-step approach to safely restructuring the repository while maintaining functionality
and resolving architectural concerns.*