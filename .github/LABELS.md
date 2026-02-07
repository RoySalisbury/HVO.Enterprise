# GitHub Labels for HVO.Enterprise

This repository uses the following label scheme for issue management:

## Issue Type Labels

- **user-story** (🟢 green `#28a745`) - User stories following the structured template
- **bug** (🔴 red `#d73a4a`) - Something isn't working
- **enhancement** (🔵 blue `#0075ca`) - New feature or request
- **documentation** (📘 blue `#0075ca`) - Improvements or additions to documentation

## Status Labels

- **needs-triage** (⚪ white `#ffffff`) - Needs initial review and categorization
- **ready** (🟢 green `#28a745`) - Ready for development
- **in-progress** (🟡 yellow `#fbca04`) - Currently being worked on
- **in-review** (🟠 orange `#ff9800`) - Code review in progress
- **blocked** (🔴 red `#d73a4a`) - Blocked by dependencies

## Priority Labels

- **priority-critical** (🔴 red `#b60205`) - Critical priority
- **priority-high** (🟠 orange `#d93f0b`) - High priority
- **priority-medium** (🟡 yellow `#fbca04`) - Medium priority
- **priority-low** (⚪ gray `#cccccc`) - Low priority

## Category Labels

- **core-package** (🔵 blue `#0052cc`) - Core telemetry package
- **extension-package** (🔵 blue `#0052cc`) - Extension packages
- **testing** (🟣 purple `#5319e7`) - Testing and validation
- **samples** (🟣 purple `#5319e7`) - Sample applications
- **infrastructure** (⚫ gray `#666666`) - Build, CI/CD, tooling

## Story Points Labels

- **sp-1** - 1 story point
- **sp-2** - 2 story points
- **sp-3** - 3 story points
- **sp-5** - 5 story points
- **sp-8** - 8 story points
- **sp-13** - 13 story points
- **sp-21** - 21 story points

## Sprint Labels

- **sprint-1** - Sprint 1
- **sprint-2** - Sprint 2
- **sprint-3** - Sprint 3
- (continue as needed)

## Setting Up Labels

You can create these labels using the GitHub CLI:

```bash
# Issue Type Labels
gh label create "user-story" --description "User story following structured template" --color "28a745"
gh label create "bug" --description "Something isn't working" --color "d73a4a"
gh label create "enhancement" --description "New feature or request" --color "0075ca"
gh label create "documentation" --description "Documentation improvements" --color "0075ca"

# Status Labels
gh label create "needs-triage" --description "Needs initial review" --color "ffffff"
gh label create "ready" --description "Ready for development" --color "28a745"
gh label create "in-progress" --description "Currently being worked on" --color "fbca04"
gh label create "in-review" --description "Code review in progress" --color "ff9800"
gh label create "blocked" --description "Blocked by dependencies" --color "d73a4a"

# Priority Labels
gh label create "priority-critical" --description "Critical priority" --color "b60205"
gh label create "priority-high" --description "High priority" --color "d93f0b"
gh label create "priority-medium" --description "Medium priority" --color "fbca04"
gh label create "priority-low" --description "Low priority" --color "cccccc"

# Category Labels
gh label create "core-package" --description "Core telemetry package" --color "0052cc"
gh label create "extension-package" --description "Extension packages" --color "0052cc"
gh label create "testing" --description "Testing and validation" --color "5319e7"
gh label create "samples" --description "Sample applications" --color "5319e7"
gh label create "infrastructure" --description "Build, CI/CD, tooling" --color "666666"

# Story Points Labels
gh label create "sp-1" --description "1 story point" --color "c2e0c6"
gh label create "sp-2" --description "2 story points" --color "bfe5bf"
gh label create "sp-3" --description "3 story points" --color "a3d9a5"
gh label create "sp-5" --description "5 story points" --color "7bc96f"
gh label create "sp-8" --description "8 story points" --color "51af32"
gh label create "sp-13" --description "13 story points" --color "2e7d32"
gh label create "sp-21" --description "21 story points" --color "1b5e20"

# Sprint Labels (create as needed)
gh label create "sprint-1" --description "Sprint 1" --color "5319e7"
gh label create "sprint-2" --description "Sprint 2" --color "5319e7"
```

## Label Usage Guidelines

### For User Stories
Apply the following labels to each user story:
1. **user-story** (always)
2. **Category** (core-package, extension-package, testing, samples)
3. **Story Points** (sp-1, sp-2, sp-3, sp-5, sp-8, sp-13, sp-21)
4. **Sprint** (sprint-1, sprint-2, etc.)
5. **Priority** (priority-critical, priority-high, priority-medium, priority-low)
6. **Status** (needs-triage, ready, in-progress, in-review, blocked)

### For Bugs
Apply the following labels:
1. **bug** (always)
2. **Priority** (based on severity)
3. **Status** (current status)
4. **Category** (which component is affected)

### For Features/Enhancements
Apply the following labels:
1. **enhancement** (always)
2. **Priority** (based on importance)
3. **Status** (current status)
4. **Category** (which component is affected)

## Viewing Filtered Issues

Use these filters in GitHub Issues:

- All user stories: `label:user-story`
- Sprint 1 stories: `label:user-story label:sprint-1`
- High priority bugs: `label:bug label:priority-high`
- Core package work: `label:core-package`
- Ready for work: `label:ready`
- Blocked items: `label:blocked`

## Automation

Consider using GitHub Actions to:
- Auto-add labels based on issue template used
- Move issues to project boards based on status labels
- Send notifications for critical/high priority issues
