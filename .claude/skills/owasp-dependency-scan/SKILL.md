---
name: owasp-dependency-scan
description: >-
  Scan a project's supply-chain dependencies for known vulnerabilities (CVEs) using OWASP
  Dependency-Check plus the native .NET and npm scanners, then produce a consolidated,
  prioritized report. Use this whenever the user wants to check dependencies for security
  vulnerabilities, run an SCA (software composition analysis) scan, audit NuGet/npm/PackageReference
  packages, find CVEs in third-party libraries, check for supply-chain risk, run "OWASP dependency
  check", verify there are no vulnerable transitive dependencies before a release, or wire dependency
  scanning into CI. Trigger even when the user only says "scan dependencies", "check for vulnerable
  packages", "npm audit", "dotnet list vulnerable", or "is anything in here CVE'd" — this skill is the
  authoritative path for all of those in .NET and Node/npm codebases.
---

# OWASP Dependency Scan (.NET + npm)

Detect publicly disclosed vulnerabilities in a codebase's third-party dependencies. OWASP
Dependency-Check is the authoritative cross-ecosystem scanner (it correlates evidence against the
NVD, the GitHub Advisory DB, and others). Because its coverage of **transitive .NET PackageReference**
dependencies is weak, this skill pairs it with the fast native scanners — `dotnet list package
--vulnerable` and `npm audit` — and merges everything into one prioritized report.

Use `scripts/scan.ps1` as the orchestrator. It detects what ecosystems are present, picks the best
available OWASP DC runner, runs the native cross-checks, and writes a consolidated Markdown summary
plus machine-readable reports. Read this file end-to-end before running so the flags and thresholds
you pass match the user's intent (local triage vs. a CI gate).

## Decision flow

1. **Confirm scope.** Which repo/path, and is this a local triage scan or a CI gate that should fail
   the build? A gate needs a `-FailOnCvss` threshold; local triage usually does not.
2. **Pick the OWASP DC runner** (the script does this automatically, but understand the choice):
   - **Docker (preferred)** — `owasp/dependency-check` image. No Java needed, identical locally and in
     CI, and the NVD database persists in a named volume. Use whenever Docker is available.
   - **Installed CLI** — `dependency-check` on PATH. **Requires Java 11+** (Java 8 will not run
     OWASP DC 12.x). Install via `choco install dependency-check` or the release zip.
   - See `references/install.md` for both paths and the Java-version gotcha.
3. **Handle the NVD API key.** OWASP DC downloads the NVD data via the NVD API. Without a key the
   first update is heavily rate-limited (10+ minutes); with one it is minutes. The script reads
   `NVD_API_KEY` from the environment and passes `--nvdApiKey` automatically. If it is absent, warn
   the user the first run will be slow and point them to
   https://nvd.nist.gov/developers/request-an-api-key — then proceed without it (the database caches,
   so subsequent runs are fast regardless).
4. **Run the scan** (see below).
5. **Read the consolidated report, then explain and prioritize** — do not just dump the file. Lead
   with Critical/High findings, name the offending package and the fixed version, and give the exact
   upgrade command. See "Reporting" below.

## Running a scan

Local triage (interactive, no build failure):

```powershell
pwsh .claude/skills/owasp-dependency-scan/scripts/scan.ps1 -Path .
```

CI gate (fail on any CVSS ≥ 7.0 = High/Critical):

```powershell
pwsh .claude/skills/owasp-dependency-scan/scripts/scan.ps1 -Path . -FailOnCvss 7.0 -Format "HTML,JSON,SARIF,JUNIT"
```

Key parameters (run `Get-Help scripts/scan.ps1 -Detailed` for all of them):

| Parameter | Purpose |
|-----------|---------|
| `-Path` | Repo root to scan. Default `.`. |
| `-OutDir` | Report output directory. Default `<Path>/.security/dependency-scan`. |
| `-FailOnCvss <n>` | Exit non-zero if any finding's CVSS ≥ n. Omit for local triage; set (e.g. `7.0`) for CI gates. |
| `-Format` | OWASP DC report formats (comma-separated): `HTML,JSON,SARIF,JUNIT,CSV,ALL`. Default `HTML,JSON`. |
| `-NvdApiKey` | NVD API key. Defaults to `$env:NVD_API_KEY`. |
| `-Runner` | Force `docker` or `cli`. Default `auto` (Docker if present, else CLI). |
| `-SkipOwaspDc` | Run only the fast native scanners (no NVD download). Good for a quick first pass. |
| `-DataDir` | OWASP DC NVD database cache. Default `~/.owasp-dc-data`. Persist this in CI to skip re-downloads. |

The script always runs the native scanners in addition to OWASP DC:
- **.NET** — `dotnet list package --vulnerable --include-transitive` for every `.sln`/`.csproj` found. This is the most reliable source for transitive NuGet CVEs. See `references/dotnet.md`.
- **npm** — `npm audit --json` wherever a `package-lock.json` (or `package.json`) is found. See `references/npm.md`.

If the user wants a fast answer and does not need the NVD-backed OWASP DC pass yet, run with
`-SkipOwaspDc` first — the native scanners return in seconds and catch most real issues — then offer
the full OWASP DC scan.

## Reporting

The orchestrator writes `summary.md` in the output directory: a severity roll-up table, then one row
per finding with package, installed version, fixed version, CVE/advisory id, CVSS, and source
(owasp-dc / dotnet / npm). Present findings to the user like this — never just link the raw report:

```
## Dependency scan — <project>
**Critical: N · High: N · Moderate: N · Low: N**  (sources: OWASP DC, dotnet, npm audit)

### Must fix (Critical/High)
- **<package> <installed>** → upgrade to **<fixed>** — <CVE-id>, CVSS <score>. <one-line impact>.
  `dotnet add package <package> --version <fixed>`  (or `npm install <package>@<fixed>`)

### Should fix (Moderate)
...

### Notes
- False positives can be suppressed in `dependency-check-suppression.xml` (see references/suppression.md).
- <N> findings are transitive; fix by upgrading the top-level package that pulls them in.
```

Prioritize by severity, then by whether a fix exists. For transitive dependencies, identify the
direct package to bump (`dotnet list package --include-transitive` shows the tree; `npm ls <pkg>`
shows the chain) rather than telling the user to upgrade a package they don't reference directly.

## CI integration

For wiring this into the pipeline (GitHub Actions, Azure DevOps, Octopus) with a cached NVD database,
SARIF upload, and JUnit test results, read `references/ci.md`. The key points: cache `-DataDir`
between runs, store the `NVD_API_KEY` as a secret, and use `-FailOnCvss` to gate. Per this repo's
conventions, do not modify pipeline files without explicit approval — propose the change first.

## Reference files

- `references/install.md` — Docker vs. CLI install, Java 11+ requirement, NVD API key setup.
- `references/dotnet.md` — .NET/NuGet specifics, why native `dotnet list` beats OWASP DC for transitive deps, `packages.lock.json`.
- `references/npm.md` — npm/Node specifics, dev vs. prod dependencies, `npm audit fix` cautions.
- `references/ci.md` — Pipeline integration, caching, thresholds, SARIF/JUnit.
- `references/suppression.md` — Suppressing false positives with `dependency-check-suppression.xml`.
- `assets/dependency-check-suppression.xml` — Starter suppression file to copy into a repo.
