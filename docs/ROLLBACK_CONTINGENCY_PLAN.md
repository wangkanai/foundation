# Rollback & Contingency Plan - Foundation Restructure

## 🎯 **Overview**

Comprehensive safety net and recovery procedures for the Foundation restructuring process, ensuring safe rollback at any point
with minimal data loss.

---

## 🚨 **Emergency Rollback Procedures**

### **Immediate Rollback (Any Phase)**

If critical issues arise at any point during migration:

```bash
#!/bin/bash
# emergency-rollback.sh

echo "🚨 EMERGENCY ROLLBACK INITIATED"
echo "Timestamp: $(date)"

# Stop any running processes
pkill -f dotnet 2>/dev/null || true

# Stash any current work
git stash push -m "Emergency stash before rollback - $(date)"

# Return to known-good state
git checkout backup/pre-foundation-restructure
git checkout -b emergency-rollback-$(date +%Y%m%d-%H%M%S)

# Verify we're in good state
if dotnet build --verbosity minimal > /dev/null 2>&1; then
    echo "✅ Successfully rolled back to working state"
    echo "Current branch: $(git branch --show-current)"
else
    echo "❌ Rollback state has issues - check backup integrity"
    exit 1
fi

# Push emergency branch
git push -u origin $(git branch --show-current)

echo "🔄 Emergency rollback complete"
echo "Next steps:"
echo "1. Analyze what went wrong"
echo "2. Fix issues in this branch"
echo "3. Resume migration when ready"
```

### **Quick Recovery Commands**

```bash
# One-liner emergency rollback
git stash && git checkout backup/pre-foundation-restructure && git checkout -b emergency-rollback-$(date +%s)

# Verify rollback success
dotnet build && dotnet test && echo "✅ Rollback successful"
```

---

## 📋 **Staged Rollback Points**

### **Rollback Point 1: After Backup Creation**

**What's Safe to Rollback:** All changes (nothing permanent yet)
**Recovery Method:**

```bash
git checkout main
git branch -D feature/foundation-restructure 2>/dev/null || true
echo "✅ Clean slate - can restart migration"
```

### **Rollback Point 2: After Structure Creation**

**What's Safe to Rollback:** New directories and project files
**Recovery Method:**

```bash
# Remove new structure
rm -rf src/Foundation tests/Foundation benchmark/Foundation

# Clean git state
git reset --hard HEAD~1  # If committed
# OR
git restore . && git clean -fd  # If not committed

echo "✅ Structure removed - back to original state"
```

### **Rollback Point 3: After Content Migration**

**What's Safe to Rollback:** All copied content and namespace changes
**Recovery Method:**

```bash
# If migration went wrong, remove Foundation directory
rm -rf src/Foundation tests/Foundation

# Restore original files if accidentally modified
git checkout HEAD -- src/Domain src/Audit src/EntityFramework
git checkout HEAD -- tests/Domain tests/Audit tests/EntityFramework

echo "✅ Content migration rolled back"
```

### **Rollback Point 4: After Solution Update**

**What's Safe to Rollback:** Solution file changes
**Recovery Method:**

```bash
# Restore original solution file
git checkout HEAD -- Domain.slnx

# Remove any new projects from solution
dotnet sln remove src/Foundation/*/*.csproj 2>/dev/null || true
dotnet sln remove tests/Foundation/*/*.csproj 2>/dev/null || true

echo "✅ Solution changes rolled back"
```

---

## 🛡️ **Data Protection Strategy**

### **Critical Backup Points**

1. **Pre-migration snapshot** (`backup/pre-foundation-restructure` branch)
2. **Tagged version** (`v5.0.0-pre-foundation` tag)
3. **Phase checkpoints** (automatic git stashes)
4. **Working state backups** (manual stashes before risky operations)

### **Backup Verification**

```bash
#!/bin/bash
# verify-backups.sh

echo "🔍 Verifying backup integrity..."

# Check backup branch exists and builds
if git checkout backup/pre-foundation-restructure > /dev/null 2>&1; then
    if dotnet build --verbosity minimal > /dev/null 2>&1; then
        echo "✅ Backup branch: VALID"
    else
        echo "❌ Backup branch: BUILD FAILS"
        exit 1
    fi
    git checkout - > /dev/null 2>&1
else
    echo "❌ Backup branch: MISSING"
    exit 1
fi

# Check backup tag exists
if git tag | grep -q "v5.0.0-pre-foundation"; then
    echo "✅ Backup tag: EXISTS"
else
    echo "❌ Backup tag: MISSING"
    exit 1
fi

# Check stash backups
stash_count=$(git stash list | grep -c "foundation" || echo "0")
echo "📦 Foundation stashes available: $stash_count"

echo "🛡️ Backup verification complete"
```

---

## 🔄 **Progressive Recovery Strategy**

### **Level 1: Soft Recovery**

For minor issues (build errors, test failures):

```bash
# Fix current issue without full rollback
echo "🔧 Attempting soft recovery..."

# Clean and rebuild
dotnet clean
rm -rf bin/ obj/ packages/
dotnet restore
dotnet build

# If that fails, try selective reset
git reset --soft HEAD~1
git restore src/Foundation/ --staged
```

### **Level 2: Partial Rollback**

For specific component failures:

```bash
# Roll back specific package while keeping others
echo "🔄 Partial rollback for specific package..."

# Example: Roll back Events package but keep others
rm -rf src/Foundation/Events
git checkout HEAD -- src/Domain/Events src/Domain/Infrastructure

# Rebuild without problematic component
dotnet build
```

### **Level 3: Phase Rollback**

Return to previous successful phase:

```bash
# Roll back to last known-good checkpoint
echo "⏪ Rolling back to previous phase..."

# Find last successful checkpoint
last_checkpoint=$(git log --oneline | grep -E "(Phase [0-9]|checkpoint)" | head -1 | cut -d' ' -f1)

if [ -n "$last_checkpoint" ]; then
    git reset --hard "$last_checkpoint"
    echo "✅ Rolled back to: $last_checkpoint"
else
    echo "⚠️ No checkpoint found - performing full rollback"
    git checkout backup/pre-foundation-restructure
fi
```

### **Level 4: Full Rollback**

Complete return to original state:

```bash
echo "🚨 Full rollback to original state..."

# Return to backup
git checkout backup/pre-foundation-restructure
git checkout -b recovery-$(date +%Y%m%d-%H%M%S)

# Verify state
dotnet build && dotnet test
echo "✅ Full rollback complete - back to original state"
```

---

## 🏥 **Issue-Specific Recovery**

### **Build Failures**

```bash
# Common build failure recovery
echo "🔨 Recovering from build failures..."

# Clear build artifacts
dotnet clean
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# Restore packages
rm -rf ~/.nuget/packages/wangkanai.* 2>/dev/null || true
dotnet restore --force

# Try build again
dotnet build --verbosity detailed
```

### **Test Failures**

```bash
# Test failure recovery
echo "🧪 Recovering from test failures..."

# Check if namespace issues
if grep -r "using Wangkanai\.Domain" tests/Foundation/ | grep -v "Foundation.Domain"; then
    echo "🔧 Fixing namespace issues..."
    find tests/Foundation -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Domain;/using Wangkanai.Foundation.Domain;/g' {} \;
    find tests/Foundation -name "*.cs" -exec sed -i '' 's/using Wangkanai\.Audit;/using Wangkanai.Foundation.Audit;/g' {} \;
fi

# Re-run tests
dotnet test --verbosity detailed
```

### **Package Generation Failures**

```bash
# Package generation recovery
echo "📦 Recovering from package failures..."

# Clean package output
rm -rf packages/ bin/ obj/

# Check project references
for proj in src/Foundation/*/*.csproj; do
    echo "Checking $proj..."
    dotnet build "$proj" --verbosity minimal || echo "❌ Failed: $proj"
done

# Try pack again
dotnet pack --configuration Release --output ./packages
```

### **Git State Corruption**

```bash
# Git state recovery
echo "🔧 Recovering from git issues..."

# Reset any staged changes
git reset HEAD .

# Clean working directory
git clean -fd

# If really corrupted, start fresh
if ! git status > /dev/null 2>&1; then
    echo "🚨 Git state corrupted - restoring from backup"
    cd ..
    rm -rf domain-corrupted
    mv domain domain-corrupted
    git clone https://github.com/wangkanai/domain.git
    cd domain
    git checkout backup/pre-foundation-restructure
fi
```

---

## 📊 **Recovery Decision Matrix**

| Issue Type             | Severity | Recovery Method     | Time Cost |
|------------------------|----------|---------------------|-----------|
| Build warning          | Low      | Continue migration  | 5 min     |
| Single test failure    | Low      | Fix test + continue | 15 min    |
| Multiple test failures | Medium   | Partial rollback    | 30 min    |
| Package gen failure    | Medium   | Phase rollback      | 45 min    |
| Namespace issues       | Medium   | Automated fix       | 20 min    |
| Major build failure    | High     | Phase rollback      | 1 hour    |
| Git corruption         | High     | Full rollback       | 2 hours   |
| Data loss              | Critical | Emergency rollback  | 15 min    |

---

## 🎯 **Prevention Strategies**

### **Checkpoint Creation**

Automatic checkpoints at key phases:

```bash
# Auto-checkpoint script
checkpoint_phase() {
    local phase_name="$1"
    git add -A
    git commit -m "checkpoint: $phase_name complete - $(date)"
    git tag "checkpoint-$phase_name-$(date +%Y%m%d-%H%M%S)"
    echo "✅ Checkpoint created: $phase_name"
}

# Usage in migration script
checkpoint_phase "structure-creation"
checkpoint_phase "content-migration"
checkpoint_phase "namespace-updates"
```

### **Validation Gates**

Prevent progression with failing validation:

```bash
# Validation gate
validate_before_continue() {
    if ! dotnet build --verbosity minimal > /dev/null 2>&1; then
        echo "❌ Build failing - cannot continue"
        echo "Run rollback or fix issues before proceeding"
        exit 1
    fi

    if ! dotnet test --verbosity minimal > /dev/null 2>&1; then
        echo "❌ Tests failing - cannot continue"
        echo "Run rollback or fix issues before proceeding"
        exit 1
    fi

    echo "✅ Validation passed - safe to continue"
}
```

### **Parallel Work Protection**

Protect against accidental parallel changes:

```bash
# Work protection
if [ -f ".migration-in-progress" ]; then
    echo "⚠️ Migration already in progress!"
    echo "Complete current migration or run rollback first"
    exit 1
fi

# Create lock file
echo "$(date)" > .migration-in-progress
trap 'rm -f .migration-in-progress' EXIT
```

---

## 🚀 **Recovery Testing**

### **Test Recovery Procedures**

```bash
#!/bin/bash
# test-recovery.sh

echo "🧪 Testing recovery procedures..."

# Test emergency rollback
git checkout -b test-recovery-$(date +%s)
echo "test change" > test-file.txt
git add test-file.txt
git commit -m "test change"

# Simulate emergency rollback
git stash && git checkout backup/pre-foundation-restructure

# Verify we're back to good state
if dotnet build && dotnet test; then
    echo "✅ Emergency rollback test: PASSED"
else
    echo "❌ Emergency rollback test: FAILED"
fi

# Cleanup test
git checkout main
git branch -D test-recovery-* 2>/dev/null || true
```

### **Recovery Time Estimates**

- **Emergency rollback**: 2-5 minutes
- **Phase rollback**: 10-15 minutes
- **Partial recovery**: 15-30 minutes
- **Full state rebuild**: 30-60 minutes

---

## ✅ **Recovery Success Criteria**

### **Post-Recovery Validation**

After any recovery operation:

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes
- [ ] All original functionality works
- [ ] Git state is clean
- [ ] Backup integrity maintained
- [ ] No data loss occurred
- [ ] Team notified of recovery action

### **Documentation Requirements**

After recovery:

- Document what went wrong
- Update procedures based on lessons learned
- Add new prevention measures
- Share knowledge with team

---

*This comprehensive rollback strategy ensures safe recovery from any point in the migration process with minimal disruption and
data protection.*