. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd docker

Set-RepoRoot
docker compose -f $DevComposeFile up -d

Write-Host "Dev infrastructure is running."
Write-Host "PostgreSQL: localhost:5432"
Write-Host "Keycloak dev provider: http://localhost:18080"
