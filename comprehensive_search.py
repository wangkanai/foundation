#!/usr/bin/env python3
"""
Comprehensive search for all exception patterns in the codebase
"""

import os
import re
import glob
from pathlib import Path

def search_patterns(directory):
    """Search for all relevant patterns"""
    results = {
        'old_argumentnull': [],
        'new_throwijull': [],
        'argumentexception_old': [],
        'files_analyzed': 0,
        'total_lines': 0
    }
    
    # Old patterns to find
    old_patterns = [
        (r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(', 'if null == throw'),
        (r'if\s*\(\s*(\w+)\s+is\s+null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(', 'if is null throw'),
        (r'if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*{\s*throw\s+new\s+ArgumentNullException\s*\(', 'if null block throw'),
        (r'if\s*\(\s*string\.IsNullOrEmpty\s*\(\s*(\w+)\s*\)\s*\)\s*throw\s+new\s+ArgumentException\s*\(', 'if IsNullOrEmpty throw'),
    ]
    
    # New patterns to count
    new_patterns = [
        (r'\.ThrowIfNull\s*\(\s*\)', 'ThrowIfNull extension'),
        (r'ArgumentNullException\.ThrowIfNull\s*\(\s*(\w+)\s*\)', 'ArgumentNullException.ThrowIfNull'),
        (r'ArgumentException\.ThrowIfNullOrEmpty\s*\(\s*(\w+)\s*\)', 'ArgumentException.ThrowIfNullOrEmpty'),
    ]
    
    cs_files = list(Path(directory).glob('**/*.cs'))
    results['files_analyzed'] = len(cs_files)
    
    print(f"Analyzing {len(cs_files)} C# files...")
    
    for file_path in cs_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')
                results['total_lines'] += len(lines)
                
            # Search for old patterns
            for pattern, description in old_patterns:
                matches = re.finditer(pattern, content, re.IGNORECASE | re.MULTILINE)
                for match in matches:
                    line_num = content[:match.start()].count('\n') + 1
                    line_content = lines[line_num - 1].strip() if line_num <= len(lines) else ""
                    results['old_argumentnull'].append({
                        'file': str(file_path.relative_to(directory)),
                        'line': line_num,
                        'content': line_content,
                        'pattern': description,
                        'param': match.group(1) if match.groups() else 'unknown'
                    })
            
            # Search for new patterns
            for pattern, description in new_patterns:
                matches = re.finditer(pattern, content, re.IGNORECASE)
                for match in matches:
                    line_num = content[:match.start()].count('\n') + 1
                    line_content = lines[line_num - 1].strip() if line_num <= len(lines) else ""
                    results['new_throwijull'].append({
                        'file': str(file_path.relative_to(directory)),
                        'line': line_num,
                        'content': line_content,
                        'pattern': description
                    })
                        
        except Exception as e:
            print(f"Error reading {file_path}: {e}")
    
    return results

def main():
    foundation_dir = Path("/Users/wangkanai/Sources/foundation")
    results = search_patterns(foundation_dir)
    
    print("\n" + "="*80)
    print("COMPREHENSIVE ANALYSIS RESULTS")
    print("="*80)
    print(f"Files analyzed: {results['files_analyzed']}")
    print(f"Total lines of code: {results['total_lines']:,}")
    print(f"Old ArgumentNullException patterns: {len(results['old_argumentnull'])}")
    print(f"Modern ThrowIfNull patterns: {len(results['new_throwijull'])}")
    
    if results['old_argumentnull']:
        print("\n" + "="*80)
        print("OLD PATTERNS FOUND (TO MODERNIZE)")
        print("="*80)
        
        # Group by file
        by_file = {}
        for item in results['old_argumentnull']:
            if item['file'] not in by_file:
                by_file[item['file']] = []
            by_file[item['file']].append(item)
        
        for file_path, items in sorted(by_file.items()):
            print(f"\n{file_path} ({len(items)} patterns):")
            for item in items:
                print(f"  Line {item['line']:3}: {item['content']}")
                print(f"             → Parameter: {item['param']}, Pattern: {item['pattern']}")
    else:
        print("\n✅ NO OLD PATTERNS FOUND - Codebase already modernized!")
    
    if results['new_throwijull']:
        print(f"\n" + "="*80)
        print("MODERN PATTERNS FOUND (ALREADY UPDATED)")
        print("="*80)
        print(f"Total modern patterns: {len(results['new_throwijull'])}")
        
        # Show a sample
        for i, item in enumerate(results['new_throwijull'][:10]):
            print(f"  {item['file']}:{item['line']} - {item['pattern']}")
        
        if len(results['new_throwijull']) > 10:
            print(f"  ... and {len(results['new_throwijull']) - 10} more")

if __name__ == "__main__":
    main()