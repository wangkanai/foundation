# Foundation Monorepo Migration Guide

## 🎯 **Migration Overview**

Guide for consumers migrating from the old flat structure to the new Foundation monorepo architecture.

---

## 📊 **Migration Impact Summary**

### **Package Name Changes**

| **Old Package** | **New Package** | **Status** |
|-----------------|-----------------|------------|
| `Wangkanai.Domain` (v5.0.0) | `Wangkanai.Foundation.Domain` (v1.0.0) | ✅ Available |
| `Wangkanai.Audit` (v0.3.0) | `Wangkanai.Audit.Domain` (v1.0.0) | ✅ Available |  
| `Wangkanai.EntityFramework` (v3.7.0) | `Wangkanai.EntityFramework` (v1.0.0) | ✅ Available |

### **Namespace Changes**

| **Old Namespace** | **New Namespace** |
|-------------------|-------------------|
| `Wangkanai.Domain` | `Wangkanai.Foundation.Domain` |
| `Wangkanai.Audit` | `Wangkanai.Audit` (unchanged) |
| `Wangkanai.EntityFramework` | `Wangkanai.EntityFramework` (unchanged) |

---

## 🚀 **Step-by-Step Migration**

### **Step 1: Update Package References**

#### **Before (PackageReference)**
```xml
<PackageReference Include="Wangkanai.Domain" Version="5.0.0" />
<PackageReference Include="Wangkanai.Audit" Version="0.3.0" />
<PackageReference Include="Wangkanai.EntityFramework" Version="3.7.0" />
```

#### **After (PackageReference)**
```xml
<PackageReference Include="Wangkanai.Foundation.Domain" Version="1.0.0" />
<PackageReference Include="Wangkanai.Audit.Domain" Version="1.0.0" />
<PackageReference Include="Wangkanai.EntityFramework" Version="1.0.0" />
```

### **Step 2: Update Using Statements**

#### **Before (C# Code)**
```csharp
using Wangkanai.Domain;
using Wangkanai.Domain.Events;
using Wangkanai.Domain.Interfaces;
using Wangkanai.Domain.Primitives;
using Wangkanai.Audit;
using Wangkanai.EntityFramework;
```

#### **After (C# Code)**
```csharp
using Wangkanai.Foundation.Domain;
using Wangkanai.Foundation.Domain.Events;  
using Wangkanai.Foundation.Domain.Interfaces;
using Wangkanai.Foundation.Domain.Primitives;
using Wangkanai.Audit;                     // ← No change
using Wangkanai.EntityFramework;           // ← No change
```

### **Step 3: Update Code References**

#### **Entity Base Classes**
```csharp
// Before
public class User : Entity<int> { }
public class Product : KeyGuidEntity { }

// After - Same usage, different namespace
public class User : Entity<int> { }        // Wangkanai.Foundation.Domain
public class Product : KeyGuidEntity { }   // Wangkanai.Foundation.Domain
```

#### **Value Objects**
```csharp
// Before
public class Money : ValueObject { }

// After - Same usage, different namespace
public class Money : ValueObject { }       // Wangkanai.Foundation.Domain
```

#### **Result Pattern**
```csharp
// Before
public Result<User> GetUser(int id) { }

// After - Same usage, different namespace  
public Result<User> GetUser(int id) { }    // Wangkanai.Foundation.Domain
```

### **Step 4: Domain Events (⚠️ Issue #50 Pending)**

#### **Current State (Still Coupled)**
```csharp
// This still exists with hosting coupling
using Wangkanai.Foundation.Domain;         // IEventListener still here

public class UserEventListener : IEventListener<UserCreated, UserAction>
{
    // Still implements IHostedService - Issue #50 unresolved
}
```

#### **Expected Future State**
```csharp
// When Issue #50 is resolved
using Wangkanai.Foundation.Domain;         // Pure domain events
using Wangkanai.Foundation.Events;         // Hosting abstractions

public class UserEventHandler : IDomainEventHandler<UserCreated>  // Pure domain
{
    public Task HandleAsync(UserCreated domainEvent, CancellationToken cancellationToken)
    {
        // Pure domain logic
    }
}

public class UserEventListener : IEventListenerService<UserCreated, UserAction>  // Hosting wrapper
{
    // IHostedService implementation
}
```

---

## 🔧 **Migration Automation**

### **PowerShell Migration Script**
```powershell
# migrate-to-foundation.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath
)

Write-Host "🚀 Starting Foundation migration for: $ProjectPath"

# Step 1: Update project file package references
Write-Host "📦 Updating package references..."

$projectFile = Get-Content "$ProjectPath" -Raw
$projectFile = $projectFile -replace 'Wangkanai\.Domain" Version="[^"]*"', 'Wangkanai.Foundation.Domain" Version="1.0.0"'
$projectFile = $projectFile -replace 'Wangkanai\.Audit" Version="[^"]*"', 'Wangkanai.Audit.Domain" Version="1.0.0"'
$projectFile = $projectFile -replace 'Wangkanai\.EntityFramework" Version="[^"]*"', 'Wangkanai.EntityFramework" Version="1.0.0"'
Set-Content "$ProjectPath" $projectFile

# Step 2: Update using statements in C# files
Write-Host "📝 Updating using statements..."

$csFiles = Get-ChildItem -Path (Split-Path $ProjectPath) -Filter "*.cs" -Recurse

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'using Wangkanai\.Domain;', 'using Wangkanai.Foundation.Domain;'
    $content = $content -replace 'using Wangkanai\.Domain\.', 'using Wangkanai.Foundation.Domain.'
    Set-Content $file.FullName $content
}

Write-Host "✅ Migration completed!"
Write-Host "⚠️  Note: Issue #50 still pending - IEventListener hosting coupling exists"
Write-Host "🔧 Next steps:"
Write-Host "   1. Build project: dotnet build"
Write-Host "   2. Run tests: dotnet test"  
Write-Host "   3. Update any remaining references manually"
```

### **Bash Migration Script**
```bash
#!/bin/bash
# migrate-to-foundation.sh

PROJECT_PATH=$1

if [ -z "$PROJECT_PATH" ]; then
    echo "Usage: $0 <path-to-project-file>"
    exit 1
fi

echo "🚀 Starting Foundation migration for: $PROJECT_PATH"

# Step 1: Update package references
echo "📦 Updating package references..."
sed -i 's/Wangkanai\.Domain" Version="[^"]*"/Wangkanai.Foundation.Domain" Version="1.0.0"/g' "$PROJECT_PATH"
sed -i 's/Wangkanai\.Audit" Version="[^"]*"/Wangkanai.Audit.Domain" Version="1.0.0"/g' "$PROJECT_PATH"
sed -i 's/Wangkanai\.EntityFramework" Version="[^"]*"/Wangkanai.EntityFramework" Version="1.0.0"/g' "$PROJECT_PATH"

# Step 2: Update using statements
echo "📝 Updating using statements in C# files..."
project_dir=$(dirname "$PROJECT_PATH")
find "$project_dir" -name "*.cs" -exec sed -i 's/using Wangkanai\.Domain;/using Wangkanai.Foundation.Domain;/g' {} \;
find "$project_dir" -name "*.cs" -exec sed -i 's/using Wangkanai\.Domain\./using Wangkanai.Foundation.Domain./g' {} \;

echo "✅ Migration completed!"
echo "⚠️  Note: Issue #50 still pending - IEventListener hosting coupling exists"
echo "🔧 Next steps:"
echo "   1. Build project: dotnet build"
echo "   2. Run tests: dotnet test"
echo "   3. Update any remaining references manually"
```

---

## ⚠️ **Breaking Changes & Considerations**

### **Version Reset**
- **All packages reset to v1.0.0** - indicates major architectural change
- **Semantic versioning restart** - new ecosystem foundation established

### **IEventListener Coupling (Issue #50)**
```csharp
// ⚠️ BREAKING: This interface still couples domain with hosting
public interface IEventListener<in TEvent, in TAction> : IHostedService
```

**Impact**: Projects using `IEventListener` still have hosting dependencies

**Mitigation**: 
1. Current: Continue using existing interface (coupling remains)
2. Future: Wait for Issue #50 resolution with proper abstraction

### **Project Reference Complexity**
- **Cross-domain references** use complex relative paths
- **Build system dependency** on monorepo structure
- **IDE experience** may show projects in different locations

---

## 🧪 **Validation After Migration**

### **Build Verification**
```bash
# Clean and rebuild to ensure everything works
dotnet clean
dotnet restore
dotnet build

# Should complete without errors
```

### **Test Verification** 
```bash
# Run all tests to ensure functionality preserved
dotnet test

# Should show same test results as before migration
```

### **Runtime Verification**
```csharp
// Test key components still work
var entity = new MyEntity(1);           // Entity creation
var result = Result.Success(entity);    // Result pattern
var valueObj = new MyValueObject();     // Value objects

// Should behave identically to previous versions
```

---

## 📈 **Benefits After Migration**

### **✅ Improved Architecture**
- **Foundation branding** - clear ecosystem identity
- **Domain separation** - monorepo organization
- **Version alignment** - all packages v1.0.0

### **🚀 Future-Ready**
- **Ecosystem expansion** - ready for additional domains  
- **Independent evolution** - domains can evolve separately
- **Enterprise structure** - scales for large organizations

### **🔧 Developer Experience**
- **Consistent naming** - `Wangkanai.Foundation.*` pattern
- **Clear dependencies** - domain relationships visible
- **Modern architecture** - follows current best practices

---

## 🎯 **Migration Timeline**

### **Immediate (Day 1)**
1. ✅ Update package references in consumer projects
2. ✅ Update using statements  
3. ✅ Rebuild and test projects
4. ⚠️ Address any compilation errors

### **Short-term (Week 1)**
1. 🔄 Monitor for runtime issues
2. 📝 Update internal documentation
3. 👥 Train team on new structure

### **Long-term (Month 1)**
1. ⏳ Wait for Issue #50 resolution
2. 🔄 Migrate to pure domain events when available
3. 📦 Consider using metapackage when created

---

## 📞 **Support & Troubleshooting**

### **Common Issues**

#### **Build Errors After Migration**
```bash
# Error: Cannot find package 'Wangkanai.Domain'
# Solution: Ensure all references updated to 'Wangkanai.Foundation.Domain'

# Error: Namespace 'Wangkanai.Domain' not found  
# Solution: Update using statements to 'Wangkanai.Foundation.Domain'
```

#### **Runtime Issues**
- **Behavior differences**: Should be minimal - same core logic
- **Performance impact**: Should be negligible - same implementations
- **Event handling**: May need adjustment when Issue #50 is resolved

### **Getting Help**
1. **Check validation scripts** - use provided migration automation
2. **Review documentation** - consult updated architectural guides  
3. **Test thoroughly** - run full test suites after migration
4. **Monitor Issue #50** - track progress on hosting dependency resolution

---

## 🏆 **Success Criteria**

Your migration is successful when:

- [x] Project builds without errors
- [x] All tests pass  
- [x] Runtime behavior unchanged
- [x] New namespace references work correctly
- [ ] Issue #50 resolution applied (when available)

---

*This migration guide ensures smooth transition to the Foundation monorepo architecture while preparing for future enhancements.*