#!/usr/bin/env python3
"""
Create GitHub Issues from User Story Markdown Files

This script automates the creation of GitHub issues from user story markdown files.
It parses the markdown files and creates issues using the GitHub API.

Requirements:
    pip install PyGithub

Usage:
    export GITHUB_TOKEN=your_github_token
    python create-github-issues.py
"""

import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

# Uncomment when ready to use:
# from github import Github, GithubException


# Completed stories that should be marked as complete
COMPLETED_STORIES = {
    "US-001",  # Core Package Setup
    "US-002",  # Auto-Managed Correlation
    "US-003",  # Background Job Correlation
    "US-004",  # Bounded Queue Worker
    "US-019",  # HVO.Common Library
}

# Story metadata
STORY_METADATA = {
    "US-001": {"sprint": 1, "sp": 3, "category": "Core Package"},
    "US-002": {"sprint": 1, "sp": 5, "category": "Core Package"},
    "US-003": {"sprint": 3, "sp": 5, "category": "Core Package"},
    "US-004": {"sprint": 2, "sp": 8, "category": "Core Package"},
    "US-005": {"sprint": 1, "sp": 5, "category": "Core Package"},
    "US-006": {"sprint": 2, "sp": 8, "category": "Core Package"},
    "US-007": {"sprint": 5, "sp": 3, "category": "Core Package"},
    "US-008": {"sprint": 5, "sp": 5, "category": "Core Package"},
    "US-009": {"sprint": 2, "sp": 5, "category": "Core Package"},
    "US-010": {"sprint": 3, "sp": 5, "category": "Core Package"},
    "US-011": {"sprint": 5, "sp": 5, "category": "Core Package"},
    "US-012": {"sprint": 3, "sp": 8, "category": "Core Package"},
    "US-013": {"sprint": 4, "sp": 5, "category": "Core Package"},
    "US-014": {"sprint": 6, "sp": 8, "category": "Core Package"},
    "US-015": {"sprint": 6, "sp": 5, "category": "Core Package"},
    "US-016": {"sprint": 4, "sp": 5, "category": "Core Package"},
    "US-017": {"sprint": 5, "sp": 3, "category": "Core Package"},
    "US-018": {"sprint": 4, "sp": 5, "category": "Core Package"},
    "US-019": {"sprint": 6, "sp": 5, "category": "Extension Package"},
    "US-020": {"sprint": 7, "sp": 3, "category": "Extension Package"},
    "US-021": {"sprint": 7, "sp": 5, "category": "Extension Package"},
    "US-022": {"sprint": 7, "sp": 8, "category": "Extension Package"},
    "US-023": {"sprint": 8, "sp": 3, "category": "Extension Package"},
    "US-024": {"sprint": 8, "sp": 5, "category": "Extension Package"},
    "US-025": {"sprint": 8, "sp": 5, "category": "Extension Package"},
    "US-026": {"sprint": 9, "sp": 30, "category": "Testing & Samples"},
    "US-027": {"sprint": 10, "sp": 13, "category": "Testing & Samples"},
    "US-028": {"sprint": 10, "sp": 13, "category": "Testing & Samples"},
    "US-029": {"sprint": 10, "sp": 8, "category": "Documentation"},
    "US-030": {"sprint": 10, "sp": 3, "category": "Documentation"},
}


def parse_markdown_file(filepath: Path) -> Dict[str, str]:
    """Parse a user story markdown file and extract sections."""
    content = filepath.read_text(encoding="utf-8")
    
    # Extract story ID from filename
    story_id = filepath.stem.split("-")[0:2]
    story_id = "-".join(story_id)  # e.g., "US-001"
    
    # Extract title from first line
    title_match = re.search(r"^# (US-\d+): (.+)$", content, re.MULTILINE)
    title = title_match.group(2) if title_match else "Unknown Title"
    
    sections = {}
    
    # Extract header metadata
    status_match = re.search(r"\*\*Status\*\*: (.+)", content)
    category_match = re.search(r"\*\*Category\*\*: (.+)", content)
    effort_match = re.search(r"\*\*Effort\*\*: (.+)", content)
    sprint_match = re.search(r"\*\*Sprint\*\*: (.+)", content)
    
    sections["story_id"] = story_id
    sections["title"] = title
    sections["status"] = status_match.group(1) if status_match else "❌ Not Started"
    sections["category"] = category_match.group(1) if category_match else "Unknown"
    sections["effort"] = effort_match.group(1) if effort_match else "Unknown"
    sections["sprint"] = sprint_match.group(1) if sprint_match else "Unknown"
    
    # Extract main sections
    section_patterns = [
        ("description", r"## Description\s*\n(.*?)(?=\n## |\Z)"),
        ("acceptance_criteria", r"## Acceptance Criteria\s*\n(.*?)(?=\n## |\Z)"),
        ("technical_requirements", r"## Technical Requirements\s*\n(.*?)(?=\n## |\Z)"),
        ("testing_requirements", r"## Testing Requirements\s*\n(.*?)(?=\n## |\Z)"),
        ("performance_requirements", r"## Performance Requirements\s*\n(.*?)(?=\n## |\Z)"),
        ("dependencies", r"## Dependencies\s*\n(.*?)(?=\n## |\Z)"),
        ("definition_of_done", r"## Definition of Done\s*\n(.*?)(?=\n## |\Z)"),
        ("notes", r"## Notes\s*\n(.*?)(?=\n## |\Z)"),
        ("related_docs", r"## Related Documentation\s*\n(.*?)(?=\n## |\Z)"),
    ]
    
    for section_name, pattern in section_patterns:
        match = re.search(pattern, content, re.DOTALL)
        if match:
            sections[section_name] = match.group(1).strip()
        else:
            sections[section_name] = ""
    
    return sections


def generate_issue_body(sections: Dict[str, str]) -> str:
    """Generate the issue body from parsed sections."""
    body_parts = []
    
    # Description
    if sections.get("description"):
        body_parts.append(f"## Description\n\n{sections['description']}")
    
    # Acceptance Criteria
    if sections.get("acceptance_criteria"):
        body_parts.append(f"## Acceptance Criteria\n\n{sections['acceptance_criteria']}")
    
    # Technical Requirements
    if sections.get("technical_requirements"):
        body_parts.append(f"## Technical Requirements\n\n{sections['technical_requirements']}")
    
    # Testing Requirements
    if sections.get("testing_requirements"):
        body_parts.append(f"## Testing Requirements\n\n{sections['testing_requirements']}")
    
    # Performance Requirements
    if sections.get("performance_requirements"):
        body_parts.append(f"## Performance Requirements\n\n{sections['performance_requirements']}")
    
    # Dependencies
    if sections.get("dependencies"):
        body_parts.append(f"## Dependencies\n\n{sections['dependencies']}")
    
    # Definition of Done
    if sections.get("definition_of_done"):
        body_parts.append(f"## Definition of Done\n\n{sections['definition_of_done']}")
    
    # Notes
    if sections.get("notes"):
        body_parts.append(f"## Notes\n\n{sections['notes']}")
    
    # Related Documentation
    if sections.get("related_docs"):
        body_parts.append(f"## Related Documentation\n\n{sections['related_docs']}")
    
    return "\n\n".join(body_parts)


def get_labels(story_id: str, metadata: Dict[str, any]) -> List[str]:
    """Generate labels for the issue."""
    labels = ["user-story"]
    
    # Add status label
    if story_id in COMPLETED_STORIES:
        labels.append("status:complete")
    else:
        labels.append("status:not-started")
    
    # Add story point label
    sp = metadata.get("sp")
    if sp:
        labels.append(f"sp-{sp}")
    
    # Add sprint label
    sprint = metadata.get("sprint")
    if sprint:
        labels.append(f"sprint-{sprint}")
    
    # Add category label
    category = metadata.get("category", "")
    if "Core Package" in category:
        labels.append("core-package")
    elif "Extension Package" in category:
        labels.append("extension-package")
    elif "Testing" in category or "Samples" in category:
        labels.append("testing")
    elif "Documentation" in category:
        labels.append("documentation")
    
    # Add priority label based on sprint
    if sprint and sprint <= 2:
        labels.append("priority:p0")
    elif sprint and sprint <= 4:
        labels.append("priority:p1")
    elif sprint and sprint <= 8:
        labels.append("priority:p2")
    else:
        labels.append("priority:p3")
    
    return labels


def create_issues_from_directory(user_stories_dir: Path, repo_name: str, dry_run: bool = True):
    """Create GitHub issues from all user story markdown files."""
    
    if not dry_run:
        # Uncomment when ready to use
        print("ERROR: GitHub API integration not yet enabled")
        print("To enable:")
        print("1. Uncomment the PyGithub import at the top")
        print("2. Uncomment the GitHub API code below")
        print("3. Set GITHUB_TOKEN environment variable")
        print("4. Run with --no-dry-run flag")
        return
        
        # token = os.environ.get("GITHUB_TOKEN")
        # if not token:
        #     print("ERROR: GITHUB_TOKEN environment variable not set")
        #     sys.exit(1)
        # 
        # g = Github(token)
        # repo = g.get_repo(repo_name)
    
    # Find all user story files
    story_files = sorted(user_stories_dir.glob("US-*.md"))
    
    print(f"Found {len(story_files)} user story files\n")
    
    for story_file in story_files:
        sections = parse_markdown_file(story_file)
        story_id = sections["story_id"]
        title = sections["title"]
        
        metadata = STORY_METADATA.get(story_id, {})
        labels = get_labels(story_id, metadata)
        
        issue_title = f"[USER STORY]: {story_id} - {title}"
        issue_body = generate_issue_body(sections)
        
        print(f"{'='*80}")
        print(f"Story: {story_id}")
        print(f"Title: {issue_title}")
        print(f"Labels: {', '.join(labels)}")
        print(f"Status: {'✅ COMPLETE' if story_id in COMPLETED_STORIES else '❌ NOT STARTED'}")
        print(f"Body length: {len(issue_body)} characters")
        
        if dry_run:
            print("DRY RUN - Would create issue")
        else:
            try:
                # Uncomment when ready to use:
                # issue = repo.create_issue(
                #     title=issue_title,
                #     body=issue_body,
                #     labels=labels
                # )
                # print(f"✅ Created issue #{issue.number}")
                pass
            except Exception as e:
                print(f"❌ Error creating issue: {e}")
        
        print()


def main():
    """Main entry point."""
    # Find the repository root
    script_dir = Path(__file__).parent
    repo_root = script_dir.parent
    user_stories_dir = repo_root / "docs" / "user-stories"
    
    if not user_stories_dir.exists():
        print(f"ERROR: User stories directory not found: {user_stories_dir}")
        sys.exit(1)
    
    repo_name = "RoySalisbury/HVO.Enterprise"
    dry_run = "--no-dry-run" not in sys.argv
    
    if dry_run:
        print("="*80)
        print("DRY RUN MODE - No issues will be created")
        print("Run with --no-dry-run to actually create issues")
        print("="*80)
        print()
    
    create_issues_from_directory(user_stories_dir, repo_name, dry_run=dry_run)
    
    print("\n" + "="*80)
    print("Summary:")
    print(f"- Total stories: {len(list(user_stories_dir.glob('US-*.md')))}")
    print(f"- Completed stories: {len(COMPLETED_STORIES)}")
    print(f"- Not started stories: {len(list(user_stories_dir.glob('US-*.md'))) - len(COMPLETED_STORIES)}")
    print("="*80)


if __name__ == "__main__":
    main()
