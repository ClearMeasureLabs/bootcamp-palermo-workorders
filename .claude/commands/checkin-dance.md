---
name: "Checkin Dance"
description: Backmerge, commit, push, create/update PR, and monitor builds until green
category: Workflow
tags: [workflow, git, ci]
---

Backmerge from master, commit, push, create or update PR, and monitor PR builds. When all PR builds, checks, and comments are resolved, report completed. If a PR comment shows up, evaluate what to do, do it, comment a reply, and resolve it.

**Delegation:** If a subagent (Task tool) is available, delegate the entire checkin dance to a subagent so the main agent remains free for prompting. The subagent should execute all steps below autonomously and return only the final status (success with PR URL, or failure with details). If no subagent is available, execute the steps directly.

**Steps**

1. **Backmerge from master**
   - Run `git fetch origin` to get the latest remote state
   - Run `git merge origin/master` to merge latest master into the current branch
   - If there are merge conflicts, resolve them:
     - Analyze each conflicting file to understand the intent of both sides
     - Choose the resolution that preserves both changes where possible
     - Stage resolved files with `git add`
     - Complete the merge with `git commit`
   - If conflicts are too complex or ambiguous, abort the merge and report back to the user

2. **Check working tree status**
   - Run `git status` and `git diff` to understand pending changes
   - Run `git log --oneline -5` to understand recent commit message style

3. **Commit changes**
   - Stage relevant files with `git add`
   - Create a concise commit message summarizing the changes
   - Do not commit files that likely contain secrets

4. **Push to remote**
   - Push the current branch to origin with `git push -u origin <branch>`

5. **Create or update PR**
   - Check if a PR already exists for this branch: `gh pr view --json number 2>nul`
   - If no PR exists, create one with `gh pr create` including a summary of changes
   - If a PR already exists, it will automatically pick up the new push

6. **Monitor PR builds**
   - Run `gh pr checks <pr-number> --watch` to monitor all CI checks
   - Wait for all checks to complete

7. **Handle results**
   - If all checks pass and no PR comments need attention, report completed
   - If a check fails, investigate the failure logs, fix the issue, and loop back to step 3
   - If a PR comment appears, evaluate it, make changes if needed, reply to the comment, resolve it, and loop back to step 3

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
- If merge conflicts are too complex, abort and ask the user
- If a fix requires significant changes, pause and ask the user before proceeding
- Report the PR URL when done
