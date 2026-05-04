#!/usr/bin/env bash
set -euo pipefail

# Render all PlantUML files under arch/ (excluding templates) into PNG and SVG using Docker
# Usage: ./arch/render-diagrams.sh

FILES=$(find . -type f -path './arch/*.puml' -not -path './arch/templates/*' -print)
if [ -z "$FILES" ]; then
  echo "No .puml files found under arch/"
  exit 0
fi

for f in $FILES; do
  echo "Rendering $f -> ${f%.puml}.png, ${f%.puml}.svg"
  docker run --rm -i plantuml/plantuml -tpng -pipe < "$f" > "${f%.puml}.png"
  docker run --rm -i plantuml/plantuml -tsvg -pipe < "$f" > "${f%.puml}.svg"
done

echo "Done"
