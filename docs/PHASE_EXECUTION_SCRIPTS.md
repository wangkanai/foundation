# Foundation Restructure - Execution Scripts

## 📋 **Phase-by-Phase Execution Commands**

### **Phase 1: Preparation & Backup**

```bash
# Create backup branch from current main
git checkout main
git pull origin main
git checkout -b backup/pre-foundation-restructure
git push -u origin backup/pre-foundation-restructure

# Tag current state  
git tag -a v5.0.0-pre-foundation -m "Pre-Foundation restructure snapshot - Domain v5.0.0, Audit v0.3.0, EF v3.7.0"
git push origin v5.0.0-pre-foundation

# Create working branch
git checkout main
git checkout -b feature/foundation-restructure
```

**Validation**: Confirm branches created successfully
```bash
git branch -a | grep -E "(backup|feature)"
git tag | grep "pre-foundation"
```

---

### **Phase 2: Project Structure Creation**

```bash
# Create Foundation directory structure
mkdir -p src/Foundation/Domain
mkdir -p src/Foundation/Audit
mkdir -p src/Foundation/EntityFramework
mkdir -p src/Foundation/Events
mkdir -p src/Foundation/Metapackage

# Create corresponding test directories
mkdir -p tests/Foundation/Domain
mkdir -p tests/Foundation/Audit
mkdir -p tests/Foundation/EntityFramework
mkdir -p tests/Foundation/Events

# Create corresponding benchmark directories
mkdir -p benchmark/Foundation/Domain
mkdir -p benchmark/Foundation/Audit
mkdir -p benchmark/Foundation/EntityFramework
```

**Validation**: Verify directory structure
```bash
tree src/Foundation tests/Foundation benchmark/Foundation
```

---

### **Phase 3A: Create New Project Files**

#### Foundation.Domain Project File
```bash
cat > src/Foundation/Domain/Wangkanai.Foundation.Domain.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <Product>Wangkanai Foundation Domain</Product>
    <PackageTags>wangkanai;foundation;domain;ddd;entity;valueobject;dotnet</PackageTags>
    <Description>Core Domain-Driven Design patterns and abstractions for .NET applications</Description>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Common" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Wangkanai.System" />
    <PackageReference Include="Wangkanai.Validation" />
  </ItemGroup>
</Project>
EOF
```

#### Foundation.Audit Project File
```bash
cat > src/Foundation/Audit/Wangkanai.Foundation.Audit.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <Product>Wangkanai Foundation Audit</Product>
    <PackageTags>wangkanai;foundation;audit;trail;traceability;ddd</PackageTags>
    <Description>Comprehensive audit trail and change tracking for DDD applications</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Domain/Wangkanai.Foundation.Domain.csproj" />
  </ItemGroup>
</Project>
EOF
```

#### Foundation.EntityFramework Project File
```bash
cat > src/Foundation/EntityFramework/Wangkanai.Foundation.EntityFramework.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <Product>Wangkanai Foundation EntityFramework</Product>
    <PackageTags>wangkanai;foundation;entityframework;orm;ddd;dotnet</PackageTags>
    <Description>Entity Framework Core integrations for Wangkanai Foundation DDD patterns</Description>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Audit/Wangkanai.Foundation.Audit.csproj" />
  </ItemGroup>
</Project>
EOF
```

#### Foundation.Events Project File
```bash
cat > src/Foundation/Events/Wangkanai.Foundation.Events.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <Product>Wangkanai Foundation Events</Product>
    <PackageTags>wangkanai;foundation;events;domain-events;hosting;ddd</PackageTags>
    <Description>Event infrastructure and hosting integration for domain events</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Domain/Wangkanai.Foundation.Domain.csproj" />
  </ItemGroup>
</Project>
EOF
```

#### Foundation Metapackage Project File
```bash
cat > src/Foundation/Metapackage/Wangkanai.Foundation.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <Product>Wangkanai Foundation</Product>
    <PackageTags>wangkanai;foundation;ddd;domain;audit;entityframework;events</PackageTags>
    <Description>Complete DDD foundation package including Domain, Audit, EntityFramework, and Events</Description>
    <IncludeBuildOutput>false</IncludeBuildOutput>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Domain/Wangkanai.Foundation.Domain.csproj" />
    <ProjectReference Include="../Audit/Wangkanai.Foundation.Audit.csproj" />
    <ProjectReference Include="../EntityFramework/Wangkanai.Foundation.EntityFramework.csproj" />
    <ProjectReference Include="../Events/Wangkanai.Foundation.Events.csproj" />
  </ItemGroup>
</Project>
EOF
```

**Validation**: Verify project files created
```bash
ls -la src/Foundation/*/Wangkanai.Foundation.*.csproj
```

---

### **Phase 3B: Content Migration**

#### Migrate Domain Files
```bash
# Copy all domain files (except Events and Infrastructure)
cp -r src/Domain/* src/Foundation/Domain/ 2>/dev/null || true

# Remove the project file (we created new one)
rm -f src/Foundation/Domain/Wangkanai.Domain.csproj*

# Remove Events directory (will be restructured)
rm -rf src/Foundation/Domain/Events

# Remove Infrastructure directory (will be restructured)  
rm -rf src/Foundation/Domain/Infrastructure
```

#### Migrate Audit Files
```bash
# Copy all audit files
cp -r src/Audit/* src/Foundation/Audit/ 2>/dev/null || true

# Remove the project file (we created new one)
rm -f src/Foundation/Audit/Wangkanai.Audit.csproj*
```

#### Migrate EntityFramework Files
```bash
# Copy all EF files
cp -r src/EntityFramework/* src/Foundation/EntityFramework/ 2>/dev/null || true

# Remove the project file (we created new one)
rm -f src/Foundation/EntityFramework/Wangkanai.EntityFramework.csproj*
```

**Validation**: Verify content copied
```bash
find src/Foundation -name "*.cs" | wc -l
ls -la src/Foundation/*/
```

---

### **Phase 3C: Create Events Package Content**

```bash
# Create Events infrastructure files
mkdir -p src/Foundation/Events/Services
mkdir -p src/Foundation/Events/Extensions
mkdir -p src/Foundation/Events/Interfaces

# Create basic Events structure files (will need manual implementation)
cat > src/Foundation/Events/Usings.cs << 'EOF'
global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.DependencyInjection;
global using Wangkanai.Foundation.Domain.Events;
EOF
```

---

### **Phase 3D: Update Namespaces**

#### Domain Namespace Updates
```bash
# Update all .cs files in Foundation.Domain
find src/Foundation/Domain -name "*.cs" -exec sed -i '' 's/namespace Wangkanai\.Domain/namespace Wangkanai.Foundation.Domain/g' {} \;
find src/Foundation/Domain -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Domain/using Wangkanai.Foundation.Domain/g' {} \;
```

#### Audit Namespace Updates  
```bash
# Update all .cs files in Foundation.Audit
find src/Foundation/Audit -name "*.cs" -exec sed -i '' 's/namespace Wangkanai\.Audit/namespace Wangkanai.Foundation.Audit/g' {} \;
find src/Foundation/Audit -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Audit/using Wangkanai.Foundation.Audit/g' {} \;
find src/Foundation/Audit -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Domain/using Wangkanai.Foundation.Domain/g' {} \;
```

#### EntityFramework Namespace Updates
```bash
# Update all .cs files in Foundation.EntityFramework
find src/Foundation/EntityFramework -name "*.cs" -exec sed -i '' 's/namespace Wangkanai\.EntityFramework/namespace Wangkanai.Foundation.EntityFramework/g' {} \;
find src/Foundation/EntityFramework -name "*.cs" -exec sed -i '' 's/using Wangkanai\.EntityFramework/using Wangkanai.Foundation.EntityFramework/g' {} \;
find src/Foundation/EntityFramework -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Audit/using Wangkanai.Foundation.Audit/g' {} \;
find src/Foundation/EntityFramework -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Domain/using Wangkanai.Foundation.Domain/g' {} \;
```

**Validation**: Verify namespace changes
```bash
grep -r "namespace Wangkanai\.Domain" src/Foundation/ || echo "✅ No old Domain namespaces found"
grep -r "namespace Wangkanai\.Audit" src/Foundation/ || echo "✅ No old Audit namespaces found"  
grep -r "namespace Wangkanai\.EntityFramework" src/Foundation/ || echo "✅ No old EF namespaces found"
```

---

### **Phase 4: Test Migration**

#### Create Test Project Files
```bash
# Foundation.Domain Tests
cat > tests/Foundation/Domain/Wangkanai.Foundation.Domain.Tests.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../../src/Foundation/Domain/Wangkanai.Foundation.Domain.csproj" />
  </ItemGroup>
</Project>
EOF

# Foundation.Audit Tests  
cat > tests/Foundation/Audit/Wangkanai.Foundation.Audit.Tests.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../../src/Foundation/Audit/Wangkanai.Foundation.Audit.csproj" />
  </ItemGroup>
</Project>
EOF

# Foundation.EntityFramework Tests
cat > tests/Foundation/EntityFramework/Wangkanai.Foundation.EntityFramework.Tests.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../../src/Foundation/EntityFramework/Wangkanai.Foundation.EntityFramework.csproj" />
  </ItemGroup>
</Project>
EOF

# Foundation.Events Tests
cat > tests/Foundation/Events/Wangkanai.Foundation.Events.Tests.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../../src/Foundation/Events/Wangkanai.Foundation.Events.csproj" />
  </ItemGroup>
</Project>
EOF
```

#### Migrate Test Content
```bash
# Copy existing test files and update namespaces
cp -r tests/Domain/* tests/Foundation/Domain/ 2>/dev/null || true
cp -r tests/Audit/* tests/Foundation/Audit/ 2>/dev/null || true
cp -r tests/EntityFramework/* tests/Foundation/EntityFramework/ 2>/dev/null || true

# Remove old project files from copied content
rm -f tests/Foundation/Domain/Wangkanai.Domain.Tests.csproj
rm -f tests/Foundation/Audit/Wangkanai.Audit.Tests.csproj
rm -f tests/Foundation/EntityFramework/Wangkanai.EntityFramework.Tests.csproj

# Update test namespaces
find tests/Foundation/Domain -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Domain/using Wangkanai.Foundation.Domain/g' {} \;
find tests/Foundation/Audit -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Audit/using Wangkanai.Foundation.Audit/g' {} \;
find tests/Foundation/Audit -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Domain/using Wangkanai.Foundation.Domain/g' {} \;
find tests/Foundation/EntityFramework -name "*.cs" -exec sed -i '' 's/using Wangkanai\.EntityFramework/using Wangkanai.Foundation.EntityFramework/g' {} \;
find tests/Foundation/EntityFramework -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Audit/using Wangkanai.Foundation.Audit/g' {} \;
```

**Validation**: Verify test projects created
```bash
ls -la tests/Foundation/*/Wangkanai.Foundation.*.Tests.csproj
```

---

### **Phase 5: Solution Update**

#### Update Solution File
```bash
# Create new solution structure (manual edit needed for Domain.slnx)
echo "🔧 MANUAL STEP: Update Domain.slnx to include new Foundation projects"
echo "Add these projects to the solution:"
echo "  - src/Foundation/Domain/Wangkanai.Foundation.Domain.csproj"
echo "  - src/Foundation/Audit/Wangkanai.Foundation.Audit.csproj"
echo "  - src/Foundation/EntityFramework/Wangkanai.Foundation.EntityFramework.csproj"
echo "  - src/Foundation/Events/Wangkanai.Foundation.Events.csproj"
echo "  - src/Foundation/Metapackage/Wangkanai.Foundation.csproj"
echo ""
echo "Add these test projects:"
echo "  - tests/Foundation/Domain/Wangkanai.Foundation.Domain.Tests.csproj"
echo "  - tests/Foundation/Audit/Wangkanai.Foundation.Audit.Tests.csproj"
echo "  - tests/Foundation/EntityFramework/Wangkanai.Foundation.EntityFramework.Tests.csproj"
echo "  - tests/Foundation/Events/Wangkanai.Foundation.Events.Tests.csproj"
```

---

### **Phase 6: Build & Test**

```bash
# Clean and restore
dotnet clean
dotnet restore

# Build everything
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Pack packages
dotnet pack --configuration Release --output ./packages
```

**Validation**: All commands should succeed without errors

---

### **Phase 7: Cleanup**

```bash
# Remove old directory structure (ONLY after validation passes)
echo "⚠️  MANUAL STEP: After confirming everything works:"
echo "rm -rf src/Domain"
echo "rm -rf src/Audit" 
echo "rm -rf src/EntityFramework"
echo "rm -rf tests/Domain"
echo "rm -rf tests/Audit"
echo "rm -rf tests/EntityFramework"
echo "rm -rf benchmark/Domain"
echo "rm -rf benchmark/Audit"
echo "rm -rf benchmark/EntityFramework"
```

---

### **Phase 8: Final Commit**

```bash
git add .
git commit -m "feat: restructure to Wangkanai.Foundation hierarchy

BREAKING CHANGES:
- Wangkanai.Domain → Wangkanai.Foundation.Domain  
- Wangkanai.Audit → Wangkanai.Foundation.Audit
- Wangkanai.EntityFramework → Wangkanai.Foundation.EntityFramework
- Add new Wangkanai.Foundation.Events package (resolves #50)
- Add new Wangkanai.Foundation metapackage

Migration Guide:
- Update package references in consumer projects
- Update using statements to new namespaces
- Use Wangkanai.Foundation for complete package set

Resolves: #50 - Separate domain events from hosting infrastructure"

# Tag the new version
git tag -a v1.0.0 -m "Wangkanai Foundation v1.0.0

Complete restructure to hierarchical Foundation pattern:
- Foundation.Domain v1.0.0
- Foundation.Audit v1.0.0  
- Foundation.EntityFramework v1.0.0
- Foundation.Events v1.0.0 (NEW)
- Foundation metapackage v1.0.0 (NEW)"

# Push to remote
git push origin feature/foundation-restructure
git push origin v1.0.0
```

---

## 🚨 **Emergency Rollback**

If anything goes wrong at any point:

```bash
# Immediate rollback to backup
git checkout backup/pre-foundation-restructure
git checkout -b hotfix/emergency-rollback
git push -u origin hotfix/emergency-rollback

# Restore working state
git checkout main
git reset --hard v5.0.0-pre-foundation
```

---

## ✅ **Validation Commands**

Run these at each major phase:

```bash
# Structure validation
find src/Foundation -name "*.csproj" | wc -l  # Should be 5
find tests/Foundation -name "*.csproj" | wc -l  # Should be 4

# Build validation
dotnet build --verbosity minimal

# Test validation  
dotnet test --logger "console;verbosity=minimal"

# Package validation
dotnet pack --configuration Release --verbosity minimal
```

*Execute these commands phase by phase, validating after each major step.*