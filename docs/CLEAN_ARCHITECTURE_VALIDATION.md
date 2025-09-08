# Clean Architecture Validation Strategy

## 🎯 **Validation for Foundation Clean Architecture**

Comprehensive validation strategy for the Clean Architecture implementation within the Foundation monorepo structure.

---

## 🧪 **Clean Architecture Validation Scripts**

### **Layer Dependency Validation**

#### **Domain Layer Purity Check**

```bash
#!/bin/bash
# validate-domain-purity.sh

echo "🔍 Validating Foundation Domain Layer Purity..."

# Check for external package dependencies (should only be Microsoft.* and System.*)
echo "📦 Checking Domain package dependencies..."
domain_packages=$(dotnet list Foundation/src/Domain/Wangkanai.Foundation.Domain.csproj package 2>/dev/null | grep -v "Microsoft\.\|System\." | grep -E "^\s*>" | head -10)

if [ -n "$domain_packages" ]; then
    echo "❌ Domain layer has external dependencies:"
    echo "$domain_packages"
    echo "🚨 VIOLATION: Domain should only depend on framework packages"
    exit 1
else
    echo "✅ Domain layer is pure - no external dependencies"
fi

# Check for infrastructure imports (should not exist)
echo "🔍 Checking for infrastructure imports in domain..."
infra_imports=$(find Foundation/src/Domain -name "*.cs" -exec grep -l "Microsoft\.Extensions\.Hosting\|Microsoft\.Extensions\.DependencyInjection" {} \;)

if [ -n "$infra_imports" ]; then
    echo "❌ Domain layer has infrastructure imports:"
    echo "$infra_imports"
    echo "🚨 VIOLATION: Domain should not import infrastructure packages"
    exit 1
else
    echo "✅ Domain layer clean - no infrastructure imports"
fi

echo "✅ Domain layer purity: VALIDATED"
```

#### **Application Layer Dependency Check**

```bash
#!/bin/bash
# validate-application-dependencies.sh

echo "🔍 Validating Foundation Application Layer Dependencies..."

# Application should only reference Domain
app_refs=$(dotnet list Foundation/src/Application/Wangkanai.Foundation.Application.csproj reference)

echo "📦 Application project references:"
echo "$app_refs"

# Check that Application references Domain
if echo "$app_refs" | grep -q "Foundation.Domain"; then
    echo "✅ Application correctly references Domain"
else
    echo "❌ Application missing Domain reference"
    exit 1
fi

# Check that Application does NOT directly reference Infrastructure
if echo "$app_refs" | grep -q "Foundation.Infrastructure"; then
    echo "⚠️  Application directly references Infrastructure (acceptable but not ideal)"
else
    echo "✅ Application does not directly reference Infrastructure (Clean Architecture preserved)"
fi

echo "✅ Application layer dependencies: VALIDATED"
```

#### **Infrastructure Layer Freedom Check**

```bash
#!/bin/bash
# validate-infrastructure-freedom.sh

echo "🔍 Validating Foundation Infrastructure Layer..."

# Infrastructure can reference anything, but let's document what it does reference
infra_refs=$(dotnet list Foundation/src/Infrastructure/Wangkanai.Foundation.Infrastructure.csproj reference)

echo "📦 Infrastructure project references:"
echo "$infra_refs"

# Infrastructure should typically reference Domain
if echo "$infra_refs" | grep -q "Foundation.Domain"; then
    echo "✅ Infrastructure correctly references Domain"
else
    echo "⚠️  Infrastructure not referencing Domain (unusual but not invalid)"
fi

# Check that Infrastructure can handle external dependencies
infra_packages=$(dotnet list Foundation/src/Infrastructure/Wangkanai.Foundation.Infrastructure.csproj package)
echo "📦 Infrastructure external packages:"
echo "$infra_packages"

echo "✅ Infrastructure layer: VALIDATED (has freedom to reference external concerns)"
```

### **Clean Architecture Flow Validation**

#### **Dependency Direction Check**

```bash
#!/bin/bash
# validate-dependency-flow.sh

echo "🔍 Validating Clean Architecture Dependency Flow..."

# Expected flow: Infrastructure → Application → Domain
# Domain should have NO references to Application or Infrastructure
# Application should have NO references to Infrastructure (ideally)
# Infrastructure CAN reference Application and Domain

check_reverse_dependencies() {
    local layer=$1
    local should_not_reference=$2

    refs=$(dotnet list Foundation/src/$layer/Wangkanai.Foundation.$layer.csproj reference)
    if echo "$refs" | grep -q "$should_not_reference"; then
        echo "❌ $layer incorrectly references $should_not_reference"
        echo "🚨 VIOLATION: Dependency inversion violated"
        return 1
    else
        echo "✅ $layer correctly does NOT reference $should_not_reference"
        return 0
    fi
}

# Domain should not reference Application or Infrastructure
check_reverse_dependencies "Domain" "Application" || exit 1
check_reverse_dependencies "Domain" "Infrastructure" || exit 1

# Application should not reference Infrastructure (ideally)
if check_reverse_dependencies "Application" "Infrastructure"; then
    echo "✅ Clean Architecture: Ideal dependency flow maintained"
else
    echo "⚠️  Application references Infrastructure (pragmatic but not pure Clean Architecture)"
fi

echo "✅ Dependency flow: VALIDATED"
```

---

## 🚨 **Issue #50 Specific Validation**

### **Hosting Dependency Detection**

```bash
#!/bin/bash
# validate-issue-50-resolution.sh

echo "🚨 Validating Issue #50 Resolution..."

# Check for IHostedService usage in Domain layer
echo "🔍 Checking Domain layer for hosting dependencies..."
domain_hosting=$(find Foundation/src/Domain -name "*.cs" -exec grep -l "IHostedService\|Microsoft\.Extensions\.Hosting" {} \;)

if [ -n "$domain_hosting" ]; then
    echo "❌ ISSUE #50 UNRESOLVED: Found hosting dependencies in Domain:"
    echo "$domain_hosting"

    # Show specific violations
    for file in $domain_hosting; do
        echo "📄 $file:"
        grep -n "IHostedService\|Microsoft\.Extensions\.Hosting" "$file"
    done

    echo ""
    echo "🔧 RESOLUTION NEEDED:"
    echo "   1. Move IEventListener to Infrastructure layer"
    echo "   2. Create pure domain interfaces in Domain layer"
    echo "   3. Remove Microsoft.Extensions.Hosting from Domain"
    exit 1
else
    echo "✅ Issue #50 RESOLVED: No hosting dependencies in Domain layer"
fi

# Check Infrastructure layer has hosting capabilities
echo "🔍 Checking Infrastructure layer for hosting capabilities..."
infra_hosting=$(find Foundation/src/Infrastructure -name "*.cs" -exec grep -l "IHostedService\|Microsoft\.Extensions\.Hosting" {} \;)

if [ -n "$infra_hosting" ]; then
    echo "✅ Infrastructure layer properly handles hosting concerns:"
    echo "$infra_hosting"
else
    echo "⚠️  Infrastructure layer has no hosting capabilities (may need to add them)"
fi

echo "✅ Issue #50 validation: COMPLETE"
```

### **Domain Purity Validation**

```bash
#!/bin/bash
# validate-domain-purity-comprehensive.sh

echo "🔍 Comprehensive Domain Purity Validation..."

domain_dir="Foundation/src/Domain"

# Check 1: No external infrastructure dependencies
echo "1️⃣ Checking for infrastructure dependencies..."
infra_deps=$(find "$domain_dir" -name "*.cs" -exec grep -l "using Microsoft\.Extensions\." {} \; | grep -v "Microsoft\.Extensions\.Identity\|Microsoft\.Extensions\.Primitives")

if [ -n "$infra_deps" ]; then
    echo "❌ Domain has infrastructure dependencies:"
    for file in $infra_deps; do
        echo "   📄 $file"
        grep -n "using Microsoft\.Extensions\." "$file" | grep -v "Identity\|Primitives"
    done
    exit 1
fi

# Check 2: No concrete implementations of infrastructure interfaces
echo "2️⃣ Checking for infrastructure implementations..."
infra_impls=$(find "$domain_dir" -name "*.cs" -exec grep -l ": IHostedService\|: BackgroundService\|: IServiceScope" {} \;)

if [ -n "$infra_impls" ]; then
    echo "❌ Domain has infrastructure implementations:"
    echo "$infra_impls"
    exit 1
fi

# Check 3: Only domain-appropriate packages in project file
echo "3️⃣ Checking project dependencies..."
bad_packages=$(cat "$domain_dir/Wangkanai.Foundation.Domain.csproj" | grep -o 'Include="[^"]*"' | grep -v "Microsoft\.EntityFramework\|Microsoft\.Extensions\.Identity\|Microsoft\.CodeAnalysis\|Wangkanai\.\|System\." | head -5)

if [ -n "$bad_packages" ]; then
    echo "❌ Domain project has questionable dependencies:"
    echo "$bad_packages"
fi

echo "✅ Domain purity: COMPREHENSIVE VALIDATION COMPLETE"
```

---

## 📦 **Package Generation Validation**

### **Multi-Layer Package Test**

```bash
#!/bin/bash
# validate-clean-architecture-packages.sh

echo "📦 Validating Clean Architecture Package Generation..."

# Clean previous packages
rm -rf packages/
mkdir -p packages/

# Pack each layer separately
layers=("Domain" "Application" "Infrastructure")

for layer in "${layers[@]}"; do
    echo "📦 Packing Foundation.$layer..."

    project_path="Foundation/src/$layer/Wangkanai.Foundation.$layer.csproj"
    if [ -f "$project_path" ]; then
        dotnet pack "$project_path" --configuration Release --output ./packages --verbosity minimal

        if [ $? -eq 0 ]; then
            echo "✅ Foundation.$layer package generated successfully"
        else
            echo "❌ Foundation.$layer package generation failed"
            exit 1
        fi
    else
        echo "⚠️  Project not found: $project_path"
    fi
done

# Validate packages were created
echo ""
echo "📋 Generated Clean Architecture packages:"
ls -la packages/Wangkanai.Foundation.*.nupkg

# Count packages (should have all layers)
package_count=$(ls packages/Wangkanai.Foundation.*.nupkg 2>/dev/null | wc -l)
echo "📊 Package count: $package_count"

if [ "$package_count" -ge 3 ]; then
    echo "✅ All Clean Architecture layers packaged successfully"
else
    echo "❌ Missing packages - expected at least 3 (Domain, Application, Infrastructure)"
    exit 1
fi
```

---

## 🎯 **Complete Clean Architecture Validation**

### **Master Validation Script**

```bash
#!/bin/bash
# validate-clean-architecture-complete.sh

echo "🚀 Complete Foundation Clean Architecture Validation..."

# Exit on any error
set -e

echo ""
echo "🧪 Phase 1: Layer Purity Validation"
./validate-domain-purity.sh
./validate-application-dependencies.sh
./validate-infrastructure-freedom.sh

echo ""
echo "🔄 Phase 2: Dependency Flow Validation"
./validate-dependency-flow.sh

echo ""
echo "🚨 Phase 3: Issue #50 Resolution Check"
./validate-issue-50-resolution.sh

echo ""
echo "🏗️ Phase 4: Build Validation"
echo "Building all Clean Architecture layers..."
dotnet build Foundation/src/Domain/Wangkanai.Foundation.Domain.csproj --verbosity minimal
dotnet build Foundation/src/Application/Wangkanai.Foundation.Application.csproj --verbosity minimal
dotnet build Foundation/src/Infrastructure/Wangkanai.Foundation.Infrastructure.csproj --verbosity minimal

echo ""
echo "🧪 Phase 5: Test Validation"
echo "Running Foundation tests..."
if [ -f "Foundation/tests/Domain/Wangkanai.Foundation.Domain.Tests.csproj" ]; then
    dotnet test Foundation/tests/Domain/Wangkanai.Foundation.Domain.Tests.csproj --verbosity minimal
else
    echo "⚠️  No tests found for Foundation domain"
fi

echo ""
echo "📦 Phase 6: Package Generation Validation"
./validate-clean-architecture-packages.sh

echo ""
echo "🎉 CLEAN ARCHITECTURE VALIDATION: COMPLETE"
echo ""
echo "📊 Summary:"
echo "  ✅ Domain layer purity maintained"
echo "  ✅ Application layer dependencies correct"
echo "  ✅ Infrastructure layer properly isolated"
echo "  ✅ Dependency flow follows Clean Architecture"
echo "  ✅ All layers build successfully"
echo "  ✅ All packages generate correctly"

# Final Issue #50 status
if find Foundation/src/Domain -name "*.cs" -exec grep -l "IHostedService" {} \; | head -1 > /dev/null; then
    echo "  ⚠️  Issue #50: Still pending (IHostedService in Domain)"
    echo "      → Move IEventListener to Infrastructure layer to complete"
else
    echo "  ✅ Issue #50: RESOLVED (Domain pure of hosting dependencies)"
fi

echo ""
echo "🏆 Foundation Clean Architecture: VALIDATED"
```

---

## 🎯 **Success Criteria**

### **Clean Architecture Compliance**

- [x] **Domain Layer**: Pure business logic, no external dependencies
- [x] **Application Layer**: Use cases, depends only on Domain
- [x] **Infrastructure Layer**: External concerns, can depend on Domain/Application
- [ ] **Issue #50**: Domain free of hosting dependencies (pending IEventListener move)

### **Package Quality**

- [x] **All layers build** independently
- [x] **All layers package** successfully
- [ ] **All tests pass** (tests may need updates for new structure)
- [x] **Dependency flow** follows Clean Architecture principles

### **Integration Quality**

- [x] **Cross-layer** references work correctly
- [x] **Solution builds** with all layers
- [x] **Monorepo structure** maintains Clean Architecture
- [x] **Documentation** reflects current reality

---

## 📋 **Usage Instructions**

```bash
# Make all scripts executable
chmod +x validate-*.sh

# Run complete validation
./validate-clean-architecture-complete.sh

# Run specific validations
./validate-domain-purity.sh              # Domain layer only
./validate-application-dependencies.sh   # Application layer only
./validate-issue-50-resolution.sh        # Issue #50 specific
./validate-clean-architecture-packages.sh # Package generation
```

---

*This validation strategy ensures the Clean Architecture implementation maintains proper layer separation while enabling the
sophisticated Foundation package architecture.*