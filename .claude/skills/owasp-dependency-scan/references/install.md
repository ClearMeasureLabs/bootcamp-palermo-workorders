# Installing / running OWASP Dependency-Check

Current version: **12.x** (12.2.2 as of mid-2026). Two supported runners.

## Runner 1: Docker (recommended)

No Java on the host is required, and the run is identical locally and in CI. The NVD database
persists in a mounted volume so only the first run is slow.

```powershell
docker run --rm `
  -v "${PWD}:/src:ro" `
  -v "$HOME/.owasp-dc-data:/usr/share/dependency-check/data" `
  -v "${PWD}/.security/dependency-scan:/report" `
  owasp/dependency-check:latest `
  --scan /src --project myproject --out /report `
  --format HTML --format JSON --enableExperimental `
  --nvdApiKey $env:NVD_API_KEY
```

`scripts/scan.ps1` uses this path automatically when the Docker daemon is reachable. It passes
`--disableAssembly` because the .NET Assembly Analyzer needs a bundled runtime not present in the
image — the native `dotnet list package --vulnerable` scan covers .NET assemblies far better anyway.

## Runner 2: Installed CLI

**Requires Java 11+.** OWASP DC 12.x will not run on Java 8. Check with `java -version`; if it reports
`1.8.x` install a newer JRE (e.g. `choco install temurin17` or Microsoft OpenJDK) and ensure it is
first on `PATH`, or just use the Docker runner.

Install the CLI:

```powershell
# Chocolatey (needs an elevated shell)
choco install dependency-check

# Or download the release zip and add its bin/ to PATH
# https://github.com/dependency-check/DependencyCheck/releases
```

Run:

```powershell
dependency-check --scan . --project myproject --out .security/dependency-scan `
  --data $HOME/.owasp-dc-data --format HTML --format JSON `
  --nvdApiKey $env:NVD_API_KEY
```

## NVD API key

Since v9.0 OWASP DC pulls CVE data from the NVD API. Without a key the first update is heavily
rate-limited (often 10+ minutes) and can fail with 403/429 errors. With a key it takes a few minutes.

1. Request a free key: https://nvd.nist.gov/developers/request-an-api-key
2. Set it in the environment so the script picks it up automatically:
   ```powershell
   setx NVD_API_KEY "your-key-here"     # persists for new shells
   $env:NVD_API_KEY = "your-key-here"   # current shell
   ```
3. In CI, store it as a secret and expose it as the `NVD_API_KEY` environment variable.

The downloaded database caches under `-DataDir` (default `~/.owasp-dc-data`). After the first
successful update, later scans are fast even without a key. Persist this directory in CI.

## Common failures

| Symptom | Cause / fix |
|---------|-------------|
| Hangs 10+ min on "Download Started for NVD CVE" | No API key or rate-limited. Set `NVD_API_KEY`; let the first run finish — it caches. |
| `UnsupportedClassVersionError` / won't start | Java < 11. Use Docker or install Java 17. |
| 403 / 429 from NVD | Rate limited without a key, or key invalid. |
| Docker "permission denied" on volume | On Linux CI, ensure the mounted paths are readable by the container user. |
| Empty report, no dependencies found | Scan path wrong, or only source with no restored packages. For .NET, rely on the native scanner. |
