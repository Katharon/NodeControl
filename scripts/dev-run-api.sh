#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd dotnet

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-${NODECONTROL_API_URL}}"
export NODECONTROL_CONNECTION_STRING

cd_repo_root
dotnet run --project "${API_PROJECT}" --urls "${ASPNETCORE_URLS}" "$@"
