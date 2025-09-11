#!/usr/bin/env python3

import os
import re

def modernize_exceptions():
    print("🚀 Starting exception handling modernization...")
    
    # Counters
    files_changed = 0
    total_replacements = 0
    
    # Walk through all directories
    for root, dirs, files in os.walk("."):
        # Skip build and system directories
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj', '.git', '.vs', 'packages', 'node_modules']]
        
        for file in files:
            if not file.endswith('.cs'):
                continue
                
            file_path = os.path.join(root, file)
            
            try:
                # Read file
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                original_content = content
                file_changes = 0
                
                # Pattern 1: if (param == null) throw new ArgumentNullException(nameof(param));
                pattern = r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;'
                matches = re.findall(pattern, content, re.IGNORECASE)
                if matches:
                    content = re.sub(pattern, r'ArgumentNullException.ThrowIfNull(\1);', content, flags=re.IGNORECASE)
                    file_changes += len(matches)
                
                # Pattern 2: Multi-line version
                pattern = r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;'
                matches = re.findall(pattern, content, re.MULTILINE | re.IGNORECASE)
                if matches:
                    content = re.sub(pattern, r'ArgumentNullException.ThrowIfNull(\1);', content, flags=re.MULTILINE | re.IGNORECASE)
                    file_changes += len(matches)
                
                # Pattern 3: 'is null' version
                pattern = r'if\s*\(\s*(\w+)\s+is\s+null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;'
                matches = re.findall(pattern, content, re.IGNORECASE)
                if matches:
                    content = re.sub(pattern, r'ArgumentNullException.ThrowIfNull(\1);', content, flags=re.IGNORECASE)
                    file_changes += len(matches)
                
                # Write back if changes were made
                if file_changes > 0:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    
                    rel_path = file_path[2:] if file_path.startswith('./') else file_path
                    print(f"✅ {rel_path}: {file_changes} replacements")
                    files_changed += 1
                    total_replacements += file_changes
                    
            except Exception as e:
                print(f"❌ Error processing {file_path}: {e}")
    
    print(f"\n🎯 Modernization Complete!")
    print(f"   • Files changed: {files_changed}")
    print(f"   • Total replacements: {total_replacements}")

if __name__ == "__main__":
    modernize_exceptions()