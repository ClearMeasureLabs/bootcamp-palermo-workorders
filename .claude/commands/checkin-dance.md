---
name: "Checkin Dance"
description: Commit, push, create/update PR, and monitor builds until green
category: Workflow
tags: [workflow, git, ci]
---

Commit, push, create or update PR, and monitor PR builds. When all PR builds, checks, and comments are resolved, report completed. If a PR comment shows up, evaluate what to do, do it, comment a reply, and resolve it.

**Steps**

1. **Check working tree status**
   - Run `git status` and `git diff` to understand pending changes
   - Run `git log --oneline -5` to understand recent commit message style

2. **Commit changes**
   - Stage relevant files with `git add`
   - Create a concise commit message summarizing the changes
   - Do not commit files that likely contain secrets

3. **Push to remote**
   - Push the current branch to origin with `git push -u origin <branch>`

4. **Create or update PR**
   - Check if a PR already exists for this branch: `gh pr view --json number 2>nul`
   - If no PR exists, create one with `gh pr create` including a summary of changes
   - If a PR already exists, it will automatically pick up the new push

5. **Monitor PR builds**
   - Run `gh pr checks <pr-number> --watch` to monitor all CI checks
   - Wait for all checks to complete

6. **Handle results**
   - If all checks pass and no PR comments need attention, report completed
   - If a check fails, investigate the failure logs, fix the issue, and loop back to step 2
   - If a PR comment appears, evaluate it, make changes if needed, reply to the comment, resolve it, and loop back to step 2

**Output On Success**

```
## Checkin Dance Complete

**Branch:** <branch-name>
**PR:** <pr-url>
**Status:** All checks passed, no outstanding comments
```

**Output On Failure**

```
## Checkin Dance - Action Required

**Branch:** <branch-name>
**PR:** <pr-url>
**Failed Check:** <check-name>
**Error:** <summary of failure>

Investigating and fixing...
```

**Guardrails**
- Never force push unless explicitly requested
- Never commit secrets or credentials
- Always verify commit succeeded before pushing
- If a fix requires significant changes, pause and ask the user before proceeding
- Report the PR URL when done
