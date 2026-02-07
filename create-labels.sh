#!/bin/bash
#
# Create GitHub labels for user story tracking
#
# This script creates all the labels needed for user story issue tracking
# including status, priority, sprint, story points, and category labels.
#

set -euo pipefail

# Check if gh is authenticated
if ! gh auth status >/dev/null 2>&1; then
    echo 'ERROR: GitHub CLI not authenticated. Run: gh auth login'
    exit 1
fi

REPO='RoySalisbury/HVO.Enterprise'

echo "Creating labels in $REPO..."
echo ""

# Base label
gh label create "user-story" --color "0E8A16" --description "User story" --repo "$REPO" --force

# Status labels
gh label create "status:not-started" --color "D4C5F9" --description "Work has not started" --repo "$REPO" --force
gh label create "status:in-progress" --color "FEF2C0" --description "Work is in progress" --repo "$REPO" --force
gh label create "status:complete" --color "0E8A16" --description "Work is complete" --repo "$REPO" --force
gh label create "status:blocked" --color "D93F0B" --description "Work is blocked" --repo "$REPO" --force

# Priority labels
gh label create "priority:p0" --color "B60205" --description "Critical priority" --repo "$REPO" --force
gh label create "priority:p1" --color "D93F0B" --description "High priority" --repo "$REPO" --force
gh label create "priority:p2" --color "FBCA04" --description "Medium priority" --repo "$REPO" --force
gh label create "priority:p3" --color "0E8A16" --description "Low priority" --repo "$REPO" --force

# Category labels
gh label create "core-package" --color "1D76DB" --description "Core telemetry package" --repo "$REPO" --force
gh label create "extension-package" --color "5319E7" --description "Extension package" --repo "$REPO" --force
gh label create "testing" --color "C5DEF5" --description "Testing and samples" --repo "$REPO" --force
gh label create "documentation" --color "0075CA" --description "Documentation" --repo "$REPO" --force

# Story point labels
for sp in 1 2 3 5 8 13 20 30; do
    gh label create "sp-$sp" --color "EDEDED" --description "$sp story points" --repo "$REPO" --force
done

# Sprint labels (sprints 1-10)
for sprint in {1..10}; do
    gh label create "sprint-$sprint" --color "BFD4F2" --description "Sprint $sprint" --repo "$REPO" --force
done

echo ""
echo "Done! All labels created."
