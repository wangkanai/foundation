# Wangkanai Domain - Project Overview

## Purpose

Comprehensive collection of domain-driven design patterns, auditing capabilities, and Entity Framework utilities for .NET
applications. The project provides core building blocks for implementing clean
domain models with proper domain-driven design principles.

## Packages

The repository produces three NuGet packages:

1. **Wangkanai.Domain** - Core domain-driven design patterns and building blocks
   - Entity base classes with strongly-typed IDs
   - Value objects with equality comparison
   - Domain events and handlers
   - Result patterns for error handling
   - Audit trails and tracking

2. **Wangkanai.Audit** - Comprehensive auditing capabilities
   - Automatic change tracking
   - User audit trails
   - Configurable audit policies
   - Integration with Entity Framework
   - Audit data querying and reporting

3. **Wangkanai.EntityFramework** - Entity Framework Core utilities and extensions
   - Database builder extensions
   - Value generators for timestamps
   - Convention-based configurations
   - Migration utilities
   - Performance optimizations

## Architecture

Clean architecture approach with clear separation of concerns:

- **Domain**: Core business logic and entities
- **Audit**: Cross-cutting audit and tracking concerns
- **EntityFramework**: Data access layer utilities

## Key Design Patterns

- Domain-Driven Design (DDD)
- Generic Programming with strongly-typed entities
- SOLID principles
- Entity Framework Core integration
- Comprehensive audit trails