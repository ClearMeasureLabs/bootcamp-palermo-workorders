#!/usr/bin/env bash
set -euo pipefail

# Setup developer environment helpers
# - Configure repository-local git hooks path
# - Make pre-commit hook executable (if present)

echo "Setting repository hooks path to .githooks"
git config core.hooksPath .githooks

if [ -f ".githooks/pre-commit" ]; then
  echo "Making .githooks/pre-commit executable"
  chmod +x .githooks/pre-commit || true
fi

echo "Setup complete. To enable hook enforcement in your shell, ensure you've run this script from the repo root."
