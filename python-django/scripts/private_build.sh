#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
PYTHON_BIN="${PYTHON_BIN:-python3}"
VENV="${VENV_DIR:-.venv}"
if [[ ! -x "$VENV/bin/python" ]]; then "$PYTHON_BIN" -m venv "$VENV"; fi
"$VENV/bin/python" -m pip install --upgrade pip
"$VENV/bin/python" -m pip install -r requirements.txt
export DJANGO_DB_PATH="${DJANGO_DB_PATH:-$PWD/build/private-build.sqlite3}"
mkdir -p "$(dirname "$DJANGO_DB_PATH")"
rm -f "$DJANGO_DB_PATH"
"$VENV/bin/python" manage.py check
"$VENV/bin/python" manage.py migrate --noinput
"$VENV/bin/python" manage.py test --verbosity 2
if [[ "${RUN_ACCEPTANCE:-0}" == "1" ]]; then
  "$VENV/bin/python" -m playwright install chromium
  "$VENV/bin/python" acceptance_tests.py
fi
