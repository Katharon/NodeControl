#!/usr/bin/env bash

# Shared paths and defaults for local NodeControl dev/demo scripts.

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

BACKEND_SOLUTION="${REPO_ROOT}/src/backend/NodeControl.slnx"
API_PROJECT="${REPO_ROOT}/src/backend/NodeControl.Api/NodeControl.Api.csproj"
WORKER_PROJECT="${REPO_ROOT}/src/backend/NodeControl.Worker/NodeControl.Worker.csproj"
INFRASTRUCTURE_PROJECT="${REPO_ROOT}/src/backend/NodeControl.Infrastructure/NodeControl.Infrastructure.csproj"
FRONTEND_DIR="${REPO_ROOT}/src/frontend/nodecontrol-web"
DEV_COMPOSE_FILE="${REPO_ROOT}/docker-compose.dev.yml"

DEFAULT_CONNECTION_STRING="Host=localhost;Port=5432;Database=nodecontrol;Username=nodecontrol;Password=nodecontrol_dev_password"
NODECONTROL_CONNECTION_STRING="${NODECONTROL_CONNECTION_STRING:-${DEFAULT_CONNECTION_STRING}}"
NODECONTROL_API_URL="${NODECONTROL_API_URL:-http://localhost:5257}"
NODECONTROL_FRONTEND_URL="${NODECONTROL_FRONTEND_URL:-http://localhost:3000}"
NODECONTROL_API_ORIGIN="${NODECONTROL_API_ORIGIN:-${NODECONTROL_API_URL}}"

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 127
  fi
}

cd_repo_root() {
  cd "${REPO_ROOT}"
}
