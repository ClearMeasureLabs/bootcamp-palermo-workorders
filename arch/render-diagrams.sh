#!/usr/bin/env bash
set -euo pipefail

# Render all PlantUML files under arch/ (excluding templates) into PNG and SVG using Docker
# Usage: ./arch/render-diagrams.sh

if pwd -W >/dev/null 2>&1; then
  ROOT_DIR=$(pwd -W)
else
  ROOT_DIR=$(pwd)
fi

FILES=$(find arch -type f -name '*.puml' -not -path './arch/templates/*' -print)
if [ -z "$FILES" ]; then
  echo "No .puml files found under arch/"
  exit 0
fi

for f in $FILES; do
  echo "Rendering $f -> ${f%.puml}.png, ${f%.puml}.svg"
  docker run --rm -v "$ROOT_DIR":/workspace -w /workspace plantuml/plantuml -tpng "/workspace/$f"
  docker run --rm -v "$ROOT_DIR":/workspace -w /workspace plantuml/plantuml -tsvg "/workspace/$f"
done

echo "Done"
