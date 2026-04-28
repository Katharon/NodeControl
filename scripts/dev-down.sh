#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd docker

cd_repo_root
docker compose -f "${DEV_COMPOSE_FILE}" down

echo "Dev infrastructure stopped. Volumes are preserved."
