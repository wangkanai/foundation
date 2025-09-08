# Validation & Testing Strategy - Foundation Restructure

## 🎯 **Overview**
Comprehensive validation strategy to ensure safe migration from `Wangkanai.*` to `Wangkanai.Foundation.*` hierarchy without breaking functionality.

---

## 🧪 **Testing Phases**

### **Phase 1: Pre-Migration Baseline**
Establish baseline before any changes to compare against.

#### Baseline Commands
```bash
# Record current state
echo "=== PRE-MIGRATION BASELINE ===" > validation_log.txt
echo "Date: $(date)" >> validation_log.txt
echo "" >> validation_log.txt

# Current build status
echo "🔧 Build Status:" >> validation_log.txt
dotnet build --verbosity minimal >> validation_log.txt 2>&1

# Current test status  
echo -e "\n🧪 Test Status:" >> validation_log.txt
dotnet test --logger "console;verbosity=minimal" >> validation_log.txt 2>&1

# Current package versions
echo -e "\n📦 Package Versions:" >> validation_log.txt
grep -r "VersionPrefix" src/*/Wangkanai.*.csproj >> validation_log.txt

# Test count baseline
echo -e "\n📊 Test Metrics:" >> validation_log.txt
echo "Total test projects: $(find tests -name "*.csproj" | wc -l)" >> validation_log.txt
echo "Total test files: $(find tests -name "*Tests.cs" | wc -l)" >> validation_log.txt
```

#### Success Criteria
- ✅ All builds pass
- ✅ All tests pass  
- ✅ Current versions documented
- ✅ Test counts recorded

---

### **Phase 2: Incremental Validation**
Validate at each major migration step.

#### After Project Structure Creation
```bash
echo -e "\n=== POST-STRUCTURE CREATION ===" >> validation_log.txt

# Verify new directories created
echo "📁 Directory Structure:" >> validation_log.txt
tree src/Foundation tests/Foundation >> validation_log.txt 2>&1

# Verify project files created
echo -e "\n📄 Project Files:" >> validation_log.txt
find src/Foundation -name "*.csproj" >> validation_log.txt
find tests/Foundation -name "*.csproj" >> validation_log.txt
```

#### After Content Migration
```bash
echo -e "\n=== POST-CONTENT MIGRATION ===" >> validation_log.txt

# File count verification
echo "📊 File Migration Metrics:" >> validation_log.txt
echo "Domain .cs files: $(find src/Foundation/Domain -name "*.cs" | wc -l)" >> validation_log.txt  
echo "Audit .cs files: $(find src/Foundation/Audit -name "*.cs" | wc -l)" >> validation_log.txt
echo "EF .cs files: $(find src/Foundation/EntityFramework -name "*.cs" | wc -l)" >> validation_log.txt

# Namespace validation
echo -e "\n🏷️ Namespace Validation:" >> validation_log.txt
echo "Old Domain namespaces remaining: $(grep -r "namespace Wangkanai\.Domain" src/Foundation/ | wc -l)" >> validation_log.txt
echo "Old Audit namespaces remaining: $(grep -r "namespace Wangkanai\.Audit" src/Foundation/ | wc -l)" >> validation_log.txt
echo "Old EF namespaces remaining: $(grep -r "namespace Wangkanai\.EntityFramework" src/Foundation/ | wc -l)" >> validation_log.txt
```

#### After Each Build Attempt
```bash
echo -e "\n=== BUILD VALIDATION ===" >> validation_log.txt

# Build individual projects
for project in src/Foundation/*/Wangkanai.Foundation.*.csproj; do
    echo "Building $(basename $project):" >> validation_log.txt
    dotnet build "$project" --verbosity minimal >> validation_log.txt 2>&1
    echo "Exit code: $?" >> validation_log.txt
    echo "" >> validation_log.txt
done

# Build solution
echo "Building complete solution:" >> validation_log.txt  
dotnet build --verbosity minimal >> validation_log.txt 2>&1
echo "Exit code: $?" >> validation_log.txt
```

---

### **Phase 3: Functional Testing**

#### Unit Test Validation
```bash
echo -e "\n=== UNIT TEST VALIDATION ===" >> validation_log.txt

# Test individual projects
for project in tests/Foundation/*/Wangkanai.Foundation.*.Tests.csproj; do
    echo "Testing $(basename $project):" >> validation_log.txt
    dotnet test "$project" --logger "console;verbosity=minimal" >> validation_log.txt 2>&1
    echo "" >> validation_log.txt
done

# Test complete solution
echo "Testing complete solution:" >> validation_log.txt
dotnet test --logger "console;verbosity=minimal" >> validation_log.txt 2>&1
```

#### Package Generation Test
```bash
echo -e "\n=== PACKAGE GENERATION TEST ===" >> validation_log.txt

# Clean previous packages
rm -rf packages/ 2>/dev/null || true
mkdir -p packages

# Pack individual projects
for project in src/Foundation/*/Wangkanai.Foundation.*.csproj; do
    echo "Packing $(basename $project):" >> validation_log.txt
    dotnet pack "$project" --configuration Release --output ./packages --verbosity minimal >> validation_log.txt 2>&1
    echo "Exit code: $?" >> validation_log.txt
done

# List generated packages
echo -e "\n📦 Generated Packages:" >> validation_log.txt
ls -la packages/*.nupkg >> validation_log.txt 2>&1
```

---

### **Phase 4: Integration Testing**

#### Consumer Project Simulation
Create a minimal test consumer to validate the new packages work correctly.

```bash
# Create test consumer project
mkdir -p test-consumer
cd test-consumer

cat > TestConsumer.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../src/Foundation/Metapackage/Wangkanai.Foundation.csproj" />
  </ItemGroup>
</Project>
EOF

cat > Program.cs << 'EOF'
using Wangkanai.Foundation.Domain;
using Wangkanai.Foundation.Audit;
using Wangkanai.Foundation.EntityFramework;

Console.WriteLine("Testing Wangkanai Foundation packages...");

// Test Domain
var entity = new TestEntity(1);
Console.WriteLine($"Entity created with ID: {entity.Id}");

// Test that all namespaces work
Console.WriteLine("✅ All Foundation packages loaded successfully!");

public class TestEntity : Entity<int>
{
    public TestEntity(int id) : base(id) { }
}
EOF

# Build and run test consumer
echo -e "\n=== CONSUMER INTEGRATION TEST ===" >> ../validation_log.txt
dotnet build >> ../validation_log.txt 2>&1
dotnet run >> ../validation_log.txt 2>&1
echo "Consumer test exit code: $?" >> ../validation_log.txt

cd ..
```

---

### **Phase 5: Regression Testing**

#### Compare Against Baseline
```bash
echo -e "\n=== REGRESSION ANALYSIS ===" >> validation_log.txt

# Compare test counts
original_tests=$(grep "Total test files" validation_log.txt | head -1 | awk '{print $4}')
current_tests=$(find tests/Foundation -name "*Tests.cs" | wc -l)
echo "Original tests: $original_tests, Current tests: $current_tests" >> validation_log.txt

# Compare build success
echo "Build regression check:" >> validation_log.txt
if dotnet build --verbosity minimal > /dev/null 2>&1; then
    echo "✅ Build still passes" >> validation_log.txt
else
    echo "❌ Build regression detected!" >> validation_log.txt
fi

# Compare test success  
echo "Test regression check:" >> validation_log.txt
if dotnet test --verbosity minimal > /dev/null 2>&1; then
    echo "✅ Tests still pass" >> validation_log.txt
else
    echo "❌ Test regression detected!" >> validation_log.txt
fi
```

---

## 🚨 **Automated Validation Script**

Complete validation script to run at each checkpoint:

```bash
#!/bin/bash
# validate-foundation.sh

set -e

VALIDATION_LOG="validation_log.txt"
PHASE="$1"

echo "=== VALIDATION PHASE: $PHASE ===" >> $VALIDATION_LOG
echo "Timestamp: $(date)" >> $VALIDATION_LOG
echo "" >> $VALIDATION_LOG

# Function to check command success
check_command() {
    local cmd="$1"
    local description="$2"
    
    echo "🔍 $description..." | tee -a $VALIDATION_LOG
    if eval "$cmd" >> $VALIDATION_LOG 2>&1; then
        echo "✅ $description: PASSED" | tee -a $VALIDATION_LOG
        return 0
    else
        echo "❌ $description: FAILED" | tee -a $VALIDATION_LOG
        return 1
    fi
}

# Core validations
check_command "dotnet restore" "Package restore"
check_command "dotnet build --verbosity minimal" "Solution build"
check_command "dotnet test --verbosity minimal" "Unit tests"

# Structure validations
if [ -d "src/Foundation" ]; then
    check_command "find src/Foundation -name '*.csproj' | wc -l | grep -q '[1-9]'" "Foundation projects exist"
fi

if [ -d "tests/Foundation" ]; then
    check_command "find tests/Foundation -name '*.csproj' | wc -l | grep -q '[1-9]'" "Foundation tests exist"  
fi

# Namespace validations (after migration)
if [ "$PHASE" = "post-migration" ]; then
    old_namespaces=$(grep -r "namespace Wangkanai\.[DAE]" src/Foundation/ 2>/dev/null | wc -l)
    if [ $old_namespaces -eq 0 ]; then
        echo "✅ Namespace migration: COMPLETE" | tee -a $VALIDATION_LOG
    else
        echo "❌ Namespace migration: INCOMPLETE ($old_namespaces remaining)" | tee -a $VALIDATION_LOG
        exit 1
    fi
fi

# Package generation (final phase)
if [ "$PHASE" = "final" ]; then
    check_command "dotnet pack --configuration Release --output ./packages --verbosity minimal" "Package generation"
    check_command "ls packages/*.nupkg | wc -l | grep -q '[1-9]'" "Packages created"
fi

echo "" >> $VALIDATION_LOG
echo "Phase $PHASE validation completed successfully!" | tee -a $VALIDATION_LOG
echo "=======================================" >> $VALIDATION_LOG
echo "" >> $VALIDATION_LOG
```

#### Usage
```bash
# Make executable
chmod +x validate-foundation.sh

# Run at each phase
./validate-foundation.sh "baseline"
./validate-foundation.sh "structure-created"  
./validate-foundation.sh "content-migrated"
./validate-foundation.sh "post-migration"
./validate-foundation.sh "final"
```

---

## 📊 **Success Metrics**

### Critical Success Indicators
- ✅ **Zero build errors** across all projects
- ✅ **All unit tests pass** (same count as baseline)
- ✅ **All packages generate successfully**
- ✅ **Consumer integration test passes**
- ✅ **No old namespaces remain** in Foundation projects

### Performance Metrics
- ✅ **Build time** ≤ baseline + 20%
- ✅ **Test execution time** ≤ baseline + 20%  
- ✅ **Package size** ≤ baseline + 10%

### Quality Metrics
- ✅ **Test coverage maintained** (same % as baseline)
- ✅ **No new warnings** introduced
- ✅ **All analyzers pass** (EnforceExtendedAnalyzerRules)

---

## ⚠️ **Failure Response**

### If Validation Fails
```bash
# Document failure
echo "❌ VALIDATION FAILURE at phase: $PHASE" >> validation_log.txt
echo "Timestamp: $(date)" >> validation_log.txt

# Collect diagnostic info
echo "=== DIAGNOSTIC INFO ===" >> validation_log.txt
dotnet build --verbosity diagnostic >> validation_log.txt 2>&1

# Immediate rollback
git stash
git checkout backup/pre-foundation-restructure
git checkout -b hotfix/validation-failure-$(date +%s)

echo "🚨 Validation failed! Rolled back to backup branch."
echo "Check validation_log.txt for details."
```

### Recovery Process
1. **Analyze** validation_log.txt for specific failures
2. **Fix** identified issues in backup branch
3. **Re-run** validation on fixes
4. **Resume** migration process once issues resolved

---

## ✅ **Final Validation Checklist**

Before considering migration complete:

- [ ] All Foundation projects build successfully
- [ ] All Foundation tests pass
- [ ] All Foundation packages generate
- [ ] Consumer project works with new packages  
- [ ] No old namespaces remain in Foundation code
- [ ] Documentation updated with new package names
- [ ] Migration guide created for consumers
- [ ] Git history clean with proper tags
- [ ] Backup branches preserved for rollback

---

*This validation strategy ensures safe, methodical migration with multiple checkpoints and clear rollback paths.*