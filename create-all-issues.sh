#!/bin/bash
#
# Auto-generated script to create GitHub issues from user stories
# Generated at: Sat Feb  7 10:33:04 PM UTC 2026
#

set -euo pipefail

# Check if gh is authenticated
if ! gh auth status >/dev/null 2>&1; then
    echo 'ERROR: GitHub CLI not authenticated. Run: gh auth login'
    exit 1
fi

REPO='RoySalisbury/HVO.Enterprise'

echo 'Creating issue for US-001: Core Package Setup and Dependencies'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-001 - Core Package Setup and Dependencies" \
    --label "user-story,status:complete,sp-3,sprint-1,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-001-core-package-setup.md" \
    || echo 'Failed to create US-001'

echo 'Creating issue for US-002: Auto-Managed Correlation with AsyncLocal'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-002 - Auto-Managed Correlation with AsyncLocal" \
    --label "user-story,status:complete,sp-5,sprint-1,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-002-auto-managed-correlation.md" \
    || echo 'Failed to create US-002'

echo 'Creating issue for US-003: Background Job Correlation Utilities'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-003 - Background Job Correlation Utilities" \
    --label "user-story,status:complete,sp-5,sprint-1,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-003-background-job-correlation.md" \
    || echo 'Failed to create US-003'

echo 'Creating issue for US-004: Bounded Queue with Channel-Based Worker'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-004 - Bounded Queue with Channel-Based Worker" \
    --label "user-story,status:complete,sp-8,sprint-2,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-004-bounded-queue-worker.md" \
    || echo 'Failed to create US-004'

echo 'Creating issue for US-005: Lifecycle Management'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-005 - Lifecycle Management" \
    --label "user-story,status:not-started,sp-5,sprint-1,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-005-lifecycle-management.md" \
    || echo 'Failed to create US-005'

echo 'Creating issue for US-006: Runtime-Adaptive Metrics'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-006 - Runtime-Adaptive Metrics" \
    --label "user-story,status:not-started,sp-8,sprint-2,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-006-runtime-adaptive-metrics.md" \
    || echo 'Failed to create US-006'

echo 'Creating issue for US-007: Exception Tracking'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-007 - Exception Tracking" \
    --label "user-story,status:not-started,sp-3,sprint-5,core-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-007-exception-tracking.md" \
    || echo 'Failed to create US-007'

echo 'Creating issue for US-008: Configuration Hot Reload'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-008 - Configuration Hot Reload" \
    --label "user-story,status:not-started,sp-5,sprint-5,core-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-008-configuration-hot-reload.md" \
    || echo 'Failed to create US-008'

echo 'Creating issue for US-009: Multi-Level Configuration'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-009 - Multi-Level Configuration" \
    --label "user-story,status:not-started,sp-5,sprint-2,core-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-009-multi-level-configuration.md" \
    || echo 'Failed to create US-009'

echo 'Creating issue for US-010: ActivitySource Sampling'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-010 - ActivitySource Sampling" \
    --label "user-story,status:not-started,sp-5,sprint-3,core-package,priority:p1" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-010-activitysource-sampling.md" \
    || echo 'Failed to create US-010'

echo 'Creating issue for US-011: Context Enrichment'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-011 - Context Enrichment" \
    --label "user-story,status:not-started,sp-5,sprint-3,core-package,priority:p1" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-011-context-enrichment.md" \
    || echo 'Failed to create US-011'

echo 'Creating issue for US-012: Operation Scope'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-012 - Operation Scope" \
    --label "user-story,status:not-started,sp-8,sprint-4,core-package,priority:p1" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-012-operation-scope.md" \
    || echo 'Failed to create US-012'

echo 'Creating issue for US-013: ILogger Enrichment'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-013 - ILogger Enrichment" \
    --label "user-story,status:not-started,sp-5,sprint-4,core-package,priority:p1" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-013-ilogger-enrichment.md" \
    || echo 'Failed to create US-013'

echo 'Creating issue for US-014: DispatchProxy Instrumentation'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-014 - DispatchProxy Instrumentation" \
    --label "user-story,status:not-started,sp-8,sprint-6,core-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-014-dispatchproxy-instrumentation.md" \
    || echo 'Failed to create US-014'

echo 'Creating issue for US-015: Parameter Capture'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-015 - Parameter Capture" \
    --label "user-story,status:not-started,sp-5,sprint-5,core-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-015-parameter-capture.md" \
    || echo 'Failed to create US-015'

echo 'Creating issue for US-016: Statistics and Health Checks'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-016 - Statistics and Health Checks" \
    --label "user-story,status:not-started,sp-5,sprint-4,core-package,priority:p1" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-016-statistics-health-checks.md" \
    || echo 'Failed to create US-016'

echo 'Creating issue for US-017: HTTP Instrumentation'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-017 - HTTP Instrumentation" \
    --label "user-story,status:not-started,sp-3,sprint-5,core-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-017-http-instrumentation.md" \
    || echo 'Failed to create US-017'

echo 'Creating issue for US-018: DI and Static Initialization'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-018 - DI and Static Initialization" \
    --label "user-story,status:not-started,sp-5,sprint-4,core-package,priority:p1" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-018-di-static-initialization.md" \
    || echo 'Failed to create US-018'

echo 'Creating issue for US-019: HVO.Common Library Structure'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-019 - HVO.Common Library Structure" \
    --label "user-story,status:complete,sp-5,sprint-1,extension-package,priority:p0" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-019-common-library.md" \
    || echo 'Failed to create US-019'

echo 'Creating issue for US-020: IIS Extension Package'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-020 - IIS Extension Package" \
    --label "user-story,status:not-started,sp-3,sprint-7,extension-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-020-iis-extension.md" \
    || echo 'Failed to create US-020'

echo 'Creating issue for US-021: WCF Extension Package'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-021 - WCF Extension Package" \
    --label "user-story,status:not-started,sp-5,sprint-7,extension-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-021-wcf-extension.md" \
    || echo 'Failed to create US-021'

echo 'Creating issue for US-022: Database Extension Package'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-022 - Database Extension Package" \
    --label "user-story,status:not-started,sp-8,sprint-7,extension-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-022-database-extension.md" \
    || echo 'Failed to create US-022'

echo 'Creating issue for US-023: Serilog Extension Package'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-023 - Serilog Extension Package" \
    --label "user-story,status:not-started,sp-3,sprint-8,extension-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-023-serilog-extension.md" \
    || echo 'Failed to create US-023'

echo 'Creating issue for US-024: Application Insights Extension Package'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-024 - Application Insights Extension Package" \
    --label "user-story,status:not-started,sp-5,sprint-8,extension-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-024-appinsights-extension.md" \
    || echo 'Failed to create US-024'

echo 'Creating issue for US-025: Datadog Extension Package'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-025 - Datadog Extension Package" \
    --label "user-story,status:not-started,sp-5,sprint-8,extension-package,priority:p2" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-025-datadog-extension.md" \
    || echo 'Failed to create US-025'

echo 'Creating issue for US-026: Unit Test Project'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-026 - Unit Test Project" \
    --label "user-story,status:not-started,sp-30,sprint-9,testing,priority:p3" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-026-unit-test-project.md" \
    || echo 'Failed to create US-026'

echo 'Creating issue for US-027: .NET Framework 4.8 Sample Application'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-027 - .NET Framework 4.8 Sample Application" \
    --label "user-story,status:not-started,sp-13,sprint-10,testing,priority:p3" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-027-net48-sample.md" \
    || echo 'Failed to create US-027'

echo 'Creating issue for US-028: .NET 8 Sample Application'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-028 - .NET 8 Sample Application" \
    --label "user-story,status:not-started,sp-13,sprint-10,testing,priority:p3" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-028-net8-sample.md" \
    || echo 'Failed to create US-028'

echo 'Creating issue for US-029: Project Documentation'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-029 - Project Documentation" \
    --label "user-story,status:not-started,sp-8,sprint-10,documentation,priority:p3" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-029-project-documentation.md" \
    || echo 'Failed to create US-029'

echo 'Creating issue for US-030: Future Extensibility'
gh issue create \
    --repo "$REPO" \
    --title "[USER STORY]: US-030 - Future Extensibility" \
    --label "user-story,status:not-started,sp-3,sprint-10,documentation,priority:p3" \
    --body-file "/workspaces/HVO.Enterprise/docs/user-stories/US-030-future-extensibility.md" \
    || echo 'Failed to create US-030'

echo 'Done! All issues created.'
