#!/usr/bin/env bash
# Classifier checks for detect-code-changes.sh (no git / no GITHUB_OUTPUT).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DETECT="$SCRIPT_DIR/detect-code-changes.sh"
failed=0

assert_code() {
  local expected="$1"
  local label="$2"
  shift 2
  local actual
  actual="$("$DETECT" --from-list - <<<"$(printf '%s\n' "$@")" | awk -F= '/^code=/{print $2; exit}')"
  if [[ "$actual" != "$expected" ]]; then
    echo "FAIL: $label (expected code=$expected, got code=$actual)" >&2
    printf '  paths:\n' >&2
    printf '    %s\n' "$@" >&2
    failed=1
  else
    echo "PASS: $label (code=$expected)"
  fi
}

assert_code false "README only" "README.md"
assert_code false "docs markdown" "docs/foo.md"
assert_code false "nested docs" "docs/guide/intro.md"
assert_code false "mixed docs-only" "README.md" "docs/foo.md" "LICENSE"
assert_code false "issue template" ".github/ISSUE_TEMPLATE/bug.yml"
assert_code true "src path" "src/Core/Model/WorkOrder.cs"
assert_code true "src mixed with readme" "README.md" "src/Core/Model/WorkOrder.cs"
assert_code true "csproj" "src/Core/Core.csproj"
assert_code true "solution" "src/ChurchBulletin.sln"
assert_code true "build script" "PrivateBuild.ps1"
assert_code true "build.ps1" "build.ps1"
assert_code true "BuildFunctions" "BuildFunctions.ps1"
assert_code true "AcceptanceTests.ps1" "AcceptanceTests.ps1"
assert_code true "Dockerfile" "Dockerfile"
assert_code true "qodana.yaml" "qodana.yaml"
assert_code true "workflow" ".github/workflows/build.yml"
assert_code true "arch diagram" "arch/C4-Logical.puml"
assert_code true "nuget config" "nuget.config"
assert_code true "global.json" "global.json"
assert_code true "CI detect script" ".github/scripts/detect-code-changes.sh"

if [[ "$failed" -ne 0 ]]; then
  echo "detect-code-changes tests failed" >&2
  exit 1
fi
echo "All detect-code-changes tests passed."
