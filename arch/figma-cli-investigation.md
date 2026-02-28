# Figma CLI Investigation: Can Figma Render Architecture Diagrams?

**Date:** 2026-02-28
**Conclusion:** No — Figma CLI tools cannot render Mermaid or PlantUML diagrams.

## Current Diagram Formats in This Repo

| Format | Files | Usage |
|--------|-------|-------|
| Mermaid (`.md`) | 8 diagrams | C4 context, container, component, domain model, 4 workflow sequences |
| PlantUML (`.puml`) | 4 diagrams + 5 templates | C4 context, container, component, domain model |
| PNG/ARGR | 1 each | SolutionStructure rendered image |

All Mermaid diagrams use **Tabler icons** and a **custom CSS theme** (`mermaid-theme.css`).

## Figma CLI Tools Evaluated

### figma-use (dannote)

Controls Figma Desktop via its internal multiplayer protocol. Offers 100+ commands to create shapes, text, frames, and export assets. Cannot accept Mermaid or PlantUML input. Designed for AI agents to manipulate Figma design files, not render text-based diagrams.

- Repository: https://github.com/dannote/figma-use

### figma-cli (silships)

Wraps figma-use for Claude Code integration. Same fundamental limitation — operates on Figma design primitives, not diagram markup languages.

- Repository: https://github.com/silships/figma-cli

### Figma Gemini CLI Extension (Official)

Official MCP server connecting Figma to Gemini CLI. Reads Figma files and extracts design context for code generation. Read-only; cannot create or render diagrams from text input.

- Repository: https://github.com/figma/figma-gemini-cli-extension

### Figma Code Connect CLI (Official)

Maps code snippets to Figma components in Dev Mode. Unrelated to diagram rendering.

- Docs: https://developers.figma.com/docs/code-connect/quickstart-guide/

### figma-export / figma-exporter

Bulk export existing Figma files to PNG/SVG/PDF via the REST API. Requires designs to already exist in Figma; cannot create them from text-based diagram formats.

- https://github.com/alexchantastic/figma-export
- https://github.com/smbecker/figma-exporter

## Figma GUI Plugins (Not CLI-Based)

Several interactive Figma plugins handle Mermaid, but they require the desktop app and cannot be driven from a CLI or CI pipeline:

- **Mermaid in Figma** — paste Mermaid code, renders inside Figma
- **FigJam ↔ Mermaid Converter** — bidirectional FigJam/Mermaid conversion
- **Mermaid-to-Flow** — Mermaid markup to FigJam flowcharts
- **Mermaid to FigJam** — includes reverse conversion

**Known issue:** Mermaid SVG exports render as black boxes when imported into Figma (tracked at https://github.com/mermaid-js/mermaid/issues/6915), making the SVG pipeline unreliable.

No PlantUML plugins exist for Figma.

## Recommended CLI Tools for Diagram Rendering

### Mermaid CLI (mmdc)

The correct CLI tool for rendering the Mermaid diagrams in this repo:

```bash
npm install -g @mermaid-js/mermaid-cli
mmdc -i arch-c4-system.md -o arch-c4-system.svg --iconPacks '@iconify-json/tabler'
```

### PlantUML CLI

Handles `.puml` templates with GraphViz:

```bash
java -jar plantuml.jar arch-c4-system.puml
```

## Summary

Figma is a design tool, not a diagram rendering engine. Its CLI ecosystem is oriented toward manipulating and exporting existing Figma design files, not converting text-based diagram formats like Mermaid or PlantUML. The `mermaid-cli` (`mmdc`) and `PlantUML CLI` remain the correct tools for rendering this repo's architecture diagrams from the command line.
