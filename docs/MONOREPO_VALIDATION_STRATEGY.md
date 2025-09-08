# Monorepo Validation Strategy - Foundation

## 🎯 **Overview**

Validation strategy for the completed Foundation monorepo structure, ensuring all domains build correctly and maintain proper dependencies.

---

## 🧪 **Validation Phases**

### **Phase 1: Structure Validation**

#### **Directory Structure Check**
```bash
#!/bin/bash
# validate-monorepo-structure.sh

echo "🔍 Validating monorepo structure..."

# Check top-level domain directories
domains=("Foundation" "Audit" "EntityFramework")
for domain in "${domains[@]}"; do
    if [ -d "$domain" ]; then
        echo "✅ $domain domain directory exists"
        
        # Check required subdirectories
        subdirs=("src" "benchmarks" "tests")
        for subdir in "${subdirs[@]}"; do
            if [ -d "$domain/$subdir" ]; then
                echo "  ✅ $domain/$subdir exists"
            else
                echo "  ❌ $domain/$subdir missing"
            fi
        done
    else
        echo "❌ $domain domain directory missing"
    fi
done

# Check solution file
if [ -f "Foundation.slnx" ]; then
    echo "✅ Foundation.slnx exists"
else
    echo "❌ Foundation.slnx missing"
fi
```

#### **Project Registration Validation**
```bash
# Check all projects are registered in solution
echo "📋 Validating project registration..."

# Expected projects
expected_projects=(
    "Foundation\\src\\Domain\\Wangkanai.Foundation.Domain.csproj"
    "Foundation\\benchmarks\\Domain\\Wangkanai.Foundation.Domain.Benchmark.csproj"
    "Foundation\\tests\\Domain\\Wangkanai.Foundation.Domain.Tests.csproj"
    "Audit\\src\\Domain\\Wangkanai.Audit.Domain.csproj"
    "Audit\\benchmarks\\Domain\\Wangkanai.Audit.Domain.Benchmark.csproj"
    "Audit\\tests\\Domain\\Wangkanai.Audit.Domain.Tests.csproj"
    "EntityFramework\\src\\EntityFramework\\Wangkanai.EntityFramework.csproj"
    "EntityFramework\\benchmarks\\EntityFramework\\Wangkanai.EntityFramework.Benchmark.csproj"
    "EntityFramework\\tests\\EntityFramework\\Wangkanai.EntityFramework.Tests.csproj"
)

for project in "${expected_projects[@]}"; do
    if grep -q "$project" Foundation.slnx; then
        echo "✅ $project registered"
    else
        echo "❌ $project not registered in solution"
    fi
done
```

---

### **Phase 2: Build Validation**

#### **Individual Domain Builds**
```bash
# Build each domain independently
echo "🔧 Building individual domains..."

build_domain() {
    domain=$1
    echo "Building $domain domain..."
    
    # Find main project in domain
    main_project=$(find "$domain/src" -name "*.csproj" | head -1)
    if [ -f "$main_project" ]; then
        dotnet build "$main_project" --verbosity minimal
        if [ $? -eq 0 ]; then
            echo "✅ $domain domain build successful"
        else
            echo "❌ $domain domain build failed"
            return 1
        fi
    else
        echo "❌ No project found in $domain/src"
        return 1
    fi
}

# Build all domains
for domain in "${domains[@]}"; do
    build_domain "$domain" || exit 1
done
```

#### **Cross-Domain Dependency Validation**
```bash
# Validate complex project references work
echo "🔗 Validating cross-domain dependencies..."

# EntityFramework → Audit → Foundation dependency chain
echo "Testing dependency chain: EntityFramework → Audit → Foundation"
dotnet build "EntityFramework/src/EntityFramework/Wangkanai.EntityFramework.csproj" --verbosity minimal

if [ $? -eq 0 ]; then
    echo "✅ Cross-domain dependency chain works"
else
    echo "❌ Cross-domain dependency chain broken"
    exit 1
fi
```

#### **Solution-Level Build**
```bash
# Full solution build
echo "🏗️ Building complete solution..."
dotnet build Foundation.slnx --verbosity minimal

if [ $? -eq 0 ]; then
    echo "✅ Complete solution builds successfully"
else
    echo "❌ Solution build failed"
    exit 1
fi
```

---

### **Phase 3: Test Validation**

#### **Domain Test Execution**
```bash
# Run tests for each domain
echo "🧪 Running domain tests..."

run_domain_tests() {
    domain=$1
    test_project=$(find "$domain/tests" -name "*.Tests.csproj" | head -1)
    
    if [ -f "$test_project" ]; then
        echo "Running tests for $domain..."
        dotnet test "$test_project" --logger "console;verbosity=minimal" --no-build
        
        if [ $? -eq 0 ]; then
            echo "✅ $domain tests passed"
        else
            echo "❌ $domain tests failed"
            return 1
        fi
    else
        echo "⚠️ No test project found for $domain"
    fi
}

# Run tests for all domains
for domain in "${domains[@]}"; do
    run_domain_tests "$domain"
done
```

#### **Solution-Level Testing**
```bash
# Run all tests via solution
echo "🔬 Running complete test suite..."
dotnet test Foundation.slnx --logger "console;verbosity=minimal" --no-build

if [ $? -eq 0 ]; then
    echo "✅ All tests pass"
else
    echo "❌ Some tests failed"
    exit 1
fi
```

---

### **Phase 4: Package Validation**

#### **Package Generation Test**
```bash
# Test package generation for each domain
echo "📦 Testing package generation..."

pack_domain() {
    domain=$1
    src_project=$(find "$domain/src" -name "*.csproj" | head -1)
    
    if [ -f "$src_project" ]; then
        echo "Packing $domain..."
        dotnet pack "$src_project" --configuration Release --output ./packages --verbosity minimal
        
        if [ $? -eq 0 ]; then
            echo "✅ $domain package generated successfully"
        else
            echo "❌ $domain package generation failed"
            return 1
        fi
    fi
}

# Clean and create packages directory
rm -rf packages/
mkdir -p packages/

# Pack all domains
for domain in "${domains[@]}"; do
    pack_domain "$domain" || exit 1
done

# List generated packages
echo "📋 Generated packages:"
ls -la packages/*.nupkg
```

---

### **Phase 5: Dependency Analysis**

#### **Project Reference Validation**
```bash
# Validate all project references are correct
echo "🔍 Analyzing project references..."

check_project_references() {
    project_file=$1
    echo "Checking references in $project_file"
    
    # Extract project references
    references=$(grep -o 'Include="[^"]*"' "$project_file" | sed 's/Include="//g' | sed 's/"//g')
    
    for ref in $references; do
        if [[ $ref == *".csproj" ]]; then
            # Check if referenced project exists
            if [ -f "$(dirname "$project_file")/$ref" ]; then
                echo "  ✅ $ref exists"
            else
                echo "  ❌ $ref missing or incorrect path"
            fi
        fi
    done
}

# Check all project files
find . -name "*.csproj" -exec bash -c 'check_project_references "$0"' {} \;
```

#### **Circular Dependency Check**
```bash
# Ensure no circular dependencies exist
echo "🔄 Checking for circular dependencies..."

# This is a simplified check - in practice, you might want a more sophisticated analysis
dotnet list package --include-transitive > dependency_report.txt

if grep -q "WARN" dependency_report.txt; then
    echo "⚠️ Potential dependency issues found"
    cat dependency_report.txt
else
    echo "✅ No obvious circular dependencies detected"
fi

rm -f dependency_report.txt
```

---

## 🎯 **Issue #50 Specific Validation**

#### **Hosting Dependency Check**
```bash
# Check for Microsoft.Extensions.Hosting dependencies in domain layer
echo "🚨 Checking Issue #50 status..."

echo "Searching for IHostedService usage in Foundation domain..."
if grep -r "IHostedService" Foundation/src/Domain/; then
    echo "❌ Issue #50: Found IHostedService coupling in domain layer"
    echo "Location: Foundation/src/Domain/Events/IEventListener.cs"
    echo "Status: UNRESOLVED - Domain still coupled with hosting infrastructure"
else
    echo "✅ Issue #50: No IHostedService coupling found in domain layer"
fi

# Check for Microsoft.Extensions.Hosting package reference
if grep -r "Microsoft.Extensions.Hosting" Foundation/src/Domain/Wangkanai.Foundation.Domain.csproj; then
    echo "❌ Domain project still references Microsoft.Extensions.Hosting"
else
    echo "✅ Domain project clean of hosting dependencies"
fi
```

---

## ✅ **Complete Validation Script**

#### **monorepo-validate.sh**
```bash
#!/bin/bash
# Complete validation script for Foundation monorepo

set -e

echo "🚀 Starting Foundation monorepo validation..."

# Phase 1: Structure
./validate-monorepo-structure.sh

# Phase 2: Builds  
echo -e "\n🔧 Phase 2: Build validation"
dotnet clean Foundation.slnx
dotnet restore Foundation.slnx
dotnet build Foundation.slnx --verbosity minimal

# Phase 3: Tests
echo -e "\n🧪 Phase 3: Test validation" 
dotnet test Foundation.slnx --logger "console;verbosity=minimal" --no-build

# Phase 4: Packages
echo -e "\n📦 Phase 4: Package validation"
rm -rf packages/ && mkdir packages/
dotnet pack Foundation.slnx --configuration Release --output ./packages --no-build --verbosity minimal

# Phase 5: Issue #50 Check
echo -e "\n🚨 Phase 5: Issue #50 validation"
if grep -r "IHostedService" Foundation/src/Domain/; then
    echo "❌ VALIDATION FAILED: Issue #50 unresolved"
    exit 1
else
    echo "✅ Issue #50 resolved"
fi

echo -e "\n🎉 Monorepo validation completed successfully!"
echo "📊 Summary:"
echo "  ✅ Structure validated"
echo "  ✅ All domains build"  
echo "  ✅ All tests pass"
echo "  ✅ Packages generate"
echo "  📋 Generated packages: $(ls packages/*.nupkg | wc -l)"
```

---

## 🎯 **Success Criteria**

### **Must Pass**
- [x] All domain directories exist with correct structure
- [x] All projects registered in Foundation.slnx  
- [x] Complete solution builds without errors
- [x] All tests execute and pass
- [x] Packages generate successfully

### **Issue #50 Resolution** 
- [ ] ❌ IEventListener no longer inherits from IHostedService
- [ ] ❌ Foundation.Domain project removes Microsoft.Extensions.Hosting dependency
- [ ] ❌ Separate Events package created or hosting abstraction implemented

---

## 📋 **Usage**

```bash
# Make script executable
chmod +x monorepo-validate.sh

# Run complete validation
./monorepo-validate.sh

# Run specific phases
./validate-monorepo-structure.sh    # Structure only
dotnet build Foundation.slnx         # Build only  
dotnet test Foundation.slnx          # Tests only
```

---

*This validation strategy ensures the monorepo structure is solid and ready for production use, while clearly identifying remaining work for Issue #50 resolution.*