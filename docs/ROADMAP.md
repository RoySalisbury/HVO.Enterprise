# HVO.Enterprise Roadmap

> **Last updated:** February 2026
> **Total scope:** 37 user stories (US-001 to US-037)

## Current Progress

**32 of 37 stories complete (~86%)**

```
Overall ██████████████████████████░░░░ 86%
```

### Completed

| Story | Title | Status |
|-------|-------|--------|
| US-001 | Core Package Setup | ✅ Done |
| US-002 | Auto-Managed Correlation | ✅ Done |
| US-003 | Background Job Correlation | ✅ Done |
| US-004 | Bounded Queue Worker | ✅ Done |
| US-005 | Lifecycle Management | ✅ Done |
| US-006 | Runtime-Adaptive Metrics | ✅ Done |
| US-007 | Exception Tracking | ✅ Done |
| US-008 | Configuration Hot Reload | ✅ Done |
| US-009 | Multi-Level Configuration | ✅ Done |
| US-010 | ActivitySource Sampling | ✅ Done |
| US-011 | Context Enrichment | ✅ Done |
| US-012 | Operation Scope | ✅ Done |
| US-013 | ILogger Enrichment | ✅ Done |
| US-014 | DispatchProxy Instrumentation | ✅ Done |
| US-015 | Parameter Capture | ✅ Done |
| US-016 | Statistics and Health Checks | ✅ Done |
| US-017 | HTTP Instrumentation | ✅ Done |
| US-018 | DI and Static Initialization | ✅ Done |
| US-019 | HVO.Common Library | ✅ Done |
| US-020 | IIS Extension | ✅ Done |
| US-021 | WCF Extension | ✅ Done |
| US-022 | Database Extension | ✅ Done |
| US-023 | Serilog Extension | ✅ Done |
| US-024 | AppInsights Extension | ✅ Done |
| US-025 | Datadog Extension | ✅ Done |
| US-026 | Unit Test Project | ✅ Done |
| US-028 | .NET 8 Sample | ✅ Done |
| US-031 | Sample Extension Integrations | ✅ Done |
| US-032 | First Chance Exception Monitoring | ✅ Done |
| US-033 | OpenTelemetry/OTLP Extension | ✅ Done |
| US-036 | gRPC Interceptor Extension | ✅ Done |
| US-037 | NuGet Package Publishing | ✅ Done |

### Remaining

| Story | Title | Status |
|-------|-------|--------|
| US-027 | .NET Framework 4.8 Sample | ❌ Not started |
| US-029 | Project Documentation | ❌ Not started |
| US-030 | Future Extensibility | ❌ Not started |
| US-034 | Seq Extension | ❌ Not started |
| US-035 | Grafana Extension | ❌ Not started |

## Planned Features

The remaining stories cover:

- **US-027**: .NET Framework 4.8 sample application demonstrating legacy integration
- **US-029**: Comprehensive project documentation (API docs, tutorials)
- **US-030**: Future extensibility design (source generators, plugin model)
- **US-034**: Seq structured log integration (CLEF format + Serilog sink helper)
- **US-035**: Grafana Loki log push + Tempo/Mimir OTLP topology

## Package Status

| Package | NuGet ID | Status |
|---------|----------|--------|
| HVO.Common | `HVO.Common` | Pre-release |
| Core Telemetry | `HVO.Enterprise.Telemetry` | Pre-release |
| IIS Extension | `HVO.Enterprise.Telemetry.IIS` | Pre-release |
| WCF Extension | `HVO.Enterprise.Telemetry.Wcf` | Pre-release |
| Serilog Extension | `HVO.Enterprise.Telemetry.Serilog` | Pre-release |
| App Insights Extension | `HVO.Enterprise.Telemetry.AppInsights` | Pre-release |
| Datadog Extension | `HVO.Enterprise.Telemetry.Datadog` | Pre-release |
| Data (shared) | `HVO.Enterprise.Telemetry.Data` | Pre-release |
| EF Core Provider | `HVO.Enterprise.Telemetry.Data.EfCore` | Pre-release |
| ADO.NET Provider | `HVO.Enterprise.Telemetry.Data.AdoNet` | Pre-release |
| Redis Provider | `HVO.Enterprise.Telemetry.Data.Redis` | Pre-release |
| RabbitMQ Provider | `HVO.Enterprise.Telemetry.Data.RabbitMQ` | Pre-release |
| OpenTelemetry/OTLP | `HVO.Enterprise.Telemetry.OpenTelemetry` | Pre-release |
| gRPC Interceptor | `HVO.Enterprise.Telemetry.Grpc` | Pre-release |
| Seq Extension | `HVO.Enterprise.Telemetry.Seq` | Planned |
| Grafana Extension | `HVO.Enterprise.Telemetry.Grafana` | Planned |

## Version Compatibility Matrix

| Target Framework | HVO.Common | Core Telemetry | IIS | WCF | Serilog | AppInsights | Datadog | Data.* |
|-----------------|:----------:|:--------------:|:---:|:---:|:-------:|:-----------:|:-------:|:------:|
| .NET 10 | ✅ | ✅ | — | — | ✅ | ✅ | ✅ | ✅ |
| .NET 8 | ✅ | ✅ | — | — | ✅ | ✅ | ✅ | ✅ |
| .NET Standard 2.0 | ✅ | ✅ | — | — | ✅ | ✅ | ✅ | ✅ |
| .NET Framework 4.8.1 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

- **HVO.Common** and **Core Telemetry** target .NET Standard 2.0 for broad compatibility, with runtime-adaptive features on .NET 8+.
- **IIS** and **WCF** extensions target .NET Standard 2.0 (compatible with .NET Framework 4.8.1 and modern .NET versions that support .NET Standard 2.0).
- **Data.\*** packages target .NET Standard 2.0 with optional .NET 8+ optimizations.

## Breaking Change Policy

All packages follow [Semantic Versioning 2.0](https://semver.org/):

- **Pre-release (`0.x`)** — Breaking changes may occur between any minor version. Pin to exact versions.
- **Stable (`1.0+`)** — Breaking changes only in major versions. Minor and patch releases are backwards-compatible.
- **Public API surface** — Any type or member in a non-`internal` namespace is considered public API. Changes require a major version bump after 1.0.
- **Behavioral changes** — Significant behavioral changes (e.g., default sampling rate) are treated as breaking unless the previous behavior was clearly a bug.
- **Binary compatibility** — Maintained within a major version. Assembly binding redirects are not required for patch upgrades.

### Migration Support

When breaking changes are introduced in a major release:

1. A migration guide is published in `docs/migration/`.
2. The previous major version receives critical bug fixes for 6 months after the new major release.
3. Obsolete APIs are preserved for at least one major version with `[Obsolete]` warnings before removal.

## Deprecation Schedule

| Item | Deprecated In | Removed In | Replacement |
|------|:------------:|:----------:|-------------|
| *No deprecations yet* | — | — | — |

Deprecation notices will be added here as the API stabilizes. The general policy:

1. **Mark** — Add `[Obsolete("Use X instead. Will be removed in vN.")]` with a compiler warning.
2. **Warn** — Keep the deprecated API functional for at least one full major release cycle.
3. **Remove** — Delete the deprecated API in the next major version after the warning period.

## Further Reading

- [Architecture](./ARCHITECTURE.md) — System design and component diagrams
- [Platform Differences](./DIFFERENCES.md) — .NET Framework vs modern .NET comparison
- [Migration Guide](./MIGRATION.md) — Migrating from other telemetry libraries
- [Versioning](./VERSIONING.md) — Versioning strategy and release process
- [README](../README.md) — Getting started and quick overview
