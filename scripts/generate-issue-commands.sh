#!/bin/bash
#
# Generate GitHub issue creation commands from user story markdown files
#
# This script parses user story markdown files and generates `gh issue create` commands
# that can be executed to create GitHub issues.
#
# Usage:
#   ./generate-issue-commands.sh > create-all-issues.sh
#   # Review create-all-issues.sh
#   chmod +x create-all-issues.sh
#   ./create-all-issues.sh
#

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
USER_STORIES_DIR="$REPO_ROOT/docs/user-stories"

# Completed stories
COMPLETED_STORIES=("US-001" "US-002" "US-003" "US-004" "US-019")

echo "#!/bin/bash"
echo "#"
echo "# Auto-generated script to create GitHub issues from user stories"
echo "# Generated at: $(date)"
echo "#"
echo ""
echo "set -euo pipefail"
echo ""
echo "# Check if gh is authenticated"
echo "if ! gh auth status >/dev/null 2>&1; then"
echo "    echo 'ERROR: GitHub CLI not authenticated. Run: gh auth login'"
echo "    exit 1"
echo "fi"
echo ""
echo "REPO='RoySalisbury/HVO.Enterprise'"
echo ""

# Process each user story file
for story_file in "$USER_STORIES_DIR"/US-*.md; do
    if [[ ! -f "$story_file" ]]; then
        continue
    fi
    
    filename=$(basename "$story_file")
    story_id="${filename%.md}"
    story_id="${story_id%-*}"
    story_id="$(echo "$story_id" | cut -d'-' -f1-2)"  # e.g., US-001
    
    # Extract title from first line
    title=$(head -n1 "$story_file" | sed 's/^# US-[0-9]*: //')
    
    # Determine if complete
    is_complete=false
    for completed in "${COMPLETED_STORIES[@]}"; do
        if [[ "$story_id" == "$completed" ]]; then
            is_complete=true
            break
        fi
    done
    
    # Get metadata
    category=$(grep "^\*\*Category\*\*:" "$story_file" | sed 's/^\*\*Category\*\*: *//')
    effort=$(grep "^\*\*Effort\*\*:" "$story_file" | sed 's/^\*\*Effort\*\*: *//' | grep -oE '[0-9]+')
    sprint=$(grep "^\*\*Sprint\*\*:" "$story_file" | sed 's/^\*\*Sprint\*\*: *//' | grep -oE '[0-9]+')
    
    # Determine labels
    labels="user-story"
    
    if [[ "$is_complete" == "true" ]]; then
        labels="$labels,status:complete"
    else
        labels="$labels,status:not-started"
    fi
    
    if [[ -n "$effort" ]]; then
        labels="$labels,sp-$effort"
    fi
    
    if [[ -n "$sprint" ]]; then
        labels="$labels,sprint-$sprint"
    fi
    
    # Category label
    if [[ "$category" == *"Core Package"* ]]; then
        labels="$labels,core-package"
    elif [[ "$category" == *"Extension Package"* ]]; then
        labels="$labels,extension-package"
    elif [[ "$category" == *"Testing"* ]] || [[ "$category" == *"Samples"* ]]; then
        labels="$labels,testing"
    elif [[ "$category" == *"Documentation"* ]]; then
        labels="$labels,documentation"
    fi
    
    # Priority based on sprint
    if [[ -n "$sprint" ]]; then
        if (( sprint <= 2 )); then
            labels="$labels,priority:p0"
        elif (( sprint <= 4 )); then
            labels="$labels,priority:p1"
        elif (( sprint <= 8 )); then
            labels="$labels,priority:p2"
        else
            labels="$labels,priority:p3"
        fi
    fi
    
    echo "echo 'Creating issue for $story_id: $title'"
    echo "gh issue create \\"
    echo "    --repo \"\$REPO\" \\"
    echo "    --title \"[USER STORY]: $story_id - $title\" \\"
    echo "    --label \"$labels\" \\"
    echo "    --body-file \"$story_file\" \\"
    echo "    || echo 'Failed to create $story_id'"
    echo ""
done

echo "echo 'Done! All issues created.'"
