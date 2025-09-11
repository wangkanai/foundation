# Exception Modernization Analysis

## Current Situation Assessment

Based on my analysis of the wangkanai/foundation codebase:

### Project Configuration

- **Framework**: .NET 9.0 with nullable reference types enabled
- **Modern Features**: Uses `ImplicitUsings` and modern C# patterns
- **Dependencies**: Uses `Wangkanai.System` and `Wangkanai.Validation` packages
- **Branch**: Currently on `coverage` branch

### Code Analysis Findings

#### Already Modernized Patterns

1. **ThrowIfNull Extensions**: Already using `.ThrowIfNull()` extensions from `Wangkanai.System`
   - Found in: `ReflectionExtensions.cs` line 57: `obj.ThrowIfNull();`
   - Found in: `DatabaseBuilderExtensions.cs` lines 17, 30, 43, 56: `context.ThrowIfNull();`

#### Architecture Quality

- Well-structured domain-driven design patterns
- Proper separation of concerns
- Modern C# performance optimizations (caching, compiled expressions)
- Comprehensive documentation

### Initial Assessment Conclusion

**The codebase appears to be significantly more modernized than initially described.**

The presence of:

- `Wangkanai.System` package (which provides modern guard clauses)
- Extensive use of `.ThrowIfNull()` patterns
- Recent project file modifications
- .NET 9.0 target framework

Suggests that much of the modernization work may have already been completed.

## Recommended Next Steps

1. **Comprehensive Pattern Search**: Run systematic search to identify remaining old patterns
2. **Validate Target**: Confirm actual count of remaining old-style ArgumentNullException patterns
3. **Prioritize by Impact**: Focus on high-usage files and public APIs
4. **Batch Updates**: Use MultiEdit for efficient bulk transformations

## Search Strategy

Target patterns to find and modernize:

- `if (param == null) throw new ArgumentNullException(nameof(param));`
- `if (param is null) throw new ArgumentNullException(nameof(param));`
- `if (string.IsNullOrEmpty(param)) throw new ArgumentException(...);`

Replace with:

- `ArgumentNullException.ThrowIfNull(param);`
- `ArgumentException.ThrowIfNullOrEmpty(param);`