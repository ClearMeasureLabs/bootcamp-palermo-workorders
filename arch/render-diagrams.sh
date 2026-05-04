#!/usr/bin/env bash
set -euo pipefail

# Render all PlantUML files under arch/ (excluding templates) into PNG and SVG using Docker
# Usage: ./arch/render-diagrams.sh

PLANTUML_IMAGE="plantuml/plantuml:1.2026.2"

FILES=$(find arch -type f -name '*.puml' -not -path './arch/templates/*' -print)
if [ -z "$FILES" ]; then
  echo "No .puml files found under arch/"
  exit 0
fi

for f in $FILES; do
  echo "Rendering $f -> ${f%.puml}.png, ${f%.puml}.svg"
  # Use pipe mode to avoid Docker volume mount path issues on some platforms
  docker run --rm -i "$PLANTUML_IMAGE" -tpng -pipe < "$f" > "${f%.puml}.png"
  docker run --rm -i "$PLANTUML_IMAGE" -tsvg -pipe < "$f" > "${f%.puml}.svg"
done

echo "Done"
