#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd npm

export NODECONTROL_API_ORIGIN

cd "${FRONTEND_DIR}"
npm run dev -- "$@"
