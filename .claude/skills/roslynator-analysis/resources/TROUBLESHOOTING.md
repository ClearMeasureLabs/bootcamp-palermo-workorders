# Roslynator Analysis — Troubleshooting

## `TypeLoadException: Could not load type 'Microsoft.Build.Framework.FileUtilities'`

**Symptom.** `roslynator analyze` fails at "Loading solution ..." with:

```
System.AggregateException: ... Could not load type 'Microsoft.Build.Framework.FileUtilities'
from assembly 'Microsoft.Build.Framework, Version=15.1.0.0, ...'
   at Microsoft.Build.Construction.SolutionFile.Parse(String solutionFile)
```

Roslynator typically exits 2 for this failure; `scripts/analyze.ps1` detects the signature, applies the workaround, and retries. If the workaround does not resolve it, `analyze.ps1` exits 4 (analysis failed).

**Cause.** Roslynator 0.12.0 (the latest release) ships its own `Microsoft.Build.Framework.dll`
inside the tool store. Against the .NET 10 SDK, that bundled copy shadows the SDK's MSBuild and
mismatches `Microsoft.Build.dll` — 0.12.0 was built for MSBuild 17.7, the SDK 10 MSBuild is 17.14,
and the `FileUtilities` type moved between them. The `15.1.0.0` in the message is MSBuild's
back-compat *assembly* version and is not the real version — check *file* versions instead.
MSBuildLocator is supposed to load `Microsoft.Build.*` from the registered SDK; shipping the DLL
locally breaks that contract.

**What does NOT fix it.**

- `--msbuild-path <sdk>` — the bundled DLL still shadows the SDK's copy.
- `dotnet tool update` / uninstall + reinstall — 0.12.0 is already the latest; same DLL returns.
- Clearing MSBuild env vars / restarting the shell — it is a packaging conflict, not env state.

**Fix (automated).** `scripts/analyze.ps1` detects this signature and neutralizes the bundled
DLL automatically, then retries. No action needed.

**Fix (manual).** Rename the bundled framework DLL so MSBuildLocator supplies the SDK's version:

```powershell
$dll = Join-Path $HOME '.dotnet/tools/.store/roslynator.dotnet.cli/0.12.0/roslynator.dotnet.cli/0.12.0/tools/net10.0/any/Microsoft.Build.Framework.dll'
Rename-Item $dll "$dll.bak"
```

Adjust the `0.12.0` and `net10.0` segments to match the installed tool version and runtime. On
non-Windows hosts the store is under `~/.dotnet/tools/.store`.

**Caveat — it re-breaks after tool updates.** Any `dotnet tool update`/reinstall restores the bad
DLL, so the workaround must be re-applied. The script re-applies it on demand; a manual fix does not.
If Tasks/Utilities conflicts surface later (during restore/build rather than solution load), the same
approach applies to `Microsoft.Build.Tasks.Core.dll` / `Microsoft.Build.Utilities.Core.dll`.

---

## A large block of `CS####` errors in the results

Not a tool failure. `CS*` are C# *compiler* diagnostics (e.g. `CS0117`, `CS0103`, `CS0246`,
`CS0234`), and a big cluster of them means Roslynator's MSBuild workspace didn't fully
restore/resolve references or run source generators — **not** that the code is broken. The
summary from `analyze.ps1` separates `CS*` from analyzer rules for this reason.

- If a normal build (`. .\build.ps1 ; Build`) is green, ignore the `CS*` entries.
- Running `dotnet restore` on the solution before analyzing usually reduces them.
- Focus on the analyzer rules (`CA*`, `NUnit*`, `SYSLIB*`, `RCS*`, …) — those are the real findings.
