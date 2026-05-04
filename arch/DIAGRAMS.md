Diagram versioning & rendering policy

This repository stores PlantUML sources (arch/*.puml) alongside their rendered images (arch/*.png, arch/*.svg). A CI job (./github/workflows/render-diagrams.yml) validates that the rendered images are up-to-date with the sources on pull requests.

Guidelines
- When you modify any arch/*.puml file, re-run the renderer locally and commit the generated PNG + SVG images.
  - Recommended (Linux/macOS): ./arch/render-diagrams.sh
  - Recommended (Windows/PowerShell): pwsh arch/render-diagrams.ps1
- Do not modify generated files manually — always produce them from the source .puml.
- PlantUML includes that reference external libraries should be pinned to a stable tag or commit. We use the `release/1-0` tag of RicardoNiepel/C4-PlantUML for stability.

CI behavior
- On pull_request the CI job re-renders diagrams and fails if any differences are detected. This prevents documentation drift.

Troubleshooting
- If CI fails with errors connecting to plantuml includes, check network access or try rendering locally and inspect the inclusion URLs.
- The CI job uses the official plantuml/plantuml Docker image. If you cannot run Docker locally, use the plantuml.jar approach: download plantuml.jar into ./.tools/plantuml.jar and run pwsh arch/render-diagrams.ps1 (PowerShell) or use Java directly.

Contact
- If you need help updating diagrams, open an issue or a PR and add a note for reviewers to help render images if you cannot run Docker locally.
