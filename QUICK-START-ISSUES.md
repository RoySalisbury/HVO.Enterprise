# Quick Start: Creating GitHub Issues from User Stories

## Summary

✅ **All 30 user story markdown files are ready**  
⏳ **GitHub issues need to be created by you (requires authentication)**

## 5-Minute Setup

### Step 1: Authenticate with GitHub

```bash
gh auth login
```

Follow the prompts to authenticate with GitHub.

### Step 2: Generate Issue Creation Script

```bash
cd /home/runner/work/HVO.Enterprise/HVO.Enterprise
./scripts/generate-issue-commands.sh > create-all-issues.sh
```

This creates a script that will create all 30 GitHub issues.

### Step 3: Review the Script (Optional but Recommended)

```bash
cat create-all-issues.sh | less
```

Look for:
- Correct issue titles
- Proper labels (status:complete for US-001, US-002, US-003, US-004, US-019)
- Story points (sp-X)
- Sprint assignments (sprint-X)

### Step 4: Execute the Script

```bash
chmod +x create-all-issues.sh
./create-all-issues.sh
```

This will create all 30 issues. Takes about 2-3 minutes.

### Step 5: Verify

```bash
# Check total count (should be 30)
gh issue list --repo RoySalisbury/HVO.Enterprise --label user-story --limit 100 | wc -l

# Check completed stories (should be 5)
gh issue list --repo RoySalisbury/HVO.Enterprise --label status:complete

# Check not-started stories (should be 25)  
gh issue list --repo RoySalisbury/HVO.Enterprise --label status:not-started
```

## Done! 🎉

All 30 user stories are now tracked as GitHub issues with:
- ✅ Proper status labels (complete/not-started)
- ✅ Story points (sp-X)
- ✅ Sprint assignments (sprint-1 through sprint-10)
- ✅ Category labels (core-package, extension-package, testing, documentation)
- ✅ Priority labels (p0, p1, p2, p3)

## Completed Stories

These 5 stories will have the `status:complete` label:
- US-001: Core Package Setup (3 SP)
- US-002: Auto-Managed Correlation (5 SP)
- US-003: Background Job Correlation (5 SP)
- US-004: Bounded Queue Worker (8 SP)
- US-019: HVO.Common Library (5 SP)

## Need More Details?

- Full documentation: `VALIDATION-SUMMARY.md`
- Script documentation: `scripts/README.md`
- Manual creation guide: `scripts/create-issues.md`
- User story files: `docs/user-stories/US-*.md`

## Troubleshooting

**GitHub CLI not installed?**
```bash
# macOS
brew install gh

# Linux
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh
```

**Authentication failed?**
```bash
gh auth logout
gh auth login
```

**Issue body too large?**
The generated script uses `--body-file` which should handle large files. If you still have issues, you may need to truncate some of the longer user stories or create them manually via the GitHub web UI.
