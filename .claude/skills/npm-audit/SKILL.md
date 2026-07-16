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
  not be run for a real npm project — e.g. a missing lockfile, **or no usable
  toolchain (neither Docker nor local Node/npm) is available** even though a
  `package.json` is present. Fail safe: if a project exists but its security can't
  be verified, it does not pass. The per-project `message` says why it couldn't run.
- **notapplicable** — nothing to test: no `package.json` found anywhere under the
  root. (If no toolchain can run *and* there's no project, this is what you get — the
  metric simply doesn't apply.) A top-level `message` explains why.

Note the boundary: **package.json present + no usable toolchain → `fail`**
(unverifiable real project); **no package.json at all → `notapplicable`** (nothing
to audit).

**Default threshold is `high`** — any **high or critical** vulnerability fails.
Moderate and low are reported but don't fail by default. Tighten with
`--fail-on moderate` or loosen with `--fail-on critical`.

## How to run

Pick an execution environment in this order: **Docker (preferred) → local Node/npm
→ neither**. Docker is preferred because it needs no local Node install and runs the
audit in a clean, known-good toolchain. The engine (`audit.mjs`) is the same either
way — only *where* Node runs it differs.

### Step 1 — Preflight: choose an execution environment

**First, check Docker.** If the Docker CLI is installed *and* its daemon is running,
use container mode and **skip the local Node/npm check entirely**:

    docker info

- **Succeeds (exit 0)** → Docker is usable → **container mode** (Step 2A).
- **Fails** (not installed / daemon not running) → fall back to checking local Node
  and npm (works on macOS, Linux, and Windows):

      node --version
      npm --version

  - **Both succeed** → **local mode** (Step 2B).
  - **Either fails** (command not found / non-zero exit) → no usable toolchain →
    **Step 3**. Do not try to run `audit.mjs`; decide fail vs. notapplicable by hand
    from whether any `package.json` exists.

### Step 2A — Container mode (Docker available, preferred)

Run the engine inside an official Node image. Mount both the skill directory
(read-only, so `audit.mjs` is reachable) and the target root at their **same
absolute paths** inside the container — this makes the reported paths real host
paths and writes the report back to the host:

```bash
docker run --rm \
  -v "<skill-dir>":"<skill-dir>":ro \
  -v "<root-path>":"<root-path>" \
  -w "<root-path>" \
  node:lts \
  node "<skill-dir>/audit.mjs" --dir "<root-path>"
```

- Replace `<skill-dir>` with this skill's directory and `<root-path>` with the
  target root (defaults to the current working directory).
- `npm audit` inside the container needs network access to fetch advisories; the
  default Docker bridge network provides it.
- Every option from Step 2B (`--fail-on`, `--depth`, `--out`, additional `--dir`)
  works identically — append them after the script path. For multiple roots, add a
  `-v "<other-root>":"<other-root>"` mount **and** a matching `--dir "<other-root>"`
  per root.
- The container's exit code is the verdict: `0` = pass, `1` = fail, `2` =
  notapplicable. `audit.mjs` still writes the report only when a `package.json` is
  found, so an empty target inside the container yields `notapplicable`, never a
  false fail.
- On Linux, container-written report files may be owned by `root` (Docker Desktop on
  macOS/Windows maps them to the host user automatically). If that's a problem, add
  `--user "$(id -u):$(id -g)"` to the `docker run` command.

### Step 2B — Local mode (Node + npm available)

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

### Step 3 — Neither Docker nor local Node/npm available

If Step 1 found no usable Docker **and** local Node/npm can't be run, the engine
cannot run at all. Look for a `package.json` anywhere under the target root (ignoring
`node_modules`), then produce the report yourself and still write both files to the
output dir (`<root>/codebase-audit-report/metrics/npm-audit/`):

- **A `package.json` exists → `status: "fail"`.** It's a real npm project whose
  security can't be verified, so fail safe. Use a `message` such as:
  `"No usable toolchain — Docker is unavailable and Node.js/npm is not installed; npm audit could not be run, failing safe since security is unverified."`
- **No `package.json` anywhere → `status: "notapplicable"`.** Nothing to audit. Use
  a `message` like: `"No npm project found and no usable toolchain (Docker or Node.js/npm) is available — npm audit not applicable."`

Write `npm-audit-result.json` in the same shape as Step 2B (below) — set `status`,
`message`, `threshold`, `auditedRoot`, `projectCount`, zeroed `vulnerabilities`, and
a `projects[]` entry per discovered `package.json` (each `status: "fail"` with the
message). Write a matching `npm-audit-result.md`. Report the status to the user and
note that installing Docker or Node.js/npm is required to actually verify the
dependencies.

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
