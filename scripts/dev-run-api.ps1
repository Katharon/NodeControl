. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd dotnet

if (-not $env:ASPNETCORE_ENVIRONMENT) {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
}

if (-not $env:ASPNETCORE_URLS) {
    $env:ASPNETCORE_URLS = $env:NODECONTROL_API_URL
}

Set-RepoRoot
dotnet run --project $ApiProject --urls $env:ASPNETCORE_URLS @args
