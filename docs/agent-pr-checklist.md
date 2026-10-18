# Agent PR checklist

Use this checklist before requesting review on an agent-authored PR.

## Local quality gates

- [ ] Local build and unit tests pass (`PrivateBuild.ps1` or equivalent compile + unit tests).
- [ ] Integration tests pass with SQLite when SQL Server is unavailable (`DATABASE_ENGINE=SQLite`).
- [ ] CRAP gate passes (`pwsh scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations`; threshold from `scripts/crap/crap-gate-threshold.json`).
- [ ] Qodana findings are clean relative to `qodana.sarif.json` (no new P1 noise; baseline respected).
- [ ] Acceptance tests pass when UX or end-to-end behavior changed (`AcceptanceTests.ps1` / Playwright).

## Pull request shape

- [ ] PR title states the change clearly and links the work item (for example `Fixes #1234` or `Refs #1234`).
- [ ] Branch name follows the pattern required by the session that launched the work — see [Branch naming](branch-naming.md).

## Notes

- Docs-only changes may skip .NET build and test gates when the Technical Design says so.
- Do not merge until CI is green and a human reviews.
