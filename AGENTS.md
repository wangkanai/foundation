# Repository Guidelines

## Project Structure & Module Organization

The solution ships three primary modules: `Foundation`, `Audit`, and `EntityFramework`. Each module follows the same layout with
`src/` for library code, `tests/` for automated suites, and `benchmarks/` for performance harnesses. Shared assets live under
`Assets/`, while repository-wide specs and articles reside in `docs/`. Use the root `Foundation.slnx` to load the entire stack, or
open a module-specific solution when iterating on a single package.

## Build, Test, and Development Commands

- `dotnet restore Foundation.slnx` — install all transitive packages defined in `Directory.Packages.props`.
- `dotnet build Foundation.slnx -c Release` — compile every module with analyzer warnings enabled.
- `dotnet test Foundation.slnx` — run the xUnit suites; add `--collect:"XPlat Code Coverage"` when validating coverage.
- `dotnet format Foundation.slnx` — enforce `.editorconfig` rules; run before opening a PR.
- `./build.ps1` — Windows convenience script that restores, builds, tests, and packages.

## Coding Style & Naming Conventions

C# files use spaces with an unusual `indent_size = 3`; keep line length under 200 characters and adopt file-scoped namespaces.
Group `using` directives with system namespaces first, then alphabetical ordering. Classes, interfaces, and public members stay
PascalCase, private fields use `_camelCase`, and async methods end with `Async`. Prefer expression-bodied members where it aids
clarity.

## Testing Guidelines

xUnit drives the test suites located under `*/tests`. Mirror the namespace of the code under test and suffix files with
`Tests.cs`. New features require accompanying tests; extend coverage for regressions and document edge cases with theory data when
possible. Execute `dotnet test` locally before pushing, and ensure flaky tests are quarantined with clear TODO notes.

## Commit & Pull Request Guidelines

Commit messages follow a Conventional Commit style (`fix:`, `feat:`, `refactor:`). Keep commits focused and include context on why
the change was necessary. PRs should summarize the impact, link tracking issues, and attach screenshots or logs when behavior
changes. Request reviews from module owners and confirm CI is green before merging.
