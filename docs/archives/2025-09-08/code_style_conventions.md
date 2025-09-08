# Code Style and Conventions

## General Conventions

- **Indentation**: 3 spaces (not tabs)
- **Line endings**: CRLF
- **Max line length**: 200 characters
- **Encoding**: UTF-8
- **Final newline**: false
- **Trim trailing whitespace**: true

## C# Language Conventions

- **Namespaces**: File-scoped (required, error level)
- **var usage**: Preferred everywhere (var keyword strongly encouraged)
- **Expression-bodied members**: Preferred for all member types
- **Pattern matching**: Preferred over traditional checks
- **this qualifier**: Avoided (false for all members)
- **Built-in types**: Preferred over BCL types (int vs Int32)

## Naming Conventions

- **Interfaces**: IPascalCase (prefix required)
- **Private fields**: _camelCase (underscore prefix required)
- **Private static fields**: _camelCase
- **Private constants**: PascalCase
- **Type parameters**: TPascalCase
- **General naming**: PascalCase for public members, camelCase for private

## Code Organization

- **Using directives**: Outside namespace, system first, separated groups
- **Braces**: Next line for all constructs
- **Properties**: Simple properties on single line preferred
- **Comments**: At first column disabled
- **XML Documentation**: Single line preferred, 140 char max

## ReSharper Specific

- **Body style**: Expression body preferred for constructors, methods, operators
- **Object creation**: Target typed when type not evident
- **Redundant parentheses**: Remove
- **Alignment**: Enabled for multiline expressions, assignments, method calls

## File Headers

All C#/C++/H files must include:

```
// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.
```

## Quality Rules

- **IDE0161**: Use file-scoped namespace (error level)
- **IDE0003**: Remove unnecessary 'this' qualification (error level)
- **IDE0055**: Formatting violations (silent)
- **CS1591**: Missing XML documentation (none - disabled)