# Task Completion Checklist

## Before Completing Any Development Task

### 1. Build Verification

```bash
# Ensure clean build
dotnet clean Domain.slnx -c Release
dotnet build Domain.slnx -c Release
```

### 2. Test Execution

```bash
# Run all tests and ensure they pass
dotnet test Domain.slnx --configuration Release --no-build

# For coverage-sensitive changes, run with coverage
dotnet test Domain.slnx --configuration Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults/
```

### 3. Code Style Compliance

- Files must include copyright header
- Follow 3-space indentation
- Use file-scoped namespaces
- Apply expression-bodied member patterns
- Ensure var usage consistency
- Verify naming conventions (interfaces: I-prefix, private fields: _-prefix)

### 4. Documentation

- Add XML documentation for public APIs
- Update CLAUDE.md if architectural changes made
- Ensure README accuracy if public-facing changes

### 5. Quality Gates

- No build warnings in Release configuration
- All tests passing
- Code follows established patterns from existing codebase
- No violations of .editorconfig rules

### 6. Integration Testing

- Verify package references work correctly
- Test Entity Framework integrations if applicable
- Ensure audit functionality works if modified

## Project-Specific Considerations

### Domain Module Changes

- Verify generic constraints on Entity<T> types
- Test equality and hash code implementations
- Ensure value object immutability

### Audit Module Changes

- Test audit trail functionality
- Verify user audit tracking
- Check Entity Framework integration

### EntityFramework Module Changes

- Test database operations
- Verify value generators
- Check convention-based configurations

## Performance Considerations

- Run relevant benchmarks if performance-critical code changed
- Monitor memory allocations in hot paths
- Consider running full benchmark suite for baseline comparisons