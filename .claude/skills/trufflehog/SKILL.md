---
name: trufflehog-secret-scan
description: Scan changed code for leaked secrets (API keys, tokens, credentials, private keys) using TruffleHog. Use this WHENEVER code has just been modified and you want to confirm nothing sensitive leaked — after editing or generating code, before a commit or push, when reviewing a diff or PR, or when the user says things like "check for secrets", "did I leak any credentials", "scan my changes", "run trufflehog", or asks to set up a secret-scanning gate. Prefer this over ad-hoc grep/regex for credentials — TruffleHog knows 800+ credential formats and can verify whether a key is live. Scope the scan to the changed code, not the whole history, so it stays fast.
---

# TruffleHog secret scan (changed code)

Catch secrets before they leave the machine. This skill runs [TruffleHog](https://github.com/trufflesecurity/trufflehog) over **only the code that changed** and reports any credentials it finds, so a hardcoded API key or token doesn't get committed, pushed, or merged.

Scanning just the diff (rather than full git history) keeps it fast and keeps the signal focused on what the current change introduced.

## When to run

Run a scan whenever changed code should be checked, including:

- **Right after modifying or generating code** — if you just wrote or edited files, scan before considering the task done.
- **Before a commit** — gate the staged changes (`--mode staged`).
- **Before a push / in a PR** — scan the branch's committed diff against the base (`--mode range --base main`).
- **On request** — the user asks to check for leaked secrets, verify credentials, or set up a scanning gate.

Default to the `working` mode when unsure — it covers everything currently uncommitted.

## Prerequisite: TruffleHog must be installed

Check first: `command -v trufflehog`. If it's missing, install it (pick what fits the environment):

```bash
# macOS / Linux (Homebrew)
brew install trufflehog

# Linux / macOS (official install script -> /usr/local/bin)
curl -sSfL https://raw.githubusercontent.com/trufflesecurity/trufflehog/main/scripts/install.sh | sh -s -- -b /usr/local/bin

# Go toolchain
go install github.com/trufflesecurity/trufflehog/v3@latest

# Docker (no local install)
docker run --rm -v "$(pwd):/repo" trufflesecurity/trufflehog:latest \
  filesystem /repo --results=verified,unknown --json
```

If you can't install it, tell the user rather than falling back to a weaker grep-based check — a home-grown regex will miss most credential formats and produce false confidence.

## How to run

Use the bundled helper, which figures out the changed-file set and calls TruffleHog with the right subcommand and flags:

```bash
bash scripts/scan_changes.sh [--mode working|staged|range] [--base REF] [--head REF] [--no-verify] [--include-unverified]
```

Pick the mode by what "changed" means right now:

| Situation | Command |
|---|---|
| Uncommitted work (staged + unstaged + new files) — the safe default | `scripts/scan_changes.sh` |
| About to commit; check only what's staged (pre-commit gate) | `scripts/scan_changes.sh --mode staged` |
| Feature branch vs base branch (pre-push / PR) | `scripts/scan_changes.sh --mode range --base main` |
| Just the most recent commit | `scripts/scan_changes.sh --mode range --base HEAD~1` |
| A specific commit range | `scripts/scan_changes.sh --mode range --base <old> --head <new>` |
| Repo lives elsewhere | add `--repo /path/to/repo` |

The script writes findings to **stdout** as line-delimited JSON (one object per finding) and a short progress/summary line to **stderr**. It uses these exit codes:

- **0** — clean, no secrets in the changed code
- **183** — secrets found (TruffleHog's `--fail` convention)
- **2** — environment/usage error (not installed, not a git repo, bad arguments)

Always check the exit code; don't infer the result from log text.

## Verification: verified vs unverified vs unknown

TruffleHog can do more than pattern-match — it can call the credential's provider to check whether the key is **live**. Findings fall into:

- **verified** — confirmed active by the provider. Treat as a live, exposed secret. Highest priority.
- **unknown** — verification was attempted but errored (e.g. network blocked, provider down). Could be live; treat seriously.
- **unverified** — matched a credential pattern but not confirmed. Includes real keys that couldn't be checked *and* false positives (example keys, random high-entropy strings).

The helper's default (`--results=verified,unknown`) is tuned for a **pass/fail gate**: it fails on live keys and verification errors while keeping false positives out of the blocking decision.

- For a thorough review ("did I leak *anything*?"), add `--include-unverified` and triage the extra matches yourself — a hardcoded key matters even if it's currently rotated.
- **Verification makes live network calls to third-party APIs.** In an offline, sandboxed, or privacy-sensitive environment, pass `--no-verify`. You lose the verified/unverified distinction but still catch pattern matches, and nothing leaves the machine.

## Reading the findings

Each finding line is JSON. The fields that matter:

- `DetectorName` — what kind of credential (e.g. `Github`, `AWS`, `Stripe`).
- `Verified` — `true` means confirmed live.
- `Raw` / `RawV2` — the matched secret. **Never echo the full secret back to the user** — show the detector, file, and line, and at most a masked fragment (first/last few chars).
- `SourceMetadata.Data.Filesystem.file` and `.line` (filesystem/working/staged modes), or `SourceMetadata.Data.Git.file`, `.line`, and `.commit` (range mode) — where it is.

Summarize like: "Found a **verified** GitHub token in `config.py:12` and an **unverified** Stripe key in `billing.js:40`." Group by file, lead with verified findings.

## If secrets are found — remediation

Deleting the line is **not** enough. If a real secret was committed anywhere (or pushed), assume it's compromised. Guide the user through, in order:

1. **Rotate / revoke first.** Generate a new credential and revoke the exposed one at the provider. This is the only step that actually closes the exposure — do it before anything else, especially for `verified` findings.
2. **Remove it from the code.** Replace the hardcoded value with an environment variable or a secrets manager; add the config file to `.gitignore` if appropriate.
3. **If it was already committed, purge it from git history** (e.g. `git filter-repo`, or BFG Repo-Cleaner) — a plain follow-up commit leaves the secret in history. Note that once pushed, it may already be cached by others; rotation (step 1) remains essential.
4. **Re-scan** to confirm the changed code is clean.

Explain the "rotate before you clean up" ordering — people's instinct is to delete the line first, but a secret that's already left the machine stays valid until it's revoked.

## Setting up an ongoing gate

If the user wants scanning to run automatically on every change (which matches "run after code has changed"), offer to wire it into their workflow rather than only running once:

- **Pre-commit hook** (via [pre-commit](https://pre-commit.com)): add a `local` hook running `trufflehog git file://. --since-commit HEAD --results=verified,unknown --fail`. Advise `git add` then `git commit` separately (avoid `git commit -am`) so the hook sees all intended changes.
- **CI (GitHub Actions / GitLab / etc.)**: run TruffleHog as a step on push/PR, scanning the base→head diff with `--results=verified,unknown --fail`. The official `trufflesecurity/trufflehog` GitHub Action handles base/head automatically.

Point the user to the current README (https://github.com/trufflesecurity/trufflehog) for platform-specific snippets, since flags and action inputs evolve.

## Notes

- Always scan a git repo with the `git` subcommand (the helper does this for `range` mode) — git's object storage needs a different path than plain filesystem scanning. The `working`/`staged` modes intentionally use `filesystem` on the changed files because those changes aren't committed yet.
- Keep scans scoped to the change. A full-history scan is a different, much slower task; only do it if the user explicitly asks to audit the whole repository.
