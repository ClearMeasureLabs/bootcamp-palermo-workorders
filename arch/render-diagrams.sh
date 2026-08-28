#!/usr/bin/env bash
set -euo pipefail

# Render all PlantUML files under arch/ (excluding templates) into PNG and SVG using Docker
# Usage: ./arch/render-diagrams.sh

PLANTUML_IMAGE="plantuml/plantuml:1.2026.2"

ROOT_DIR=$(pwd)
USE_TRUSTSTORE=false
TRUSTSTORE_FILE="truststore.jks"

# If in a sandbox environment with a custom proxy CA certificate, create a custom truststore
if [ -f "/opt/copilot-runtime/mkcert-ca/rootCA.pem" ]; then
  echo "Custom CA certificate found at /opt/copilot-runtime/mkcert-ca/rootCA.pem"
  echo "Generating custom truststore inside container..."
  rm -f "$TRUSTSTORE_FILE"
  if docker run --rm --entrypoint keytool -v /opt/copilot-runtime/mkcert-ca:/ca -v "$ROOT_DIR":/workspace "$PLANTUML_IMAGE" -import -trustcacerts -keystore "/workspace/$TRUSTSTORE_FILE" -storepass changeit -noprompt -alias copilot -file /ca/rootCA.pem >/dev/null 2>&1; then
    echo "Custom truststore generated successfully."
    USE_TRUSTSTORE=true
  else
    echo "Failed to generate custom truststore."
  fi
fi

# Find .puml files excluding the templates folder
find arch -type f -name '*.puml' -not -path 'arch/templates/*' -print0 | while IFS= read -r -d $'\0' f; do
  out_png="${f%.puml}.png"
  out_svg="${f%.puml}.svg"
  echo "Rendering $f -> $out_png, $out_svg"

  # Setup docker arguments depending on custom truststore usage
  DOCKER_ARGS_MOUNT=(-v "$ROOT_DIR":/workspace -w /workspace)
  DOCKER_ARGS_PIPE=()
  if [ "$USE_TRUSTSTORE" = true ]; then
    DOCKER_ARGS_MOUNT+=(--entrypoint java "$PLANTUML_IMAGE" -Djavax.net.ssl.trustStore="/workspace/$TRUSTSTORE_FILE" -Djavax.net.ssl.trustStorePassword=changeit -jar /opt/plantuml.jar)
    DOCKER_ARGS_PIPE+=(-v "$ROOT_DIR":/workspace --entrypoint java "$PLANTUML_IMAGE" -Djavax.net.ssl.trustStore="/workspace/$TRUSTSTORE_FILE" -Djavax.net.ssl.trustStorePassword=changeit -jar /opt/plantuml.jar)
  else
    DOCKER_ARGS_MOUNT+=("$PLANTUML_IMAGE")
    DOCKER_ARGS_PIPE+=("$PLANTUML_IMAGE")
  fi

  # Try mounting the repository into the PlantUML container so local includes resolve
  if docker run --rm "${DOCKER_ARGS_MOUNT[@]}" -tpng "/workspace/$f" > "$out_png" 2>/dev/null; then
    echo "Rendered PNG via mounted container"
  else
    echo "Mounted render failed for PNG, falling back to pipe mode"
    docker run --rm -i "${DOCKER_ARGS_PIPE[@]}" -tpng -pipe < "$f" > "$out_png"
  fi

  if docker run --rm "${DOCKER_ARGS_MOUNT[@]}" -tsvg "/workspace/$f" > "$out_svg" 2>/dev/null; then
    echo "Rendered SVG via mounted container"
  else
    echo "Mounted render failed for SVG, falling back to pipe mode"
    docker run --rm -i "${DOCKER_ARGS_PIPE[@]}" -tsvg -pipe < "$f" > "$out_svg"
  fi

done

if [ "$USE_TRUSTSTORE" = true ]; then
  rm -f "$TRUSTSTORE_FILE"
fi

echo "Done"
