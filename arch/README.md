# Architecture diagrams

Mermaid C4 diagrams in this folder use **icons from [icones.js.org](https://icones.js.org/)** (Iconify). The icon pack used is **[Tabler](https://icones.js.org/collection/tabler)**.

## Rendering with icons

To render the diagrams with icons, register the Tabler icon pack as described in [Mermaid: Registering icon pack](https://mermaid.js.org/config/icons.html).

**Example (JavaScript):**

```javascript
import mermaid from 'mermaid';

mermaid.registerIconPacks([
  {
    name: 'tabler',
    loader: () =>
      fetch('https://unpkg.com/@iconify-json/tabler@1/icons.json').then((res) => res.json()),
  },
]);
```

**Example (mermaid-cli):**

```bash
mmdc -i arch-c4-system.md -o arch-c4-system.svg --iconPacks '@iconify-json/tabler'
```

Icons are specified in C4 elements via the optional `sprite` parameter (e.g. `"tabler:user"`, `"tabler:database"`). If the renderer does not support sprites or icon packs are not registered, the diagrams still render without icons.

## Rendering PlantUML diagrams

PlantUML sources are located in this folder (files with the .puml extension). To render the PlantUML diagrams into PNG and SVG images, use the helper script included in this folder:

PowerShell (preferred on Windows/macOS):

```powershell
pwsh arch/render-diagrams.ps1
```

The script prefers Docker (plantuml/plantuml image) and will fall back to a local plantuml.jar if you have placed it in ./.tools/plantuml.jar and have a suitable Java runtime (>= 11).

If you prefer to render one file with Docker directly (no script):

```bash
# PNG
docker run --rm -i plantuml/plantuml -tpng -pipe < arch/arch-c4-system.puml > arch/arch-c4-system.png
# SVG
docker run --rm -i plantuml/plantuml -tsvg -pipe < arch/arch-c4-system.puml > arch/arch-c4-system.svg
```

## Pinning remote includes

Several PlantUML files include remote C4 library snippets via `!includeurl`. To avoid unexpected breakage if the upstream repository changes, the diagrams in this repo pin those includes to the `release/1-0` tag. When updating PlantUML includes, prefer using a stable tag or commit hash rather than the `master` branch.

## Diagram files

| File | Diagram type | Description |
|------|--------------|-------------|
| `arch-c4-system.md` | C4Context | Church Bulletin system context |
| `arch-c4-container-deployment.md` | C4Container | Containers (DB, app, UI) |
| `arch-c4-component-project-dependencies.md` | C4Component | Solution/project structure |
| `arch-c4-class-domain-model.md` | C4Component | Work order domain model |
| `WorflowFor*.md` | Sequence | Command workflow sequence diagrams |
| `arch-c4-container-ai-azure-ai-foundry-chat.puml` | C4Container | Reference: Azure AI Foundry for a chat-style client (managed inference) |
| `arch-c4-container-ai-foundry-local-phi4.puml` | C4Container | Reference: Foundry Local with Phi-4 on an edge host (no cloud LLM) |
| `arch-c4-container-ai-ollama-microsoft-extensions-ai.puml` | C4Container | Reference: `Microsoft.Extensions.AI` / LlmGateway with Ollama on the LAN |
| `arch-c4-container-ai-rag-workorders.puml` | C4Container | Reference: RAG pipeline—vectorize work orders, similarity search, LLM answers |
