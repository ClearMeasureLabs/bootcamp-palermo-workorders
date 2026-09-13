# Contributing

1. Branch from `master` and open a pull request; do not commit directly to `master`.
2. Every commit message references its work item number, e.g. `#1234`.
3. Run the private build locally before opening a pull request.

## Before you open a pull request

See the [README quick-start](README.md) for setup steps, then confirm each item below:

- [ ] Branched from `master`
- [ ] Ran `pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1` locally and it passes
- [ ] PR is scoped to a single work item
- [ ] PR title includes the work item number (e.g. `#1234 — description`)
- [ ] No secrets or local settings (e.g. `appsettings.*.json` overrides) are committed
