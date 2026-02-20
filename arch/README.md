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

## Diagram files

| File | Diagram type | Description |
|------|--------------|-------------|
| `arch-c4-system.md` | C4Context | Church Bulletin system context |
| `arch-c4-container-deployment.md` | C4Container | Containers (DB, app, UI) |
| `arch-c4-component-project-dependencies.md` | C4Component | Solution/project structure |
| `arch-c4-class-domain-model.md` | C4Component | Work order domain model |
| `WorflowFor*.md` | Sequence | Command workflow sequence diagrams |
