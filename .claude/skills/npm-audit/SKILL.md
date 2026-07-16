---
name: npm-audit
description: Run an npm security audit and produce a PASS/FAIL/NOTAPPLICABLE report. By default any high or critical vulnerability fails. Writes npm-audit-result.json and npm-audit-result.md under codebase-audit-report/metrics/npm-audit, listing every vulnerable package (name@version, severity, reason). Use when the user asks to audit npm dependencies, check for vulnerable packages, or gate a build/PR on dependency security.
---

# npm Security Audit (Pass / Fail / Not Applicable)

Produce a status verdict for a project's npm dependency security, a short severity
breakdown, and a full list of affected packages. Results are printed and written to
report files.

## Status model & threshold

The result `status` is one of:

- **pass** — the audit ran and found no vulnerability at or above the threshold.
- **fail** — the audit ran and found threshold-breaking vulnerabilities, **OR** the
  audit could not be run in a project that *is* an npm project (e.g. missing
  lockfile). We fail safe: if security can't be verified, it doesn't pass.
- **notapplicable** — there is nothing to test: no `package.json` (not an npm
  project), or `npm` isn't installed. A `message` explains why.

**Default threshold is `high`** — any **high or critical** vulnerability fails.
Moderate and low are reported but do not fail by default. Call out moderate/low
counts so the user can decide whether to tighten the gate with `--fail-on moderate`
(or `--fail-on critical` to loosen it).

## How to run

The logic lives in `audit.mjs` in this skill's directory. Run it against the target
project (defaults to the current working directory):

```bash
node <skill-dir>/audit.mjs --dir <project-path>
```

Options:
- `--dir <path>` — project to audit (default: current directory).
- `--fail-on critical|high|moderate|low` — severity gate (default: `high`).
- `--out <path>` — report output directory
  (default: `<dir>/codebase-audit-report/metrics/npm-audit`).

Exit codes: `0` = pass, `1` = fail, `2` = notapplicable.

The script runs `npm audit --json`, reads `metadata.vulnerabilities` for counts and
`vulnerabilities` for per-package advisory details (pulling installed versions from
the lockfile).

## Output files

Written to `codebase-audit-report/metrics/npm-audit/`:

- **`npm-audit-result.json`**
  - `status` — `pass` | `fail` | `notapplicable`
  - `message` (optional) — why it's not applicable, or why the audit failed to run
  - `threshold` — the failing gate used (default `high`)
  - `auditedDir`, `total`, `vulnerabilities` (counts per severity)
  - `packages` (optional) — every vulnerable package: `package` (name@version),
    `severity`, `failsThreshold` (bool), `reason` (advisory titles, if available),
    `vulnerableRange`
- **`npm-audit-result.md`** — human-readable version: status, severity table, and a
  table of all vulnerable packages across every severity level.

## Reporting to the user

Relay the console report (already concise): the status, the counts, and which
packages fail. Point at the written files for full detail rather than pasting the
whole JSON. If status is `notapplicable` or a run failure, state the `message`
plainly — never report a false pass.
