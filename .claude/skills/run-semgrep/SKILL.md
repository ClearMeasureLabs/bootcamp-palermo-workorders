---
name: run-semgrep
description: Run SemGrep static analysis against this codebase and summarize findings organized by severity. Use when asked to run semgrep, run a security scan or security audit, scan for vulnerabilities, check for SQL injection or hardcoded secrets, or evaluate static-analysis issues.
---

SemGrep security/static-analysis scan for this repo, run natively on Windows
PowerShell (no WSL, no Docker, no Python install). Drive it via
`.claude/skills/run-semgrep/driver.ps1` — it bootstraps SemGrep through `uvx`
(auto-installing `uv` via winget if needed), runs the scan, and prints a
summary grouped by severity (ERROR → WARNING → INFO).

All paths below are relative to the repo root.

## Prerequisites

- Network access (rulesets are fetched from the SemGrep registry each run).
- `uv` (provides `uvx`). The driver auto-installs it if missing; manual install:

```powershell
winget install --id astral-sh.uv -e --accept-source-agreements --accept-package-agreements
```

No Python required — SemGrep 1.170.0 ships `win_amd64` wheels and `uvx`
brings its own interpreter.

## Run (agent path)

Full scan with the default rulesets (`p/csharp`, `p/security-audit`,
`p/secrets`). Takes ~1–2 minutes for the full repo (~820 files); call with a
5-minute timeout:

```powershell
pwsh -NoProfile -File .claude/skills/run-semgrep/driver.ps1
```

Options (combine freely):

| option | what it does |
|---|---|
| `-ScanPath src/UI` | Scan one directory or file instead of the whole repo |
| `-Config p/csharp` | Override rulesets (repeatable: `-Config p/csharp,p/sql-injection`) |
| `-FailOnFindings` | Exit 1 if any ERROR/CRITICAL-severity finding exists (gating) |
| `-ShowParseErrors` | List files SemGrep could not fully parse |
| `-OutFile path.json` | Where raw JSON lands (default `$env:TEMP\semgrep-results.json`) |

Output: a summary block (file count, finding counts per severity, parse-error
count), then each finding grouped by severity with `path:line`, rule id, CWE,
and message. Raw SemGrep JSON (full messages, CWE/OWASP metadata, code
snippets) is at the `Raw JSON` path printed in the summary — read it when the
truncated summary message isn't enough.

Exit codes: 0 on a completed scan (findings or not); 1 only with
`-FailOnFindings` when ERROR-severity findings exist; throws if the scan
itself fails.

## Evaluating results

- **ERROR** findings are the actionable ones — report these first, with
  file:line and CWE. **WARNING** next; **INFO** is mostly style/hardening.
- Check the `Parse errors` count: those files were only partially analyzed,
  so findings there may be silently missed. `-ShowParseErrors` lists them.
- Known baseline (2026-07-16): exactly 1 ERROR —
  `csharp.lang.security.sqli.csharp-sqli` at `src/IntegrationTests/SqlExecuter.cs:27`
  (CWE-89, formatted string in SQL). It is test infrastructure that executes
  caller-supplied SQL by design; judge new scans against this baseline.

## Test

Quick smoke (scoped, ~15 s after first run):

```powershell
pwsh -NoProfile -File .claude/skills/run-semgrep/driver.ps1 -ScanPath src/Core -Config p/csharp
```

Expected: 64 files scanned, 0 findings, 2 parse errors.

## Gotchas

- **C# 12 primary constructors break SemGrep's C# parser** (still true in
  1.170.0). 34 files in this repo get `PartialParsing` errors (e.g.
  `src/Core/Model/Role.cs`, most `src/UI/Api/Controllers/*`). SemGrep skips
  the unparsed regions, so coverage in those files is degraded — do not treat
  a clean scan as proof those files are clean.
- **Windows has no real Python** — `python` is the Microsoft Store stub.
  That's why the driver uses `uvx` (own interpreter) instead of `pip install semgrep`.
- **Fresh shells don't see winget's PATH update** — the driver handles this
  (refreshes PATH from the registry, then globs
  `$env:LOCALAPPDATA\Microsoft\WinGet\Packages\astral-sh.uv*\uvx.exe`).
  Winget does NOT install uv to `~/.local/bin` (only the standalone installer does).
- **Do not run this via WSL Ubuntu 20.04**: SemGrep ≥ ~1.100 ships only
  `manylinux_2_34` wheels (needs glibc ≥ 2.34; Focal has 2.31) → uv falls
  back to an sdist build that produces an incompatible wheel. Native Windows
  is the supported path here.
- **SemGrep exits 0 even when findings exist** — use `-FailOnFindings` for gating.
- **`--metrics=off` works with registry rulesets** in 1.170 (older docs claim
  registry configs force metrics on).

## Troubleshooting

- **`irm https://astral.sh/uv/install.ps1 | iex` blocked/denied**: use the
  winget install line from Prerequisites instead — same result, per-user, no admin.
- **`The built wheel ...manylinux_2_34... is not compatible`**: you're running
  under an old-glibc Linux (e.g. WSL Ubuntu 20.04). Use the native Windows
  driver, or on Linux pin `uvx 'semgrep==1.99.0' --with 'setuptools<81'`
  (1.99.0 is the last manylinux2014 release; the setuptools pin is needed
  because its opentelemetry dep imports `pkg_resources`, removed in setuptools 81).
- **`Python was not found; run without arguments to install from the Microsoft Store`**:
  the Windows python stub. Ignore — nothing here needs system Python.
- **Scan hangs or times out**: first run downloads semgrep (~40 MB of wheels)
  plus three rulesets; allow 5 minutes. Subsequent runs use uv's cache.
