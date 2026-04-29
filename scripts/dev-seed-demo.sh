#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd node

export NODECONTROL_API_URL

cd_repo_root
node "${REPO_ROOT}/scripts/seed-demo.mjs" "$@"
