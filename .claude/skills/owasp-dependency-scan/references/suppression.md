# Suppressing false positives

OWASP Dependency-Check's CPE matching sometimes flags a package for a CVE that belongs to an
unrelated product with a similar name, or a finding the team has reviewed and accepted. Suppress
these with a suppression XML file rather than lowering the whole gate.

## Usage

1. Copy the starter file into the repo:
   ```
   assets/dependency-check-suppression.xml  ->  <repo>/dependency-check-suppression.xml
   ```
2. Pass it to the scan (add to the OWASP DC args in `scripts/scan.ps1`, or run OWASP DC directly):
   ```
   --suppression dependency-check-suppression.xml
   ```

## Getting the exact suppression snippet

Every finding in the **HTML report** has a "suppress" button that generates the precise
`<suppress>` block (with the right SHA/CPE/vulnerabilityName). Prefer copying that over hand-writing
rules — it targets exactly one finding on one artifact.

## Writing rules by hand

```xml
<?xml version="1.0" encoding="UTF-8"?>
<suppressions xmlns="https://jeremylong.github.io/DependencyCheck/dependency-suppression.1.3.xsd">

  <!-- Suppress ONE CVE on ONE artifact, matched by file hash (most precise). -->
  <suppress>
    <notes>False positive: CPE collision with unrelated product. Reviewed 2026-07-16.</notes>
    <sha1>384FAA82E193D4E4B0546059CA09572654BC3970</sha1>
    <cve>CVE-2020-15250</cve>
  </suppress>

  <!-- Suppress a CVE across any artifact matching a package URL regex. -->
  <suppress>
    <notes>Risk accepted: dev-only tooling, not shipped. Reviewed 2026-07-16.</notes>
    <packageUrl regex="true">^pkg:npm/some\-dev\-tool@.*$</packageUrl>
    <cve>CVE-2022-XXXXX</cve>
  </suppress>

</suppressions>
```

## Discipline

- **Always add a `<notes>` line** stating why it's suppressed and when it was reviewed. A silent
  suppression is indistinguishable from hiding a real vulnerability.
- Suppress the **narrowest** thing possible — one CVE on one artifact — not a whole package.
- Re-review suppressions periodically; a "no fix available" suppression may have a fix now.
