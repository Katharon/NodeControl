#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd docker

cd_repo_root
docker compose -f "${DEV_COMPOSE_FILE}" up -d

echo "Dev infrastructure is running."
echo "PostgreSQL: localhost:5432"
echo "Keycloak dev provider: http://localhost:18080"
