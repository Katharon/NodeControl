#!/usr/bin/env bash
set -euo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/dev-env.sh"

require_cmd dotnet

if [ -f "${REPO_ROOT}/.config/dotnet-tools.json" ]; then
  cd_repo_root
  dotnet tool restore
  ef_cmd=(dotnet tool run dotnet-ef)
elif dotnet ef --version >/dev/null 2>&1; then
  ef_cmd=(dotnet ef)
else
  echo "dotnet-ef is required for migrations." >&2
  echo "Restore the local tool with: dotnet tool restore" >&2
  exit 127
fi

export NODECONTROL_CONNECTION_STRING

cd_repo_root
"${ef_cmd[@]}" database update \
  --project "${INFRASTRUCTURE_PROJECT}" \
  --startup-project "${API_PROJECT}" \
  --context NodeControlDbContext
