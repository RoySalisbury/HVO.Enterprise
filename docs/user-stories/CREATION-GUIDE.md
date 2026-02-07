# User Story Creation Guide

This document provides templates and guidelines for creating the remaining user stories for the HVO.Enterprise project.

## Completed Stories

The following stories have been fully documented:

- ✅ US-001: Core Package Setup and Dependencies
- ✅ US-002: Auto-Managed Correlation with AsyncLocal
- ✅ US-003: Background Job Correlation Utilities
- ✅ US-004: Bounded Queue with Channel-Based Worker

## Remaining Stories to Create

### Core Package (US-005 to US-018)

- [ ] **US-005: Lifecycle Management** - AppDomain.DomainUnload hooks, IIS detection, graceful shutdown
- [ ] **US-006: Runtime-Adaptive Metrics** - Meter API (.NET 6+) vs EventCounters (.NET Framework 4.8)
- [ ] **US-007: Exception Tracking** - Fingerprinting, aggregation, error rates
- [ ] **US-008: Configuration Hot Reload** - File watcher, IOptionsMonitor, HTTP endpoint
- [ ] **US-009: Multi-Level Configuration** - Global/Type/Method/Call precedence, diagnostic API
- [ ] **US-010: ActivitySource Sampling** - Probabilistic sampling, per-source configuration
- [ ] **US-011: Context Enrichment** - User context, request context, PII handling
- [ ] **US-012: Operation Scope** - IOperationScope, accurate timing, property capture
- [ ] **US-013: ILogger Enrichment** - Automatic Activity/Correlation injection
- [ ] **US-014: DispatchProxy Instrumentation** - Attribute-based automatic instrumentation
- [ ] **US-015: Parameter Capture** - Tiered capture, sensitive data detection
- [ ] **US-016: Statistics and Health Checks** - ITelemetryStatistics, TelemetryHealthCheck
- [ ] **US-017: HTTP Instrumentation** - TelemetryHttpMessageHandler, W3C TraceContext
- [ ] **US-018: DI and Static Initialization** - AddTelemetry(), Telemetry.Initialize()

### Extension Packages (US-019 to US-025)

- [ ] **US-019: HVO.Common Library** - Result<T>, Option<T>, IOneOf, extension methods
- [ ] **US-020: IIS Extension** - IRegisteredObject, HostingEnvironment integration
- [ ] **US-021: WCF Extension** - Message inspectors, W3C TraceContext in SOAP headers
- [ ] **US-022: Database Extension** - EF Core, EF6, Dapper, ADO.NET, Redis, MongoDB
- [ ] **US-023: Serilog Extension** - Activity enricher, correlation enricher
- [ ] **US-024: AppInsights Extension** - Dual-mode bridge, telemetry initializers
- [ ] **US-025: Datadog Extension** - Dual-mode export, OTLP + DogStatsD

### Testing & Samples (US-026 to US-028)

- [ ] **US-026: Unit Test Project** - Comprehensive tests, >85% coverage, mocking strategies
- [ ] **US-027: .NET Framework 4.8 Sample** - ASP.NET MVC, WebAPI, WCF, Hangfire
- [ ] **US-028: .NET 8 Sample** - ASP.NET Core, gRPC, IHostedService, health checks

### Documentation (US-029 to US-030)

- [ ] **US-029: Project Documentation** - README, DIFFERENCES, MIGRATION, ARCHITECTURE, ROADMAP
- [ ] **US-030: Future Extensibility** - IMethodInstrumentationStrategy, source generator prep

## User Story Template

Each user story should include the following sections:

### 1. Header

```markdown
# US-XXX: [Title]

**Status**: ❌ Not Started  
**Category**: [Core Package | Extension Package | Testing | Documentation]  
**Effort**: [1-13 story points]  
**Sprint**: [Sprint number]
```

### 2. Description (User Story Format)

```markdown
## Description

As a **[user role]**,  
I want **[feature/capability]**,  
So that **[business value/benefit]**.
```

### 3. Acceptance Criteria

```markdown
## Acceptance Criteria

1. **[Category 1]**
   - [ ] Specific testable criterion
   - [ ] Another criterion
   
2. **[Category 2]**
   - [ ] Specific testable criterion
```

### 4. Technical Requirements

```markdown
## Technical Requirements

### [Subsection]

Detailed technical specifications, code samples, interface definitions, etc.

\`\`\`csharp
// Code examples
\`\`\`
```

### 5. Testing Requirements

```markdown
## Testing Requirements

### Unit Tests

1. **[Test Category]**
   \`\`\`csharp
   [Fact]
   public void TestName()
   {
       // Arrange
       // Act
       // Assert
   }
   \`\`\`

### Integration Tests

### Performance Tests (if applicable)
```

### 6. Performance Requirements

```markdown
## Performance Requirements

- **Operation A**: <Xns
- **Operation B**: <Xμs
- **Throughput**: >X ops/sec
```

### 7. Dependencies

```markdown
## Dependencies

**Blocked By**: US-XXX, US-YYY  
**Blocks**: US-ZZZ
```

### 8. Definition of Done

```markdown
## Definition of Done

- [ ] Core functionality implemented
- [ ] All unit tests passing (>XX% coverage)
- [ ] Integration tests passing
- [ ] Performance benchmarks met
- [ ] XML documentation complete
- [ ] Code reviewed and approved
- [ ] Zero warnings in build
```

### 9. Notes

```markdown
## Notes

### Design Decisions

1. **Why [decision]?**
   - Rationale
   
### Implementation Tips

- Tip 1
- Tip 2

### Common Pitfalls

- Pitfall 1
- Pitfall 2
```

### 10. Related Documentation

```markdown
## Related Documentation

- [Project Plan](../project-plan.md#X-...)
- [External Documentation](https://...)
```

## Story Sizing Guidelines

### 1 Story Point (1-2 hours)
- Simple utility functions
- Minor configuration changes
- Straightforward implementations

### 2 Story Points (2-4 hours)
- Basic features with tests
- Simple integrations
- Documentation updates

### 3 Story Points (4-8 hours)
- Core features with moderate complexity
- Multiple test scenarios
- Some integration work

### 5 Story Points (1-2 days)
- Complex features
- Multiple integration points
- Extensive testing required
- Significant design work

### 8 Story Points (2-4 days)
- Major features
- Complex algorithms
- Multiple dependencies
- Performance optimization required

### 13 Story Points (4-5 days)
- Epic-level work
- Multiple sub-features
- Extensive integration
- Complex testing scenarios

## Key References for Each Story Type

### Core Package Stories
- Reference: [Project Plan Steps 1-18](../project-plan.md)
- Focus: Performance (<100ns overhead), .NET Standard 2.0 compatibility
- Key constraints: Non-blocking, single binary, runtime adaptation

### Extension Package Stories
- Reference: [Project Plan Steps 19-25](../project-plan.md)
- Focus: Platform integration, minimal dependencies
- Key patterns: Dual-mode (EventCounters/.NET Framework vs Meter/.NET 6+)

### Testing Stories
- Reference: [Project Plan Steps 26-28](../project-plan.md)
- Focus: >85% coverage, real-world scenarios
- Key platforms: Both .NET Framework 4.8 and .NET 8

### Documentation Stories
- Reference: [Project Plan Steps 29-30](../project-plan.md)
- Focus: Clear migration path, platform differences
- Key audiences: Developers moving from .NET Framework to .NET 8

## Priority Matrix

### Must Have (P0) - Phase 1
- US-001 through US-006 (Foundation)
- US-019 (Common library)

### Should Have (P1) - Phase 2
- US-007 through US-018 (Core features)
- US-020 through US-022 (Key extensions)

### Nice to Have (P2) - Phase 3
- US-023 through US-025 (Additional extensions)
- US-026 (Testing)

### Future (P3) - Phase 4
- US-027, US-028 (Samples)
- US-029, US-030 (Documentation)

## Implementation Order

Follow this sequence for optimal dependency management:

1. **Sprint 1** (Foundation)
   - US-001, US-002, US-004, US-005

2. **Sprint 2** (Metrics & Configuration)
   - US-006, US-008, US-009, US-010

3. **Sprint 3** (Instrumentation)
   - US-003, US-007, US-012, US-013

4. **Sprint 4** (Advanced Features)
   - US-011, US-014, US-015, US-016

5. **Sprint 5** (HTTP & DI)
   - US-017, US-018

6. **Sprint 6** (Extensions - Part 1)
   - US-019, US-020, US-021

7. **Sprint 7** (Extensions - Part 2)
   - US-022, US-023, US-024, US-025

8. **Sprint 8** (Testing & Samples)
   - US-026, US-027, US-028

9. **Sprint 9** (Documentation & Polish)
   - US-029, US-030

## Estimation Guidelines

Total estimated effort: **~150-200 story points** (~6-8 weeks for 2-person team)

- Core Package (US-001 to US-018): ~80 points
- Extensions (US-019 to US-025): ~50 points
- Testing (US-026): ~30 points
- Samples (US-027 to US-028): ~25 points
- Documentation (US-029 to US-030): ~15 points

## Notes for Story Authors

When creating user stories:

1. **Be Specific**: Acceptance criteria should be testable and unambiguous
2. **Include Examples**: Code samples help clarify requirements
3. **Consider Edge Cases**: What happens when things go wrong?
4. **Think Performance**: Every operation has overhead - what's acceptable?
5. **Plan for Testing**: How will you verify this works?
6. **Document Dependencies**: What must be done first? What does this enable?
7. **Cross-Platform**: Consider .NET Framework 4.8 AND .NET 8+ scenarios
8. **Security**: PII, sensitive data, authentication considerations

## Quality Checklist

Before marking a story complete:

- [ ] Story follows template structure
- [ ] Acceptance criteria are testable
- [ ] Technical requirements include code samples
- [ ] Testing requirements include specific test cases
- [ ] Performance requirements specified (if applicable)
- [ ] Dependencies identified
- [ ] Definition of done is comprehensive
- [ ] Design decisions explained
- [ ] Links to project plan included
- [ ] Cross-platform considerations addressed
- [ ] Security implications considered
