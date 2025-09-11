#!/usr/bin/env python3
"""
Comprehensive Exception Handling Modernization
Handles all variations of old ArgumentNullException patterns
"""

import os
import re
from pathlib import Path
import argparse

class ExceptionModernizer:
    def __init__(self, root_dir: str, dry_run: bool = False):
        self.root_dir = Path(root_dir)
        self.dry_run = dry_run
        self.total_files_processed = 0
        self.total_files_changed = 0
        self.total_replacements = 0
        self.changes_by_file = {}

    def get_cs_files(self):
        """Get all C# source files, excluding build artifacts."""
        excluded_dirs = {'bin', 'obj', '.git', '.vs', 'packages', 'node_modules', 'TestResults'}
        cs_files = []
        
        for file_path in self.root_dir.rglob('*.cs'):
            if not any(excluded in file_path.parts for excluded in excluded_dirs):
                cs_files.append(file_path)
        
        return cs_files

    def modernize_patterns(self, content: str, file_path: str) -> tuple[str, int]:
        """Apply all modernization patterns to the content."""
        original_content = content
        replacements = 0
        
        # Pattern 1: Basic if-null-throw (single line)
        pattern1 = re.compile(
            r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;',
            re.IGNORECASE
        )
        matches = pattern1.findall(content)
        if matches:
            content = pattern1.sub(r'ArgumentNullException.ThrowIfNull(\1);', content)
            replacements += len(matches)

        # Pattern 2: Multi-line if-null-throw
        pattern2 = re.compile(
            r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;',
            re.MULTILINE | re.IGNORECASE
        )
        matches = pattern2.findall(content)
        if matches:
            content = pattern2.sub(r'ArgumentNullException.ThrowIfNull(\1);', content)
            replacements += len(matches)

        # Pattern 3: With braces
        pattern3 = re.compile(
            r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*\{\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;\s*\n\s*\}',
            re.MULTILINE | re.DOTALL | re.IGNORECASE
        )
        matches = pattern3.findall(content)
        if matches:
            content = pattern3.sub(r'ArgumentNullException.ThrowIfNull(\1);', content)
            replacements += len(matches)

        # Pattern 4: Using 'is null' instead of '== null'
        pattern4 = re.compile(
            r'if\s*\(\s*(\w+)\s+is\s+null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;',
            re.IGNORECASE
        )
        matches = pattern4.findall(content)
        if matches:
            content = pattern4.sub(r'ArgumentNullException.ThrowIfNull(\1);', content)
            replacements += len(matches)

        # Pattern 5: Multi-line with 'is null'
        pattern5 = re.compile(
            r'if\s*\(\s*(\w+)\s+is\s+null\s*\)\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(\s*nameof\s*\(\s*\1\s*\)\s*\)\s*;',
            re.MULTILINE | re.IGNORECASE
        )
        matches = pattern5.findall(content)
        if matches:
            content = pattern5.sub(r'ArgumentNullException.ThrowIfNull(\1);', content)
            replacements += len(matches)

        # Pattern 6: Alternative with string parameter (less common but possible)
        pattern6 = re.compile(
            r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(\s*"(\w+)"\s*\)\s*;',
            re.IGNORECASE
        )
        matches = pattern6.findall(content)
        if matches:
            content = pattern6.sub(r'ArgumentNullException.ThrowIfNull(\1);', content)
            replacements += len(matches)

        return content, replacements

    def process_file(self, file_path: Path) -> bool:
        """Process a single file."""
        try:
            # Read the file
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                original_content = f.read()

            # Apply modernization
            new_content, replacements = self.modernize_patterns(original_content, str(file_path))
            
            self.total_files_processed += 1

            if replacements > 0:
                self.total_files_changed += 1
                self.total_replacements += replacements
                self.changes_by_file[str(file_path)] = replacements

                if not self.dry_run:
                    # Write back the modified content
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(new_content)
                
                return True

            return False

        except Exception as e:
            print(f"❌ Error processing {file_path}: {e}")
            return False

    def run(self):
        """Run the modernization process."""
        print("🔍 Finding C# source files...")
        cs_files = self.get_cs_files()
        print(f"Found {len(cs_files)} C# files to process")

        if self.dry_run:
            print("🔍 DRY RUN MODE - No files will be modified")

        print("\n📝 Processing files...")
        
        for file_path in cs_files:
            if self.process_file(file_path):
                rel_path = file_path.relative_to(self.root_dir)
                replacements = self.changes_by_file[str(file_path)]
                status = "🔍 WOULD CHANGE" if self.dry_run else "✅ CHANGED"
                print(f"{status} {rel_path}: {replacements} replacements")

        # Print summary
        print(f"\n🎯 Modernization {'Analysis' if self.dry_run else 'Complete'}!")
        print(f"📊 Summary:")
        print(f"   • Files processed: {self.total_files_processed}")
        print(f"   • Files {'would be changed' if self.dry_run else 'changed'}: {self.total_files_changed}")
        print(f"   • Total replacements: {self.total_replacements}")

        if self.changes_by_file:
            print(f"\n📄 Files with changes:")
            for file_path, count in self.changes_by_file.items():
                rel_path = Path(file_path).relative_to(self.root_dir)
                print(f"   • {rel_path}: {count} replacements")

def main():
    parser = argparse.ArgumentParser(description="Modernize C# exception handling patterns")
    parser.add_argument("--root", default="/Users/wangkanai/Sources/foundation", 
                       help="Root directory to process")
    parser.add_argument("--dry-run", action="store_true", 
                       help="Show what would be changed without modifying files")
    
    args = parser.parse_args()

    modernizer = ExceptionModernizer(args.root, args.dry_run)
    modernizer.run()

if __name__ == "__main__":
    main()