## Learnings

- Prefer Docker rendering to match CI; fall back to .tools/plantuml.jar when Docker unavailable.
- 2026-05-04: Rendered diagrams and committed generated SVGs to origin/docs/architecture-enhancements (commit af194d4ba474a0eb88d08a67ce6727c5de92ec7c). Recommended enabling CI auto-commit step for non-fork PRs to keep rendered assets in sync.
