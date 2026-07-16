# npm / Node scanning

## Commands

```powershell
# Machine-readable, from the directory containing package-lock.json.
npm audit --json

# Human-readable.
npm audit

# Production dependencies only (ignore devDependencies — build/test tooling).
npm audit --omit=dev
```

`scripts/scan.ps1` runs `npm audit --json` in every directory that contains a `package-lock.json`
(or `package.json`), skipping anything under `node_modules`, and folds results into `summary.md`.

A lockfile is required for meaningful results — `npm audit` resolves advisories against the exact
tree in `package-lock.json`. If only `package.json` exists, run `npm install` first to generate one.

## Interpreting severity

npm severities map to the report as: `critical` > `high` > `moderate` > `low` > `info`. Focus on
critical/high first. Distinguish:

- **Production dependencies** — shipped to users; highest priority.
- **devDependencies** — build/test/lint tooling; real but lower risk. Use `--omit=dev` to see the
  production-only picture, and note the split when reporting.

## Fixing

```powershell
# Apply non-breaking fixes (patch/minor within semver range).
npm audit fix

# Apply fixes that require major upgrades — CAN INTRODUCE BREAKING CHANGES.
npm audit fix --force
```

Cautions:

- `npm audit fix --force` may install major-version upgrades and break the build. Never run it
  unattended; review the diff and run the test suite after.
- Many advisories are in **transitive** dependencies. Prefer upgrading the direct dependency that
  pulls in the vulnerable package. Trace the chain with `npm ls <package>`.
- Some advisories have no fix available yet. Record them as accepted risk (with a note) or suppress
  the corresponding OWASP DC finding — don't force an upgrade that breaks things for a low-severity,
  dev-only issue.
