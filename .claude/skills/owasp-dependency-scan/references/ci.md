# CI integration

Goal: a gated dependency scan that fails the build on High/Critical CVEs, caches the NVD database so
runs stay fast, and publishes results. Per repo conventions, **do not modify pipeline files without
explicit approval** — propose the change and let the maintainer apply it.

## Principles

- **Cache the NVD database** (`-DataDir` / the mounted `data` volume) between runs. A cold download
  is 10+ minutes; a warm cache is seconds. Key the cache on a daily/weekly rotation so it refreshes.
- **Store `NVD_API_KEY` as a secret** and expose it as an environment variable.
- **Gate with `-FailOnCvss 7.0`** (CVSS ≥ 7.0 = High/Critical) so only serious issues break the build.
- **Publish SARIF** to the platform's code-scanning UI and **JUnit** for test-result panes.
- Use the **Docker runner** in CI — it needs no Java setup and is version-pinned.

## GitHub Actions

```yaml
- name: Cache NVD database
  uses: actions/cache@v4
  with:
    path: ~/.owasp-dc-data
    key: owasp-dc-nvd-${{ github.run_id }}
    restore-keys: owasp-dc-nvd-

- name: Dependency scan
  shell: pwsh
  env:
    NVD_API_KEY: ${{ secrets.NVD_API_KEY }}
  run: |
    pwsh .claude/skills/owasp-dependency-scan/scripts/scan.ps1 `
      -Path . -FailOnCvss 7.0 -Format "HTML,JSON,SARIF,JUNIT"

- name: Upload SARIF
  if: always()
  uses: github/codeql-action/upload-sarif@v3
  with:
    sarif_file: .security/dependency-scan/dependency-check-report.sarif
```

## Azure DevOps

```yaml
- task: Cache@2
  inputs:
    key: 'owasp-dc-nvd | "$(Build.BuildId)"'
    restoreKeys: 'owasp-dc-nvd'
    path: $(HOME)/.owasp-dc-data

- pwsh: |
    pwsh .claude/skills/owasp-dependency-scan/scripts/scan.ps1 `
      -Path . -FailOnCvss 7.0 -Format "HTML,JSON,JUNIT"
  displayName: Dependency scan
  env:
    NVD_API_KEY: $(NVD_API_KEY)

- task: PublishTestResults@2
  condition: always()
  inputs:
    testResultsFormat: JUnit
    testResultsFiles: .security/dependency-scan/dependency-check-junit.xml
```

## Threshold guidance

| `-FailOnCvss` | Effect |
|---------------|--------|
| (omitted) | Never fails on severity. Local triage / report-only. |
| `9.0` | Fail only on Critical. Lenient gate for legacy code. |
| `7.0` | Fail on High + Critical. **Recommended default.** |
| `4.0` | Fail on Medium and above. Strict; expect noise from transitive/dev deps. |

Pair a strict threshold with a suppression file (`references/suppression.md`) so known,
risk-accepted, or false-positive findings don't block every build.
