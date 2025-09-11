#!/usr/bin/env python3
"""
Search for old-style ArgumentNullException patterns in the codebase
"""

import os
import re
import glob

def find_old_patterns(directory):
    """Find all C# files with old ArgumentNullException patterns"""
    patterns = [
        # Match: if (param == null) throw new ArgumentNullException(nameof(param));
        r'if\s*\(\s*\w+\s*==\s*null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(',
        # Match: if (param == null) { throw new ArgumentNullException(nameof(param)); }
        r'if\s*\(\s*\w+\s*==\s*null\s*\)\s*{\s*throw\s+new\s+ArgumentNullException\s*\(',
        # Match: if (param is null) throw new ArgumentNullException(nameof(param));
        r'if\s*\(\s*\w+\s+is\s+null\s*\)\s*throw\s+new\s+ArgumentNullException\s*\(',
        # Match: if (param == null) \n    throw new ArgumentNullException(nameof(param));
        r'if\s*\(\s*\w+\s*==\s*null\s*\)\s*\n\s*throw\s+new\s+ArgumentNullException\s*\(',
    ]
    
    results = []
    cs_files = glob.glob(os.path.join(directory, '**', '*.cs'), recursive=True)
    
    for file_path in cs_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            for pattern in patterns:
                matches = re.finditer(pattern, content, re.IGNORECASE | re.MULTILINE)
                for match in matches:
                    line_num = content[:match.start()].count('\n') + 1
                    line_content = content.split('\n')[line_num - 1].strip()
                    results.append({
                        'file': file_path.replace(directory, '').lstrip(os.sep),
                        'line': line_num,
                        'content': line_content,
                        'match': match.group(0)
                    })
        except Exception as e:
            print(f"Error reading {file_path}: {e}")
    
    return results

if __name__ == "__main__":
    foundation_dir = "/Users/wangkanai/Sources/foundation"
    results = find_old_patterns(foundation_dir)
    
    print(f"Found {len(results)} old ArgumentNullException patterns:")
    print("=" * 80)
    
    # Group by file
    by_file = {}
    for result in results:
        if result['file'] not in by_file:
            by_file[result['file']] = []
        by_file[result['file']].append(result)
    
    for file_path, matches in by_file.items():
        print(f"\n{file_path} ({len(matches)} matches):")
        for match in matches:
            print(f"  Line {match['line']}: {match['content']}")