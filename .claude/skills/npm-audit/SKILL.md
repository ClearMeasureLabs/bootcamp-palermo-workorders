---
name: npm-audit
description: Run npm security audits across a codebase and produce a PASS/FAIL/NOTAPPLICABLE report. Discovers every npm project (package.json) under the target root, audits each, and aggregates. By default any high or critical vulnerability fails. Writes npm-audit-result.json and npm-audit-result.md under codebase-audit-report/metrics/npm-audit, listing every vulnerable package (name@version, severity, reason) per project. Use when the user asks to audit npm dependencies, check for vulnerable packages, or gate a build/PR on dependency security.
---

# npm Security Audit (Pass / Fail / Not Applicable)

Audit a codebase's npm dependency security and return a status verdict, a severity
breakdown, and per-package detail. Handles a single project, a monorepo / multiple
project folders, or a repo with no npm project at all. Results are printed and
written to report files.

## Multi-project discovery

The script scans the target root(s) for directories containing `package.json` and
audits each as a separate project (scanning stops at each project — npm workspaces
are covered by auditing their root). `node_modules`, `.git`, hidden directories, and
the report output dir are skipped. The result **aggregates** all projects.

- **Multiple projects** → each audited; overall status fails if any project fails.
- **One project** → a single-entry aggregate.
- **No project found** → `notapplicable`.

## Status model & threshold

The overall `status` (and each project's `status`) is one of:

- **pass** — audited clean of any vulnerability at/above the threshold.
- **fail** — a threshold-breaking vulnerability was found, **OR** the audit could
  not be run for a real npm project (e.g. missing lockfile). Fail safe: unverified
  security does not pass. The per-project `message` says why it couldn't run.
- **notapplicable** — nothing to test: no `package.json` found anywhere under the
  root, or `npm` isn't installed. A top-level `message` explains why.

**Default threshold is `high`** — any **high or critical** vulnerability fails.
Moderate and low are reported but don't fail by default. Tighten with
`--fail-on moderate` or loosen with `--fail-on critical`.

## How to run

The logic lives in `audit.mjs` in this skill's directory. Run it against the target
root (defaults to the current working directory):

```bash
node <skill-dir>/audit.mjs --dir <root-path>
```

Options:
- `--dir <path>` — root to scan for projects. Repeat the flag to audit several
  explicit roots (default: current directory).
- `--fail-on critical|high|moderate|low` — severity gate (default: `high`).
- `--depth <n>` — how many directory levels deep to search for projects
  (default: `4`).
- `--out <path>` — report output directory
  (default: `<first-root>/codebase-audit-report/metrics/npm-audit`).

Exit codes: `0` = pass, `1` = fail, `2` = notapplicable.

## Output files

Written to `codebase-audit-report/metrics/npm-audit/`:

- **`npm-audit-result.json`**
  - `status` — `pass` | `fail` | `notapplicable` (overall)
  - `message` (optional) — why it's not applicable / global note
  - `threshold` — the failing gate used (default `high`)
  - `auditedRoot`, `projectCount`, `total`, `vulnerabilities` (aggregate counts)
  - `projects[]` — one per discovered project: `dir` (relative), `status`,
    `vulnerabilities`, `total`, optional `message` (e.g. audit couldn't run), and
    optional `packages[]` — every vulnerable package with `package` (name@version),
    `severity`, `failsThreshold` (bool), `reason` (advisory titles, if available),
    `vulnerableRange`.
- **`npm-audit-result.md`** — human-readable: overall status, a per-project summary
  table, and a per-project table of all vulnerable packages across every severity.

## Reporting to the user

Relay the console report (already concise): overall status, per-project pass/fail
lines, and the aggregate counts. Point at the written files for full detail rather
than pasting the whole JSON. If status is `notapplicable` or a project's audit
failed to run, state the `message` plainly — never report a false pass.
