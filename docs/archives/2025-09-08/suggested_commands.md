# Suggested Commands for Development

## Build Commands

```bash
# Clean, restore, and build entire solution
dotnet clean Domain.slnx -c Release
dotnet restore Domain.slnx
dotnet build Domain.slnx -c Release --no-restore

# Build specific projects
dotnet build src/ -c Release
dotnet build tests/ -c Release

# Use PowerShell build script (includes clean, restore, build for both src and tests)
.\build.ps1
```

## Testing Commands

```bash
# Run all tests
dotnet test Domain.slnx --configuration Release --no-build

# Run tests with coverage
dotnet test Domain.slnx --configuration Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults/ \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# Run specific test project
dotnet test tests/Domain/Wangkanai.Domain.Tests.csproj
dotnet test tests/Audit/Wangkanai.Audit.Tests.csproj
dotnet test tests/EntityFramework/Wangkanai.EntityFramework.Tests.csproj
```

## Benchmark Commands

```bash
# Run benchmarks (must be in Release configuration)
dotnet run --project benchmark/Domain --configuration Release
dotnet run --project benchmark/Audit --configuration Release
dotnet run --project benchmark/EntityFramework --configuration Release
```

## Package Management

```bash
# Restore packages using central management
dotnet restore

# Add package reference (will be managed centrally)
dotnet add package [PackageName]
```

## Development Workflow

```bash
# Check .NET version
dotnet --version
dotnet --list-sdks

# Solution file operations
dotnet sln Domain.slnx list
dotnet sln Domain.slnx add [project-path]
```

## Signing (if applicable)

```bash
# Run signing script
.\sign.ps1
```

## Git Workflow

```bash
# Standard git operations for Darwin/macOS
git status
git add .
git commit -m "message"
git push origin [branch-name]
```

## Quality Tools

The CI pipeline uses:

- SonarCloud scanner for code quality
- Codecov for coverage reporting
- Built-in .NET analyzers