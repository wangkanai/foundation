#!/usr/bin/env python3
"""
Exception Handling Modernization Script
Finds and replaces old-style ArgumentNullException patterns with modern C# 11+ patterns
"""

import os
import re
import sys
from typing import List, Tuple

# Pattern matching for old-style exception handling
OLD_PATTERNS = [
    # Basic if-null-throw pattern
    (r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;', 
     r'ArgumentNullException.ThrowIfNull(\1);'),
    
    # Alternative with different whitespace
    (r'if\s*\(\s*(\w+)\s*is\s*null\s*\)\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;', 
     r'ArgumentNullException.ThrowIfNull(\1);'),
    
    # One-line version
    (r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;', 
     r'ArgumentNullException.ThrowIfNull(\1);'),
    
    # With braces
    (r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*\{\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;\s*\n\s*\}', 
     r'ArgumentNullException.ThrowIfNull(\1);'),
]

def find_cs_files(root_dir: str) -> List[str]:
    """Find all C# source files in the project."""
    cs_files = []
    for root, dirs, files in os.walk(root_dir):
        # Skip bin, obj, .git, and other non-source directories
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj', '.git', '.vs', 'packages', 'node_modules']]
        
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))
    
    return cs_files

def modernize_file(file_path: str) -> Tuple[bool, int]:
    """Modernize exception handling in a single file."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        replacements_made = 0
        
        for old_pattern, new_pattern in OLD_PATTERNS:
            matches = re.finditer(old_pattern, content, re.MULTILINE | re.DOTALL)
            match_count = len(list(matches))
            if match_count > 0:
                content = re.sub(old_pattern, new_pattern, content, flags=re.MULTILINE | re.DOTALL)
                replacements_made += match_count
        
        if replacements_made > 0:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            return True, replacements_made
        
        return False, 0
        
    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        return False, 0

def main():
    """Main function to modernize exception handling across the project."""
    project_root = "/Users/wangkanai/Sources/foundation"
    
    print("🔍 Finding C# source files...")
    cs_files = find_cs_files(project_root)
    print(f"Found {len(cs_files)} C# files to process")
    
    total_files_changed = 0
    total_replacements = 0
    
    print("\n📝 Processing files...")
    for file_path in cs_files:
        changed, replacements = modernize_file(file_path)
        if changed:
            print(f"✅ {file_path}: {replacements} replacements made")
            total_files_changed += 1
            total_replacements += replacements
        # Uncomment to see all files processed:
        # else:
        #     print(f"⏭️  {file_path}: no changes needed")
    
    print(f"\n🎯 Modernization Complete!")
    print(f"📊 Summary:")
    print(f"   • Files processed: {len(cs_files)}")
    print(f"   • Files changed: {total_files_changed}")
    print(f"   • Total replacements: {total_replacements}")

if __name__ == "__main__":
    main()