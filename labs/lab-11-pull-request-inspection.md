# Lab 11: Pull Request as a Formal Inspection

**Curriculum Section:** Section 06 (Operate/Execute - Pull Requests as Formal Inspections)
**Estimated Time:** 35 minutes
**Type:** Build + Process

---

## Objective

Practice the PR review process as a quality gate by creating a PR for your work and peer-reviewing another student's PR.

---

## Steps

### Step 1: Prepare Your Changes

Ensure all code changes from prior labs are on a feature branch. Run:

```powershell
.\privatebuild.ps1
dotnet format style src/ChurchBulletin.sln --verify-no-changes
```

### Step 2: Review the PR Template

Open `.github/pull_request_template.md`. Check each item against your changes.

### Step 3: Create the Pull Request

Push your branch and create a PR with a proper description.

### Step 4: Peer Review Exercise

Swap PRs with a partner. Review using `.github/copilot-code-review-instructions.md`:

| Check | Pass/Fail |
|-------|-----------|
| No new NuGet packages | |
| Uses Shouldly (not FluentAssertions) | |
| Test doubles prefixed with `Stub` | |
| Onion Architecture respected | |
| No secrets or credentials | |

### Step 5: Leave Review Comments

At least 1 positive comment, 1 constructive suggestion, and 1 question.

### Step 6: Address Feedback

Review comments on your own PR and push fixes.

---

## Expected Outcome

- A properly created PR with checklist completed
- Experience giving and receiving code review feedback

---

## Discussion Questions

1. How does PR review implement the "Second Way of DevOps" (Amplify Feedback Loops)?
2. What is the cost/benefit tradeoff of formal PR reviews?
