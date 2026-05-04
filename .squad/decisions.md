# Squad Decisions

## Active Decisions

### Use local PlantUML includes to stabilize CI renders

- Status: Accepted
- Date: 2026-05-04

Context
- PR #5932 changed many diagrams to use !includeurl pointing to raw.githubusercontent.com with the branch docs/architecture-enhancements.
- Diagram rendering in CI failed or is fragile when the theme was fetched remotely: network dependency, fork/branch mismatch, intermittent failures.

Decision
- Use a local, relative include for the PlantUML theme inside the repository: !include templates/plantuml-theme.puml (path relative to files in arch/).

Trade-offs
- Pros: Reliable CI rendering for pull requests (including forks); no external network dependency; simpler to reason about rendering environment.
- Cons: Removes the external single-source-of-truth for the theme; updating theme requires changing repo content.

Implementation
- Replace !includeurl .../docs/architecture-enhancements/arch/templates/plantuml-theme.puml with !include templates/plantuml-theme.puml in arch/*.puml (committed on branch).

Owner: Danny — Lead / Architect


## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
