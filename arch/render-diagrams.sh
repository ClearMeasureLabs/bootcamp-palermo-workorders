#!/usr/bin/env bash
set -euo pipefail

# Render all PlantUML files under arch/ (excluding templates) into PNG and SVG using Docker
# Usage: ./arch/render-diagrams.sh

PLANTUML_IMAGE="plantuml/plantuml:1.2026.2"

ROOT_DIR=$(pwd)

# Find .puml files excluding the templates folder
find arch -type f -name '*.puml' -not -path 'arch/templates/*' -print0 | while IFS= read -r -d $'\0' f; do
  out_png="${f%.puml}.png"
  out_svg="${f%.puml}.svg"
  echo "Rendering $f -> $out_png, $out_svg"

  # Try mounting the repository into the PlantUML container so local includes resolve
  if docker run --rm -v "$ROOT_DIR":/workspace -w /workspace "$PLANTUML_IMAGE" -tpng "/workspace/$f" > "$out_png" 2>/dev/null; then
    echo "Rendered PNG via mounted container"
  else
    echo "Mounted render failed for PNG, falling back to pipe mode"
    docker run --rm -i "$PLANTUML_IMAGE" -tpng -pipe < "$f" > "$out_png"
  fi

  if docker run --rm -v "$ROOT_DIR":/workspace -w /workspace "$PLANTUML_IMAGE" -tsvg "/workspace/$f" > "$out_svg" 2>/dev/null; then
    echo "Rendered SVG via mounted container"
  else
    echo "Mounted render failed for SVG, falling back to pipe mode"
    docker run --rm -i "$PLANTUML_IMAGE" -tsvg -pipe < "$f" > "$out_svg"
  fi

done

echo "Done"
