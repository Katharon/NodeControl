. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd dotnet

if (-not $env:DOTNET_ENVIRONMENT) {
    $env:DOTNET_ENVIRONMENT = "Development"
}

Set-RepoRoot
dotnet run --project $WorkerProject @args
