. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd docker

Set-RepoRoot
docker compose -f $DevComposeFile down

Write-Host "Dev infrastructure stopped. Volumes are preserved."
