# User Stories for HVO.Enterprise Telemetry Library

This directory contains user stories for implementing the HVO.Enterprise telemetry library as outlined in the [project plan](../project-plan.md).

## 🎯 Creating GitHub Issues

These user stories can be converted to **GitHub Issues** using our structured issue templates:

- **[Create a User Story Issue](https://github.com/RoySalisbury/HVO.Enterprise/issues/new?template=user-story.yml)** - Use this template to create issues
- **[Helper Script](../../scripts/create-issues-helper.sh)** - Run this to see all stories with creation URLs
- **[Conversion Guide](../../.github/CREATING-ISSUES.md)** - Complete guide for creating issues from these stories

The issue templates provide:
- ✅ Azure DevOps-style structure with "As a... I want... So that..." format
- ✅ Trackable checkboxes for Acceptance Criteria and Definition of Done
- ✅ Story points and sprint tracking
- ✅ Proper labels (user-story, sp-X, sprint-X, category)
- ✅ Dependency linking between issues

See [.github/README.md](../../.github/README.md) for complete documentation on using issue templates.

## Story Organization

User stories are organized by functional area:

### Core Package Stories (US-001 to US-018)
Core functionality for the `HVO.Enterprise.Telemetry` package including correlation, metrics, tracing, and instrumentation.

- [US-001: Core Package Setup](./US-001-core-package-setup.md)
- [US-002: Auto-Managed Correlation](./US-002-auto-managed-correlation.md)
- [US-003: Background Job Correlation](./US-003-background-job-correlation.md)
- [US-004: Bounded Queue Worker](./US-004-bounded-queue-worker.md)
- [US-005: Lifecycle Management](./US-005-lifecycle-management.md)
- [US-006: Runtime-Adaptive Metrics](./US-006-runtime-adaptive-metrics.md)
- [US-007: Exception Tracking](./US-007-exception-tracking.md)
- [US-008: Configuration Hot Reload](./US-008-configuration-hot-reload.md)
- [US-009: Multi-Level Configuration](./US-009-multi-level-configuration.md)
- [US-010: ActivitySource Sampling](./US-010-activitysource-sampling.md)
- [US-011: Context Enrichment](./US-011-context-enrichment.md)
- [US-012: Operation Scope](./US-012-operation-scope.md)
- [US-013: ILogger Enrichment](./US-013-ilogger-enrichment.md)
- [US-014: DispatchProxy Instrumentation](./US-014-dispatchproxy-instrumentation.md)
- [US-015: Parameter Capture](./US-015-parameter-capture.md)
- [US-016: Statistics and Health Checks](./US-016-statistics-health-checks.md)
- [US-017: HTTP Instrumentation](./US-017-http-instrumentation.md)
- [US-018: DI and Static Initialization](./US-018-di-static-initialization.md)

### Extension Packages Stories (US-019 to US-025)
Platform-specific integration packages.

- [US-019: HVO.Common Library](./US-019-common-library.md)
- [US-020: IIS Extension](./US-020-iis-extension.md)
- [US-021: WCF Extension](./US-021-wcf-extension.md)
- [US-022: Database Extension](./US-022-database-extension.md)
- [US-023: Serilog Extension](./US-023-serilog-extension.md)
- [US-024: AppInsights Extension](./US-024-appinsights-extension.md)
- [US-025: Datadog Extension](./US-025-datadog-extension.md)

### Testing and Samples Stories (US-026 to US-028)
Validation and demonstration projects.

- [US-026: Unit Test Project](./US-026-unit-test-project.md)
- [US-027: .NET Framework 4.8 Sample](./US-027-net48-sample.md)
- [US-028: .NET 8 Sample](./US-028-net8-sample.md)

### Documentation Stories (US-029 to US-030)
Comprehensive documentation and future-proofing.

- [US-029: Project Documentation](./US-029-project-documentation.md)
- [US-030: Future Extensibility](./US-030-future-extensibility.md)

## Story Template

Each user story follows this structure:

1. **Story ID & Title**: Unique identifier and descriptive title
2. **Description**: User-facing value proposition ("As a... I want... So that...")
3. **Acceptance Criteria**: Specific, testable conditions for completion
4. **Technical Requirements**: Implementation details and constraints
5. **Testing Requirements**: Unit, integration, and validation tests
6. **Dependencies**: Other stories that must be completed first
7. **Estimated Effort**: Story points or time estimate
8. **Definition of Done**: Checklist for story completion

## Implementation Order

Stories should generally be implemented in numerical order, though some can be parallelized:

**Phase 1 - Foundation (US-001 to US-005)**
- Core infrastructure, correlation, background processing, lifecycle

**Phase 2 - Metrics & Tracing (US-006 to US-010)**
- Metrics collection, exception tracking, configuration, sampling

**Phase 3 - Enrichment & Instrumentation (US-011 to US-015)**
- Context enrichment, operation scopes, logging, automatic instrumentation

**Phase 4 - Statistics & HTTP (US-016 to US-018)**
- Health checks, statistics, HTTP client, DI setup

**Phase 5 - Extensions (US-019 to US-025)**
- Common library and platform-specific packages (can be parallelized)

**Phase 6 - Testing & Samples (US-026 to US-028)**
- Comprehensive tests and sample applications

**Phase 7 - Documentation (US-029 to US-030)**
- Final documentation and extensibility design

## Progress Tracking

Mark stories as:
- ❌ Not Started
- 🚧 In Progress
- ✅ Complete
- 🔍 In Review
- ⚠️ Blocked

Current Status: ❌ Not Started

## Notes

- All stories target .NET Standard 2.0 for maximum compatibility
- Performance targets: <100ns overhead per operation
- Zero warnings policy applies to all code
- All public APIs require XML documentation
- Follow coding standards in `.github/copilot-instructions.md`
