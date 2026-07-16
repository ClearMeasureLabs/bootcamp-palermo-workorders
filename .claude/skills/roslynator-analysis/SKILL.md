---
name: roslynator-analysis
description: Run Roslynator static analysis against a .NET solution, then parse and summarize the diagnostics. Use when the user wants to run Roslynator, analyze C#/.NET code quality, run static/code analysis on a solution (.sln), or mentions Roslynator, analyzers, or code smells in a .NET repo.
---

# Roslynator Analysis

Runs the Roslynator CLI `analyze` command against a solution and prints a parsed summary
(severity breakdown, compiler-vs-analyzer split, top rules, report link). The heavy lifting
lives in `scripts/analyze.ps1` — a deterministic, self-healing wrapper.

## Quick start

From the repo root, run the script (use this skill's directory for the path):

```powershell
pwsh <skill-dir>/scripts/analyze.ps1 -Solution src/ChurchBulletin.sln
```

Then relay its printed summary to the user.

## Workflow

1. **Resolve the solution.** If the user named a `.sln`/`.slnx`, pass it via `-Solution`.
   Otherwise call the script with no `-Solution`; it auto-uses the sole solution, or exits 3
   listing candidates. On that exit-3 list, ask the user which one (AskUserQuestion) and
   re-invoke with `-Solution`.

2. **Run the script.** `pwsh <skill-dir>/scripts/analyze.ps1 -Solution <path>`
   Optional: `-SeverityLevel info|hidden|warning|error`, `-Output <dir>`, `-TopRules <n>`.
   The script never runs `roslynator fix` and never modifies source.

3. **Handle exit codes.**
   - `0` → success; relay the summary it printed.
   - `2` → Roslynator not installed. Ask the user, then `dotnet tool install -g roslynator.dotnet.cli`, and re-run.
   - `3` → solution resolution problem (none found, or several — pick one and pass `-Solution`).
   - `4` → analysis failed for another reason. See [resources/TROUBLESHOOTING.md](resources/TROUBLESHOOTING.md).

4. **Report back** what the script printed: the solution, total + severity breakdown, the
   compiler-vs-analyzer split (call out that `CS*` entries are usually resolution noise, not
   defects), the top analyzer rules, and the clickable report link.

## What the script handles for you

- Verifies the Roslynator CLI is installed (exit 2 if not).
- Writes the XML report to an OS-agnostic temp folder (`<temp>/roslynator-results/analysis.xml`).
- **Self-heals** the known .NET 10 SDK MSBuild conflict (a `FileUtilities` `TypeLoadException`)
  by neutralizing the bundled `Microsoft.Build.Framework.dll` and retrying once. Details and the
  manual fallback are in [resources/TROUBLESHOOTING.md](resources/TROUBLESHOOTING.md).
- Splits compiler (`CS*`) diagnostics from analyzer rules so resolution noise doesn't read as defects.

## Notes

- Prefer the CLI over the `Roslynator.Analyzers` NuGet package — the CLI needs no `.csproj`/package changes.
- The report lives in the OS temp folder, so it is not tracked by git and may be cleared by the OS.
- Large/multi-targeted solutions can take a while; let the run finish.
