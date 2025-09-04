# Wangkanai Domain Library - Knowledge Base Index

**Intelligent navigation and cross-referenced documentation for the Wangkanai Domain library**

---

## Quick Start Navigation

### For New Developers
- [Quick Start Guide](#quick-start) → [Basic Concepts](#core-concepts) → [First Implementation](#getting-started-guide)
- [Glossary](glossary.md) → [FAQ](faq.md) → [Common Patterns](usage-patterns.md)

### For Experienced .NET Developers  
- [Architecture Overview](#architecture-overview) → [Design Decisions](design-decisions.md) → [Performance Guide](performance-optimization-guide.md)
- [Migration Guide](migration-guide.md) → [Advanced Patterns](#advanced-usage) → [Troubleshooting](troubleshooting-guide.md)

### For System Architects
- [Architectural Analysis](architectural_analysis_report.md) → [Design Decisions](design-decisions.md) → [Integration Patterns](integration-guide.md)
- [Performance Analysis](performance-analysis-report.md) → [Security Assessment](security_assessment_report.md) → [Quality Assessment](quality_assessment_report.md)

---

## Core Concepts

### Domain-Driven Design Foundation
- **[Entity Pattern](entity-guide.md)**: Strongly-typed entities with performance-optimized equality (`src/Domain/Entity.cs`)
- **[Value Objects](value-object-guide.md)**: Immutable objects with compiled accessors (`src/Domain/ValueObject.cs`) 
- **[Aggregate Roots](aggregate-root-guide.md)**: Domain boundaries and consistency (`src/Domain/Interfaces/IAggregateRoot.cs`)
- **[Domain Events](domain-events-guide.md)**: Event-driven architecture support (`src/Domain/Messages/`)

### Audit Trail System
- **[Audit Architecture](audit-guide.md)**: Comprehensive change tracking (`src/Audit/Audit.cs`)
- **[Auditable Entities](auditable-entities-guide.md)**: Automatic timestamp management (`src/Audit/AuditableEntity.cs`)
- **[Audit Store](audit-store-guide.md)**: Optimized audit data access (`src/Audit/Stores/`)

### Entity Framework Integration
- **[EF Core Integration](ef-integration-guide.md)**: Seamless ORM patterns (`src/EntityFramework/`)
- **[Database Context](audit-dbcontext-guide.md)**: Audit-enabled contexts (`src/EntityFramework/AuditDbContext.cs`)
- **[Value Generators](value-generators-guide.md)**: Automatic timestamp generation (`src/EntityFramework/Generators/`)

---

## Performance & Optimization

### Performance Features
- **[Entity Caching](performance-optimization-guide.md#entity-performance)**: 10% improvement with type caching (`Entity.cs:77-183`)
- **[Value Object Compilation](performance-optimization-guide.md#value-object-optimization)**: 500-1000x speed improvement (`ValueObject.cs:151-307`)
- **[Audit Optimization](performance-optimization-guide.md#audit-optimization)**: JSON serialization with span-based operations (`Audit.cs:85-170`)

### Benchmarking
- **[Performance Benchmarks](performance-analysis-report.md)**: Detailed measurement results
- **[Benchmark Implementation](../benchmark/Domain/ValueObjectPerformanceBenchmark.cs)**: Test code for performance validation

---

## Documentation Categories

### Reference Documentation
| Document | Purpose | Target Audience | Cross-References |
|----------|---------|-----------------|------------------|
| [API Reference](api-reference.md) | Complete API documentation | All developers | Links to guides and examples |
| [Glossary](glossary.md) | Terms and concepts | All users | Referenced throughout docs |
| [Architecture Diagrams](architecture-diagrams.md) | Visual system overview | Architects, senior devs | Links to implementation guides |

### Implementation Guides  
| Document | Purpose | Target Audience | Prerequisites |
|----------|---------|-----------------|---------------|
| [Getting Started](getting-started-guide.md) | First steps with the library | New developers | Basic .NET knowledge |
| [Usage Patterns](usage-patterns.md) | Common implementation patterns | All developers | Core concepts |
| [Integration Guide](integration-guide.md) | System integration scenarios | Senior developers | Architecture understanding |
| [Migration Guide](migration-guide.md) | Upgrade and migration paths | All developers | Existing system knowledge |

### Analysis & Assessment
| Document | Purpose | Target Audience | Key Insights |
|----------|---------|-----------------|--------------|
| [Design Decisions](design-decisions.md) | Architectural choice rationale | Architects, leads | Strategic context |
| [Quality Assessment](quality_assessment_report.md) | Code quality metrics | All developers | Quality standards |
| [Security Assessment](security_assessment_report.md) | Security analysis | Security teams | Risk evaluation |
| [Performance Analysis](performance-analysis-report.md) | Performance characteristics | Performance teams | Optimization opportunities |

### Troubleshooting & Support
| Document | Purpose | Target Audience | Problem Areas |
|----------|---------|-----------------|---------------|
| [Troubleshooting Guide](troubleshooting-guide.md) | Common issues and solutions | All developers | Error resolution |
| [FAQ](faq.md) | Frequently asked questions | All users | Quick answers |

---

## File Reference Map

### Source Code Organization
```
src/
├── Domain/                    # Core DDD patterns
│   ├── Entity.cs             # Performance-optimized base entity
│   ├── ValueObject.cs        # Compiled-accessor value objects  
│   ├── Interfaces/           # Core contracts (IAggregateRoot, IEntity, etc.)
│   ├── Messages/             # Domain event system
│   ├── Primitives/           # Result patterns and error handling
│   └── Extensions/           # Utility extensions
├── Audit/                    # Audit trail system
│   ├── Audit.cs             # High-performance audit entity
│   ├── AuditableEntity.cs   # Base auditable entities
│   ├── Stores/              # Audit data access layer
│   └── Extensions/          # Audit configuration helpers
└── EntityFramework/          # EF Core integration
    ├── AuditDbContext.cs    # Audit-enabled database context
    └── Generators/          # Automatic value generation
```

### Documentation Structure
```
docs/
├── knowledge-base-index.md       # This master index
├── Core Concepts/
│   ├── entity-guide.md          # Entity pattern implementation
│   ├── value-object-guide.md    # Value object best practices
│   ├── aggregate-root-guide.md  # Aggregate design patterns
│   └── domain-events-guide.md   # Event-driven architecture
├── Implementation/
│   ├── getting-started-guide.md # First steps tutorial
│   ├── usage-patterns.md        # Common implementation patterns
│   ├── integration-guide.md     # System integration scenarios
│   └── migration-guide.md       # Upgrade and migration paths
├── Reference/
│   ├── api-reference.md         # Complete API documentation  
│   ├── glossary.md             # Terms and definitions
│   └── faq.md                  # Frequently asked questions
└── Analysis/
    ├── design-decisions.md      # Architecture decision records
    ├── troubleshooting-guide.md # Problem resolution guide
    └── [existing analysis files] # Quality, security, performance reports
```

---

## Navigation Pathways

### Learning Path: New to Domain-Driven Design
1. **Foundation**: [Glossary](glossary.md) → [Core Concepts](#core-concepts)
2. **Practice**: [Getting Started](getting-started-guide.md) → [Usage Patterns](usage-patterns.md)
3. **Advanced**: [Integration Guide](integration-guide.md) → [Performance Guide](performance-optimization-guide.md)

### Learning Path: Existing DDD Experience  
1. **Architecture**: [Design Decisions](design-decisions.md) → [Architecture Diagrams](architecture-diagrams.md)
2. **Implementation**: [API Reference](api-reference.md) → [Advanced Patterns](usage-patterns.md#advanced-patterns)
3. **Optimization**: [Performance Analysis](performance-analysis-report.md) → [Benchmarking](../benchmark/)

### Learning Path: Migration from Other Libraries
1. **Assessment**: [Migration Guide](migration-guide.md) → [FAQ](faq.md)
2. **Implementation**: [Integration Guide](integration-guide.md) → [Troubleshooting](troubleshooting-guide.md)
3. **Validation**: [Quality Assessment](quality_assessment_report.md) → [Performance Benchmarks](performance-analysis-report.md)

---

## Quick Reference Cards

### Entity Pattern
```csharp
// High-performance entity with caching
public class User : Entity<Guid>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Usage
var user1 = new User { Id = Guid.NewGuid(), Name = "John" };
var user2 = new User { Id = user1.Id, Name = "Jane" };
Console.WriteLine(user1 == user2); // True - same ID
```
**Performance**: 10% improvement with type caching  
**References**: [Entity Guide](entity-guide.md), [API Reference](api-reference.md#entity)

### Value Object Pattern  
```csharp
// Optimized value object with compiled accessors
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```
**Performance**: 500-1000x improvement with compilation  
**References**: [Value Object Guide](value-object-guide.md), [Performance Guide](performance-optimization-guide.md#value-objects)

### Audit Pattern
```csharp
// Automatic audit tracking
public class Product : AuditableEntity<int>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// EF Core context with auditing
public class ShopContext : AuditDbContext
{
    public DbSet<Product> Products { get; set; }
}
```
**Features**: Automatic timestamps, change tracking, user attribution  
**References**: [Audit Guide](audit-guide.md), [EF Integration](ef-integration-guide.md)

---

## Cross-Reference Index

### Performance Topics
- **Entity Performance**: [Entity.cs:77-183] → [Performance Guide](performance-optimization-guide.md#entity-performance)
- **Value Object Performance**: [ValueObject.cs:151-307] → [Performance Guide](performance-optimization-guide.md#value-objects)  
- **Audit Performance**: [Audit.cs:85-170] → [Performance Guide](performance-optimization-guide.md#audit-optimization)
- **Benchmarking**: [benchmark/](../benchmark/) → [Performance Analysis](performance-analysis-report.md)

### Architecture Topics
- **DDD Patterns**: [Core Concepts](#core-concepts) → [Design Decisions](design-decisions.md)
- **EF Integration**: [EntityFramework/](../src/EntityFramework/) → [Integration Guide](integration-guide.md)
- **Event System**: [Messages/](../src/Domain/Messages/) → [Domain Events Guide](domain-events-guide.md)

### Troubleshooting Topics
- **Common Issues**: [FAQ](faq.md) → [Troubleshooting Guide](troubleshooting-guide.md)
- **Performance Issues**: [Performance Analysis](performance-analysis-report.md) → [Troubleshooting Guide](troubleshooting-guide.md#performance)
- **Integration Issues**: [Integration Guide](integration-guide.md) → [Troubleshooting Guide](troubleshooting-guide.md#integration)

---

## Documentation Status

| Document | Status | Last Updated | Next Review |
|----------|--------|--------------|-------------|
| Knowledge Base Index | ✅ Complete | 2025-01-15 | Q2 2025 |
| FAQ | 🔄 In Progress | - | 2025-01-16 |
| Design Decisions | 🔄 In Progress | - | 2025-01-16 |
| Troubleshooting Guide | 🔄 In Progress | - | 2025-01-16 |
| Migration Guide | 🔄 In Progress | - | 2025-01-16 |
| Glossary | 🔄 In Progress | - | 2025-01-16 |

**Legend**: ✅ Complete | 🔄 In Progress | ⏳ Planned | ❌ Needs Update

---

## Contributing to the Knowledge Base

### Adding New Documentation
1. Follow the [file organization structure](#documentation-structure)
2. Add cross-references to this index
3. Update the [navigation pathways](#navigation-pathways)
4. Include code examples with performance notes

### Updating Existing Documentation  
1. Check cross-references for consistency
2. Update the [status table](#documentation-status)
3. Verify all links and file paths
4. Test code examples for accuracy

### Content Standards
- **Code Examples**: Include performance characteristics and file references
- **Cross-References**: Link related concepts and implementation details
- **User Paths**: Consider different experience levels and use cases
- **Maintenance**: Keep status table updated with review dates