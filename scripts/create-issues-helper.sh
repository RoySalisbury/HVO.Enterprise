#!/bin/bash
#
# Script to help create GitHub issues from user story markdown files
# This script prints information that can be used to create issues manually
# or can be adapted to use GitHub CLI for automated creation
#

STORIES_DIR="docs/user-stories"

# Color codes for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "  User Story to GitHub Issue Helper"
echo "=========================================="
echo ""

# Check if GitHub CLI is installed
if command -v gh &> /dev/null; then
    echo "✓ GitHub CLI (gh) is installed"
    echo ""
else
    echo "⚠ GitHub CLI (gh) is not installed"
    echo "  Install from: https://cli.github.com/"
    echo "  This script will provide manual creation instructions"
    echo ""
fi

# Function to extract metadata from markdown file
extract_metadata() {
    local file=$1
    local field=$2
    
    case $field in
        "id")
            grep -oP "^# (US-\d+):" "$file" | grep -oP "US-\d+" || echo "US-XXX"
            ;;
        "title")
            grep -oP "^# US-\d+: \K.+" "$file" | head -1 || echo ""
            ;;
        "category")
            grep -oP "\*\*Category\*\*: \K.+" "$file" | head -1 || echo "Core Package"
            ;;
        "effort")
            grep -oP "\*\*Effort\*\*: \K\d+" "$file" | head -1 || echo "3"
            ;;
        "sprint")
            grep -oP "\*\*Sprint\*\*: \K.+" "$file" | head -1 || echo "1"
            ;;
    esac
}

# Function to print story information
print_story_info() {
    local file=$1
    local filename=$(basename "$file")
    
    local story_id=$(extract_metadata "$file" "id")
    local title=$(extract_metadata "$file" "title")
    local category=$(extract_metadata "$file" "category")
    local effort=$(extract_metadata "$file" "effort")
    local sprint=$(extract_metadata "$file" "sprint")
    
    echo -e "${GREEN}Story: $story_id - $title${NC}"
    echo "  File: $filename"
    echo "  Category: $category"
    echo "  Effort: $effort SP"
    echo "  Sprint: $sprint"
    echo "  Labels: user-story, sp-$effort, sprint-$sprint"
    echo ""
    echo "  Manual Creation URL:"
    echo "  https://github.com/RoySalisbury/HVO.Enterprise/issues/new?template=user-story.yml&title=[USER%20STORY]%20$story_id:%20$title"
    echo ""
}

# Main script
echo "Found user stories:"
echo ""

# Counter for stories
count=0

# Process each user story file
for story_file in "$STORIES_DIR"/US-*.md; do
    if [ -f "$story_file" ]; then
        count=$((count + 1))
        print_story_info "$story_file"
        echo "---"
    fi
done

echo ""
echo -e "${BLUE}Total stories found: $count${NC}"
echo ""
echo "Next steps:"
echo "1. For each story, click the 'Manual Creation URL' above"
echo "2. Fill in the template with content from the markdown file"
echo "3. Apply appropriate labels (shown above)"
echo "4. Create the issue"
echo ""
echo "For bulk creation, consider using the Python script:"
echo "  scripts/create-issues-from-stories.py"
echo ""
echo "For more information, see:"
echo "  .github/CREATING-ISSUES.md"
echo ""
