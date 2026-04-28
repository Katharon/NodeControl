#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd dotnet
require_cmd npm
require_cmd grep

cd_repo_root

echo "Checking that no .sln file exists..."
sln_files="$(find . -name "*.sln" -print)"
if [ -n "${sln_files}" ]; then
  echo "${sln_files}" >&2
  echo "Unexpected .sln file found. NodeControl uses src/backend/NodeControl.slnx." >&2
  exit 1
fi

echo "Restoring backend..."
dotnet restore "${BACKEND_SOLUTION}"

echo "Building backend..."
dotnet build "${BACKEND_SOLUTION}"

echo "Testing backend..."
dotnet test "${BACKEND_SOLUTION}"

echo "Linting frontend..."
(
  cd "${FRONTEND_DIR}"
  npm run lint
)

echo "Building frontend..."
(
  cd "${FRONTEND_DIR}"
  npm run build
)

echo "Checking API process execution boundary..."
api_process_matches="$(
  grep -RIn --exclude-dir=bin --exclude-dir=obj "ProcessStartInfo\|Process.Start\|ansible-playbook" \
    "${REPO_ROOT}/src/backend/NodeControl.Api" || true
)"
if [ -n "${api_process_matches}" ]; then
  echo "${api_process_matches}" >&2
  echo "Forbidden process execution reference found in NodeControl.Api." >&2
  exit 1
fi

if command -v curl >/dev/null 2>&1; then
  if curl -fsS "${NODECONTROL_API_URL}/api/v1/me" >/dev/null 2>&1; then
    echo "API smoke check passed: ${NODECONTROL_API_URL}/api/v1/me"
  else
    echo "API smoke check skipped: start the API with scripts/dev-run-api.sh"
  fi

  if curl -fsSI "${NODECONTROL_FRONTEND_URL}/dashboard" >/dev/null 2>&1; then
    echo "Frontend smoke check passed: ${NODECONTROL_FRONTEND_URL}/dashboard"
  else
    echo "Frontend smoke check skipped: start the frontend with scripts/dev-run-frontend.sh"
  fi
else
  echo "curl not found; local HTTP smoke checks skipped."
fi

echo "Smoke checks completed."
