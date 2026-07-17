# .NET / NuGet scanning

## Why the native scanner is authoritative for .NET

OWASP Dependency-Check identifies libraries by hashing/inspecting files and matching them to CPE
entries in the NVD. For modern .NET (SDK-style projects using `PackageReference`), this is unreliable
for **transitive** dependencies — the packages a project pulls in indirectly — because they aren't
present as discrete files until restore, and NuGet CPE matching is noisy (both false positives and
misses).

`dotnet list package --vulnerable --include-transitive` instead queries the same vulnerability data
NuGet/GitHub Advisory uses, keyed precisely by package id + resolved version. It is fast, needs no NVD
download, and reports the exact resolved versions. Treat it as the source of truth for NuGet CVEs;
use OWASP DC for cross-ecosystem coverage and defense-in-depth.

## Commands

```powershell
# Restore first — the vulnerability data resolves against restored versions.
dotnet restore src/ChurchBulletin.sln

# Direct + transitive vulnerabilities, machine-readable.
dotnet list src/ChurchBulletin.sln package --vulnerable --include-transitive --format json

# Human-readable.
dotnet list src/ChurchBulletin.sln package --vulnerable --include-transitive
```

`scripts/scan.ps1` runs this for every `.sln` (or every `.csproj` if there is no solution) and folds
the results into `summary.md`.

## Fixing transitive vulnerabilities

A vulnerable **transitive** package is not referenced directly, so you can't just bump it in the
`.csproj`. Options, in order of preference:

1. **Upgrade the direct package that pulls it in.** Find the chain:
   ```powershell
   dotnet list src/ChurchBulletin.sln package --include-transitive
   ```
   Then upgrade the top-level package to a version whose dependency graph uses the fixed transitive.
2. **Add a direct reference pinning the fixed version** when no upgraded parent exists yet. Adding a
   top-level `PackageReference` to the fixed version overrides the transitive resolution:
   ```powershell
   dotnet add src/<Project>/<Project>.csproj package <TransitivePackage> --version <fixed>
   ```
   Document why (a security pin), since it can look like an unused reference.
3. **Central Package Management**: if the repo uses `Directory.Packages.props`, set the version there.

Per this repo's conventions, adding or changing NuGet packages needs explicit approval — propose the
specific version bump and its justification rather than applying it silently.

## packages.lock.json (optional, improves OWASP DC too)

Enabling lock files gives a deterministic, fully-resolved dependency graph that both scanners read
more reliably:

```xml
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

After restore this produces `packages.lock.json` per project. Commit it. It also makes CI scans
reproducible.
