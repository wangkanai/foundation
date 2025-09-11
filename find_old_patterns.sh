#!/bin/bash

# Find all C# files and search for old exception handling patterns
echo "🔍 Searching for old exception handling patterns in C# files..."
echo ""

# Pattern 1: if (param == null) throw new ArgumentNullException(nameof(param));
echo "Pattern 1: if-null-throw with nameof"
find . -name "*.cs" -type f -not -path "./bin/*" -not -path "./obj/*" -not -path "./.git/*" -exec grep -l "if.*==.*null.*throw.*new.*ArgumentNullException.*nameof" {} \; 2>/dev/null | head -20

echo ""

# Pattern 2: More specific search for multiline patterns
echo "Pattern 2: Multiline if-null-throw patterns"  
find . -name "*.cs" -type f -not -path "./bin/*" -not -path "./obj/*" -not -path "./.git/*" -exec grep -l -B1 -A1 "throw new ArgumentNullException" {} \; 2>/dev/null | head -20

echo ""

# Pattern 3: Count total occurrences
echo "Pattern 3: Total files with ArgumentNullException"
TOTAL=$(find . -name "*.cs" -type f -not -path "./bin/*" -not -path "./obj/*" -not -path "./.git/*" -exec grep -l "ArgumentNullException" {} \; 2>/dev/null | wc -l)
echo "Files containing ArgumentNullException: $TOTAL"

echo ""

# Pattern 4: Show actual content snippets
echo "Pattern 4: Sample snippets with ArgumentNullException"
find . -name "*.cs" -type f -not -path "./bin/*" -not -path "./obj/*" -not -path "./.git/*" -exec grep -n -B2 -A1 "ArgumentNullException" {} + 2>/dev/null | head -30