#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd dotnet

export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"
export NODECONTROL_CONNECTION_STRING

cd_repo_root
dotnet run --project "${WORKER_PROJECT}" "$@"
