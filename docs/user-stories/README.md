# User Stories for HVO.Enterprise Telemetry Library

This directory contains user stories for implementing the HVO.Enterprise telemetry library as outlined in the [design decisions](../project-plan.md).

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

### Additional Stories (US-031 to US-032)
Further core and sample enhancements.

- [US-031: Sample Extension Integrations](./US-031-sample-extension-integrations.md) ✅
- [US-032: First Chance Exception Monitoring](./US-032-first-chance-exception-monitoring.md) ✅

### Additional Extension Packages (US-033 to US-037)
Additional telemetry export and transport integrations.

- [US-033: OpenTelemetry/OTLP Extension](./US-033-opentelemetry-otlp-extension.md) — Universal OTLP exporter for traces, metrics, and logs
- [US-034: Seq Extension](./US-034-seq-extension.md) — Seq structured log integration (CLEF + Serilog sink helper)
- [US-035: Grafana Extension](./US-035-grafana-extension.md) — Grafana Loki log push + Tempo/Mimir OTLP topology
- [US-036: gRPC Interceptor Extension](./US-036-grpc-interceptor-extension.md) — Server and client interceptors with `rpc.*` semantic conventions
- [US-037: NuGet Package Publishing](./US-037-nuget-package-publishing.md) — Package metadata, signing, and publishing pipeline

## Status

| Status | Count |
|--------|------:|
| ✅ Complete | 32 |
| ❌ Not Started | 5 |
| **Total** | **37** |

**Remaining**: US-027, US-029, US-030, US-034, US-035

## Notes

- All stories target .NET Standard 2.0 for maximum compatibility
- Performance targets: <100ns overhead per operation
- Zero warnings policy applies to all code
- All public APIs require XML documentation
- Follow coding standards in `.github/copilot-instructions.md`
